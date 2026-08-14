using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Vsix
{
	/// <summary>
	/// Guards the two files that describe the menu of the extension against drifting apart.
	/// </summary>
	/// <remarks>
	/// <c>Commands.cs</c> is generated from <c>Commands.vsct</c> by the VSIX Synchronizer and is
	/// checked in. The generator only runs inside Visual Studio, so an edit of the vsct file made
	/// anywhere else leaves the constants behind. A stale constant compiles and only fails at run
	/// time, where the menu item silently does not appear.
	/// </remarks>
	public class CommandTableTests
	{
		private static readonly string RepositoryRoot = FindRepositoryRoot();

		private static string VsctPath =>
			Path.Combine(RepositoryRoot, "src", "SharedFiles", "Commands.vsct");

		private static string GeneratedPath =>
			Path.Combine(RepositoryRoot, "src", "SharedFiles", "Commands.cs");

		[Fact]
		public void The_generated_ids_match_the_command_table()
		{
			IDictionary<string, int> vsct = SymbolsFromVsct();
			IDictionary<string, int> generated = SymbolsFromGeneratedFile();

			generated
				.Keys.Except(vsct.Keys)
				.Should()
				.BeEmpty("every generated constant must still exist in Commands.vsct");

			foreach (var symbol in vsct)
			{
				generated
					.Should()
					.ContainKey(
						symbol.Key,
						"Commands.vsct declares {0}, so Commands.cs has to expose it",
						symbol.Key
					);

				generated[symbol.Key].Should().Be(symbol.Value, "the value of {0}", symbol.Key);
			}
		}

		[Fact]
		public void Every_button_refers_to_a_declared_symbol()
		{
			IDictionary<string, int> vsct = SymbolsFromVsct();
			XDocument document = XDocument.Load(VsctPath);
			XNamespace ns = document.Root.GetDefaultNamespace();

			IEnumerable<string> referenced = document
				.Descendants()
				.Where(
					element =>
						element.Name == ns + "Button"
						|| element.Name == ns + "Group"
						|| element.Name == ns + "Menu"
						|| element.Name == ns + "CommandPlacement"
				)
				.Select(element => (string)element.Attribute("id"))
				.Where(id => !string.IsNullOrEmpty(id));

			referenced.Distinct().Should().OnlyContain(id => vsct.ContainsKey(id));
		}

		[Fact]
		public void Every_command_id_is_unique()
		{
			// Two buttons that share an id are routed to the same handler, which is a defect that
			// is very hard to spot in the XML.
			var duplicates = SymbolValues()
				.GroupBy(symbol => symbol.Value)
				.Where(group => group.Count() > 1)
				.Select(group => string.Join(" = ", group.Select(s => s.Key)))
				.ToList();

			duplicates.Should().BeEmpty();
		}

		private static IEnumerable<KeyValuePair<string, int>> SymbolValues()
		{
			XDocument document = XDocument.Load(VsctPath);
			XNamespace ns = document.Root.GetDefaultNamespace();

			return document
				.Descendants(ns + "GuidSymbol")
				.Where(guid => (string)guid.Attribute("name") == "ProductivityToolsCmdSet")
				.Descendants(ns + "IDSymbol")
				.Select(
					symbol =>
						new KeyValuePair<string, int>(
							(string)symbol.Attribute("name"),
							Convert.ToInt32((string)symbol.Attribute("value"), 16)
						)
				)
				.ToList();
		}

		private static IDictionary<string, int> SymbolsFromVsct()
		{
			XDocument document = XDocument.Load(VsctPath);
			XNamespace ns = document.Root.GetDefaultNamespace();

			return document
				.Descendants(ns + "IDSymbol")
				.ToDictionary(
					symbol => (string)symbol.Attribute("name"),
					symbol => Convert.ToInt32((string)symbol.Attribute("value"), 16)
				);
		}

		private static IDictionary<string, int> SymbolsFromGeneratedFile()
		{
			return Regex
				.Matches(
					File.ReadAllText(GeneratedPath),
					@"public const int (?<name>\w+) = (?<value>0x[0-9A-Fa-f]+);"
				)
				.Cast<Match>()
				.ToDictionary(
					match => match.Groups["name"].Value,
					match => Convert.ToInt32(match.Groups["value"].Value.Substring(2), 16)
				);
		}

		private static string FindRepositoryRoot()
		{
			DirectoryInfo directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

			while (directory != null)
			{
				if (File.Exists(Path.Combine(directory.FullName, "TwinCAT.ProductivityTools.sln")))
				{
					return directory.FullName;
				}

				directory = directory.Parent;
			}

			throw new DirectoryNotFoundException(
				"The repository root could not be located from "
					+ AppDomain.CurrentDomain.BaseDirectory
			);
		}
	}
}
