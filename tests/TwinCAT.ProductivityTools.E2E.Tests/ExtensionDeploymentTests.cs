using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EnvDTE;
using FluentAssertions;
using TwinCAT.ProductivityTools.E2E.Tests.Infrastructure;
using Xunit;

namespace TwinCAT.ProductivityTools.E2E.Tests
{
	/// <summary>
	/// Proves that the extension really arrives in a real IDE.
	/// </summary>
	/// <remarks>
	/// The integration tests read the command table from the source tree, which says what the
	/// extension intends to register. Only a running IDE can say what it actually registered: a
	/// command table that fails to merge, a manifest that targets the wrong shell version or a
	/// missing dependency all end in the same symptom, namely a menu that is simply not there.
	/// </remarks>
	[Collection(TwinCatSolutionCollection.Name)]
	public class ExtensionDeploymentTests
	{
		private const string CommandSet = "7e8a71c6-214a-40a2-a302-026e804258d4";

		private readonly TwinCatSolutionFixture solution;

		public ExtensionDeploymentTests(TwinCatSolutionFixture solution)
		{
			this.solution = solution;
		}

		[E2EFact]
		public void TheIdeKnowsTheProductivityToolsCommandSet()
		{
			IReadOnlyCollection<int> registered = solution.Run(RegisteredCommandIds);

			registered
				.Should()
				.NotBeEmpty(
					"the command table of the extension has to merge into the shell; an empty "
						+ "result means the VSIX is not installed for "
						+ solution.Session.Ide.Kind
				);
		}

		[E2EFact]
		public void EveryButtonOfTheCommandTableExistsInTheIde()
		{
			IReadOnlyCollection<int> registered = solution.Run(RegisteredCommandIds);

			IReadOnlyCollection<int> expected = ButtonIdsFromTheCommandTable();

			expected.Should().NotBeEmpty("the command table has to contain buttons");

			registered
				.Should()
				.Contain(
					expected,
					"every button declared in Commands.vsct has to reach the running IDE"
				);
		}

		[E2EFact]
		public void TheCommandsAreAvailableWhileATwinCatProjectIsOpen()
		{
			bool anyAvailable = solution.Run(() =>
			{
				var set = new Guid(CommandSet);

				foreach (Command command in solution.Session.Dte.Commands)
				{
					Guid parsed;

					if (
						Guid.TryParse(command.Guid, out parsed)
						&& parsed == set
						&& command.IsAvailable
					)
					{
						return true;
					}
				}

				return false;
			});

			// A command that never becomes available is indistinguishable from a missing one for
			// the user. At least one of them has to react to the open TwinCAT project.
			anyAvailable
				.Should()
				.BeTrue("no command of the extension became available for an open TwinCAT project");
		}

		private IReadOnlyCollection<int> RegisteredCommandIds()
		{
			var set = new Guid(CommandSet);

			var found = new List<int>();

			foreach (Command command in solution.Session.Dte.Commands)
			{
				Guid parsed;

				if (Guid.TryParse(command.Guid, out parsed) && parsed == set)
				{
					found.Add(command.ID);
				}
			}

			return found;
		}

		/// <summary>
		/// Reads the identifiers of every button from the checked in command table.
		/// </summary>
		private static IReadOnlyCollection<int> ButtonIdsFromTheCommandTable()
		{
			XDocument table = XDocument.Load(RepositoryPaths.CommandTable);

			XNamespace ns = table.Root.GetDefaultNamespace();

			Dictionary<string, int> symbols = table
				.Descendants(ns + "GuidSymbol")
				.Where(guid => (string)guid.Attribute("name") == "ProductivityToolsCmdSet")
				.Elements(ns + "IDSymbol")
				.ToDictionary(
					symbol => (string)symbol.Attribute("name"),
					symbol => ParseId((string)symbol.Attribute("value"))
				);

			return table
				.Descendants(ns + "Button")
				.Where(
					button =>
						(string)button.Attribute("guid") == "ProductivityToolsCmdSet"
						&& symbols.ContainsKey((string)button.Attribute("id") ?? string.Empty)
				)
				.Select(button => symbols[(string)button.Attribute("id")])
				.Distinct()
				.ToList();
		}

		private static int ParseId(string value) =>
			value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
				? Convert.ToInt32(value.Substring(2), 16)
				: Convert.ToInt32(value, 10);
	}
}
