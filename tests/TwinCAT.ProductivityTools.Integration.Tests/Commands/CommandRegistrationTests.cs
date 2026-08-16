using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Community.VisualStudio.Toolkit;
using FluentAssertions;
using Xunit;

namespace TwinCAT.ProductivityTools.Integration.Tests.Commands
{
	/// <summary>
	/// Checks that every menu entry of the extension actually reaches a handler.
	/// </summary>
	/// <remarks>
	/// The toolkit discovers command handlers by scanning the assembly for types that derive from
	/// <see cref="BaseCommand{T}"/> and carry a <see cref="CommandAttribute"/>. Nothing fails when
	/// a button of the command table has no handler, or when two handlers claim the same id: the
	/// menu entry simply does nothing, or the wrong code runs. Both cases are caught here.
	/// </remarks>
	public class CommandRegistrationTests
	{
		private static readonly Assembly ExtensionAssembly =
			typeof(ProductivityToolsPackage).Assembly;

		private static IReadOnlyList<Type> CommandTypes =>
			ExtensionAssembly
				.GetTypes()
				.Where(type => !type.IsAbstract && IsCommandHandler(type))
				.ToList();

		[Fact]
		public void EveryCommandHandlerDeclaresACommandId()
		{
			CommandTypes
				.Should()
				.NotBeEmpty()
				.And.OnlyContain(type => type.GetCustomAttribute<CommandAttribute>() != null);
		}

		[Fact]
		public void NoTwoHandlersClaimTheSameCommandId()
		{
			var duplicates = CommandTypes
				.GroupBy(type => type.GetCustomAttribute<CommandAttribute>().Id)
				.Where(group => group.Count() > 1)
				.Select(
					group =>
						$"0x{group.Key:X4}: {string.Join(", ", group.Select(type => type.Name))}"
				)
				.ToList();

			duplicates.Should().BeEmpty("a command id can only have one handler");
		}

		[Fact]
		public void EveryCommandUsesTheCommandSetOfThisPackage()
		{
			// A handler that names a command set explicitly registers itself under that set. The
			// vsct references the command sets of the TwinCAT project systems as well, but only to
			// place groups into their context menus, so a handler pointing at one of them would
			// never be invoked. Leaving the guid off means "the command set of this package",
			// which is what every handler here relies on.
			CommandTypes
				.Should()
				.OnlyContain(
					type =>
						type.GetCustomAttribute<CommandAttribute>().Guid == Guid.Empty
						|| type.GetCustomAttribute<CommandAttribute>().Guid
							== PackageGuids.ProductivityToolsCmdSet
				);
		}

		[Fact]
		public void EveryCommandIdIsDeclaredInTheCommandTable()
		{
			HashSet<int> declared = new HashSet<int>(
				ButtonsFromCommandTable().Select(button => button.Value)
			);

			foreach (Type type in CommandTypes)
			{
				int id = type.GetCustomAttribute<CommandAttribute>().Id;

				declared
					.Should()
					.Contain(
						id,
						"{0} handles 0x{1:X4}, which has to be a button in Commands.vsct",
						type.Name,
						id
					);
			}
		}

		[Fact]
		public void EveryButtonOfTheCommandTableHasAHandler()
		{
			HashSet<int> handled = new HashSet<int>(
				CommandTypes.Select(type => type.GetCustomAttribute<CommandAttribute>().Id)
			);

			IEnumerable<string> orphans = ButtonsFromCommandTable()
				.Where(button => !handled.Contains(button.Value))
				.Select(button => $"{button.Key} (0x{button.Value:X4})");

			orphans
				.Should()
				.BeEmpty("a button without a handler shows up in the menu and does nothing");
		}

		[Fact]
		public void CommandHandlersAreSealed()
		{
			// The handlers are instantiated by the toolkit and are not an extension point. Sealing
			// them keeps the inheritance that does exist - the shared base classes - meaningful.
			CommandTypes.Should().OnlyContain(type => type.IsSealed);
		}

		[Fact]
		public void CommandHandlersAreNamedAfterTheirCommand()
		{
			// The toolkit resolves nothing by name, but the constant of a handler and its type
			// name are the only link a reader has between a menu entry and the code behind it.
			IReadOnlyList<KeyValuePair<string, int>> buttons = ButtonsFromCommandTable();

			foreach (Type type in CommandTypes)
			{
				int id = type.GetCustomAttribute<CommandAttribute>().Id;

				string constant = buttons.First(button => button.Value == id).Key;

				constant
					.Should()
					.Be(
						type.Name + "Id",
						"the handler {0} should be named after the constant it handles",
						type.Name
					);
			}
		}

		private static bool IsCommandHandler(Type type)
		{
			for (Type current = type.BaseType; current != null; current = current.BaseType)
			{
				if (
					current.IsGenericType
					&& current.GetGenericTypeDefinition() == typeof(BaseCommand<>)
				)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Reads the buttons of the command table that belong to this package, resolved to their
		/// numeric value through the generated constants.
		/// </summary>
		private static IReadOnlyList<KeyValuePair<string, int>> ButtonsFromCommandTable()
		{
			XDocument document = XDocument.Load(CommandTablePath);
			XNamespace ns = document.Root.GetDefaultNamespace();

			IDictionary<string, int> symbols = typeof(PackageIds)
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(field => field.IsLiteral && field.FieldType == typeof(int))
				.ToDictionary(field => field.Name, field => (int)field.GetRawConstantValue());

			List<KeyValuePair<string, int>> buttons = new List<KeyValuePair<string, int>>();

			foreach (XElement button in document.Descendants(ns + "Button"))
			{
				string guid = button.Attribute("guid")?.Value;
				string name = button.Attribute("id")?.Value;

				if (
					guid != nameof(PackageGuids.ProductivityToolsCmdSet)
					|| name == null
					|| !symbols.TryGetValue(name, out int value)
				)
				{
					continue;
				}

				buttons.Add(new KeyValuePair<string, int>(name, value));
			}

			buttons.Should().NotBeEmpty("Commands.vsct has to declare the buttons of this package");

			return buttons;
		}

		private static string CommandTablePath =>
			Path.Combine(RepositoryRoot, "src", "SharedFiles", "Commands.vsct");

		private static string RepositoryRoot
		{
			get
			{
				DirectoryInfo directory = new DirectoryInfo(
					Path.GetDirectoryName(new Uri(ExtensionAssembly.CodeBase).LocalPath)
				);

				while (
					directory != null
					&& !Directory.Exists(Path.Combine(directory.FullName, ".git"))
					&& !File.Exists(Path.Combine(directory.FullName, ".git"))
				)
				{
					directory = directory.Parent;
				}

				if (directory == null)
				{
					throw new InvalidOperationException("The repository root was not found.");
				}

				return directory.FullName;
			}
		}
	}
}
