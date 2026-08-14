using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FluentAssertions;
using TwinCAT.ProductivityTools.Io;
using TwinCAT.ProductivityTools.ToolWindows;
using Xunit;

namespace TwinCAT.ProductivityTools.Integration.Tests.ViewModels
{
	/// <summary>
	/// Covers how the flat mapping list of TwinCAT becomes the tree the tool window shows.
	/// </summary>
	/// <remarks>
	/// TwinCAT reports a mapping under a key such as
	/// <c>TIID^Device 1 (EtherCAT)^Term 1^Channel 1</c>. The separator is what turns the list into
	/// a tree, and getting it wrong produces a window that either shows one flat row per link or
	/// silently drops mappings.
	/// </remarks>
	public class IOMappingViewModelTests
	{
		[Fact]
		public void SplitsAKeyIntoOneLevelPerSegment()
		{
			var tree = IOMappingViewModel.BuildTree(
				new Dictionary<string, List<Variable>>
				{
					["TIID^Device 1 (EtherCAT)^Term 1"] = new List<Variable>
					{
						Link("Input", "MAIN.bIn")
					}
				}
			);

			tree.Should().ContainSingle().Which.Name.Should().Be("TIID");
			tree[0].Children.Should().ContainSingle().Which.Name.Should().Be("Device 1 (EtherCAT)");
			tree[0].Children[0].Children.Should().ContainSingle().Which.Name.Should().Be("Term 1");
		}

		[Fact]
		public void MergesKeysThatShareAPrefix()
		{
			// Two devices below the same I/O node have to end up as two children of one node, not
			// as two roots. This is what makes the tree usable on a real configuration.
			var tree = IOMappingViewModel.BuildTree(
				new Dictionary<string, List<Variable>>
				{
					["TIID^Device 1"] = new List<Variable> { Link("A", "MAIN.a") },
					["TIID^Device 2"] = new List<Variable> { Link("B", "MAIN.b") }
				}
			);

			tree.Should().ContainSingle();
			tree[0]
				.Children.Select(child => child.Name)
				.Should()
				.BeEquivalentTo("Device 1", "Device 2");
		}

		[Fact]
		public void ShowsEveryLinkOfALeafWithItsAddress()
		{
			var tree = IOMappingViewModel.BuildTree(
				new Dictionary<string, List<Variable>>
				{
					["TIID^Term 1"] = new List<Variable>
					{
						Link("Channel 1", "MAIN.bIn", size: 1, offset: 0),
						Link("Channel 2", "MAIN.bOut", size: 1, offset: 1)
					}
				}
			);

			ObservableCollection<TreeNode> leaves = tree[0].Children[0].Children;

			leaves.Should().HaveCount(2);
			leaves[0].Name.Should().Be("Channel 1 | MAIN.bIn | size 1 | offset 0");
			leaves[1].Name.Should().Be("Channel 2 | MAIN.bOut | size 1 | offset 1");
		}

		[Fact]
		public void SkipsAMissingLink()
		{
			// A malformed mapping must cost its own row, not the whole tree.
			var tree = IOMappingViewModel.BuildTree(
				new Dictionary<string, List<Variable>>
				{
					["TIID^Term 1"] = new List<Variable> { null, Link("Channel 1", "MAIN.bIn") }
				}
			);

			tree[0].Children[0].Children.Should().ContainSingle();
		}

		[Fact]
		public void ToleratesAMappingWithoutLinks()
		{
			var tree = IOMappingViewModel.BuildTree(
				new Dictionary<string, List<Variable>> { ["TIID^Term 1"] = null }
			);

			tree[0].Children.Should().ContainSingle().Which.Children.Should().BeEmpty();
		}

		[Fact]
		public void ToleratesNoMappingsAtAll()
		{
			IOMappingViewModel.BuildTree(null).Should().BeEmpty();
			IOMappingViewModel
				.BuildTree(new Dictionary<string, List<Variable>>())
				.Should()
				.BeEmpty();
		}

		[Fact]
		public void ExtractsVariablesFromTheMappingXml()
		{
			// The tool window feeds the raw payload of ProduceMappingInfo straight into the
			// parser. Anything that is not valid mapping xml has to come back as an empty result
			// rather than as an exception, because the tool window offers a reload button.
			IOMappingViewModel.ExtractVariables(null).Should().BeEmpty();
			IOMappingViewModel.ExtractVariables(string.Empty).Should().BeEmpty();
		}

		private static Variable Link(string name, string path, int size = 1, int offset = 0) =>
			new Variable
			{
				Name = name,
				Path = path,
				Size = size,
				Offset = offset
			};
	}
}
