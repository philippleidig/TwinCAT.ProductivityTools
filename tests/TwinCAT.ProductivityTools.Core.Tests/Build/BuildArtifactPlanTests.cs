using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using TwinCAT.ProductivityTools.Build;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Build
{
	public class BuildArtifactPlanTests
	{
		private const string Root = @"C:\proj";

		[Fact]
		public void Treats_a_trailing_slash_as_a_directory()
		{
			BuildArtifactPlan plan = BuildArtifactPlan.Create(Root, new[] { "_Boot/", "a.tmc" });

			plan.Directories.Should().ContainSingle().Which.Should().Be(@"C:\proj\_Boot");
			plan.Files.Should().ContainSingle().Which.Should().Be(@"C:\proj\a.tmc");
		}

		[Fact]
		public void Joins_the_entry_to_the_root_with_a_separator()
		{
			// The previous implementation concatenated the strings and turned "_Boot/" into
			// "C:\proj_Boot", which is a sibling of the project rather than a child.
			BuildArtifactPlan plan = BuildArtifactPlan.Create(Root, new[] { "_Boot/" });

			plan.Directories.Single().Should().Be(Path.Combine(Root, "_Boot"));
		}

		[Fact]
		public void Never_returns_the_root_itself()
		{
			BuildArtifactPlan plan = BuildArtifactPlan.Create(Root, new[] { "/", "", "  " });

			plan.IsEmpty.Should().BeTrue();
		}

		[Fact]
		public void Deletes_a_nested_directory_before_the_one_that_contains_it()
		{
			BuildArtifactPlan plan = BuildArtifactPlan.Create(
				Root,
				new[] { "_Boot/", "_Boot/Plc/", "_Boot/Plc/Port_851/" }
			);

			plan.Directories.Should()
				.ContainInOrder(
					@"C:\proj\_Boot\Plc\Port_851",
					@"C:\proj\_Boot\Plc",
					@"C:\proj\_Boot"
				);
		}

		[Fact]
		public void Normalizes_the_separators_of_an_entry()
		{
			BuildArtifactPlan plan = BuildArtifactPlan.Create(Root, new[] { "_Boot/Plc/a.app" });

			plan.Files.Single().Should().Be(@"C:\proj\_Boot\Plc\a.app");
		}

		[Fact]
		public void Reports_a_duplicate_only_once()
		{
			BuildArtifactPlan plan = BuildArtifactPlan.Create(
				Root,
				new[] { "a.tmc", "a.tmc", "A.TMC" }
			);

			plan.Files.Should().ContainSingle();
		}

		[Fact]
		public void Tolerates_a_missing_list()
		{
			BuildArtifactPlan.Create(Root, null).IsEmpty.Should().BeTrue();
		}

		[Fact]
		public void Rejects_a_missing_root()
		{
			Action act = () => BuildArtifactPlan.Create(null, new[] { "a" });

			act.Should().Throw<ArgumentException>();
		}
	}
}
