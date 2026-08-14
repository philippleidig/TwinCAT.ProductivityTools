using System.Linq;
using FluentAssertions;
using TwinCAT.ProductivityTools.Helpers;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Helpers
{
	/// <summary>
	/// The filter decides which files "Delete build artifacts on clean" removes, so a wrong match
	/// deletes source code. It follows gitignore semantics.
	/// </summary>
	public class FileFilterTests
	{
		[Theory]
		[InlineData("Untitled1.tpy")]
		[InlineData("PLC/Untitled1.tpy")]
		[InlineData("a/b/c/Untitled1.tpy")]
		public void Matches_an_extension_pattern_at_any_depth(string path)
		{
			Filter("*.tpy").Denies(path).Should().BeTrue();
		}

		[Theory]
		[InlineData("Untitled1.plcproj")]
		[InlineData("Untitled1.tpy.keep")]
		public void Keeps_files_that_do_not_match(string path)
		{
			Filter("*.tpy").Accepts(path).Should().BeTrue();
		}

		[Fact]
		public void Matches_a_directory_pattern_and_everything_below_it()
		{
			FileFilter filter = Filter("_Boot/");

			filter.Denies("_Boot/").Should().BeTrue();
			filter.Denies("_Boot/TwinCAT RT (x64)/Plc/Port_851.app").Should().BeTrue();
		}

		[Fact]
		public void Normalizes_windows_separators()
		{
			Filter("_Boot/").Denies(@"_Boot\TwinCAT RT (x64)\Plc\Port_851.app").Should().BeTrue();
		}

		[Fact]
		public void Understands_a_leading_slash_as_anchored_to_the_root()
		{
			FileFilter filter = Filter("/build/");

			filter.Denies("build/output.tmc").Should().BeTrue();
			filter.Denies("PLC/build/output.tmc").Should().BeFalse();
		}

		[Fact]
		public void Understands_a_question_mark_as_a_single_character()
		{
			FileFilter filter = Filter("Port_85?.app");

			filter.Denies("Port_851.app").Should().BeTrue();
			filter.Denies("Port_8511.app").Should().BeFalse();
		}

		[Fact]
		public void Understands_character_ranges()
		{
			FileFilter filter = Filter("Port_85[0-9].app");

			filter.Denies("Port_851.app").Should().BeTrue();
			filter.Denies("Port_85x.app").Should().BeFalse();
		}

		[Fact]
		public void Understands_a_double_asterisk_as_any_number_of_directories()
		{
			Filter("_Libraries/**/*.library").Denies("_Libraries/system/tc2/x.library").Should().BeTrue();
		}

		[Fact]
		public void Lets_a_negation_win_over_a_broader_pattern()
		{
			FileFilter filter = Filter("*.library", "!keep.library");

			filter.Denies("x.library").Should().BeTrue();
			filter.Accepts("keep.library").Should().BeTrue();
		}

		[Fact]
		public void Ignores_comments_and_blank_lines()
		{
			FileFilter filter = Filter("# artifacts", string.Empty, "   ", "*.tpy");

			filter.Denies("x.tpy").Should().BeTrue();
			filter.Denies("# artifacts").Should().BeFalse();
		}

		[Fact]
		public void Accepts_everything_when_no_pattern_is_configured()
		{
			FileFilter filter = Filter();

			filter.Accepts("anything.plcproj").Should().BeTrue();
			filter.Denies("anything.plcproj").Should().BeFalse();
		}

		[Fact]
		public void Reports_whether_a_pattern_looked_at_a_path_at_all()
		{
			FileFilter filter = Filter("*.tpy");

			filter.Inspects("x.tpy").Should().BeTrue();
			filter.Inspects("x.plcproj").Should().BeFalse();
		}

		[Fact]
		public void Accepts_and_denies_are_opposites_for_a_simple_filter()
		{
			FileFilter filter = Filter("*.tpy");

			filter.Accepts("x.tpy").Should().Be(!filter.Denies("x.tpy"));
			filter.Accepts("x.plcproj").Should().Be(!filter.Denies("x.plcproj"));
		}

		[Fact]
		public void Filters_a_list_of_paths()
		{
			string[] files = { "MAIN.TcPOU", "Untitled1.tpy", "_Boot/x.app" };

			FileFilter filter = Filter("*.tpy", "_Boot/");

			filter.Denied(files).Should().BeEquivalentTo("Untitled1.tpy", "_Boot/x.app");
			filter.Accepted(files).Should().BeEquivalentTo("MAIN.TcPOU");
		}

		[Fact]
		public void The_shipped_default_filter_denies_the_generated_twincat_files()
		{
			FileFilter filter = new FileFilter(new FileFilterProvider());

			filter.Denies("Untitled1.tpy").Should().BeTrue();
			filter.Denies("_Boot/TwinCAT RT (x64)/Plc/Port_851.app").Should().BeTrue();
			filter.Denies("_CompileInfo/Untitled1.compileinfo").Should().BeTrue();
			filter.Denies(".vs/slnx.sqlite").Should().BeTrue();
		}

		[Fact]
		public void The_shipped_default_filter_keeps_the_sources()
		{
			FileFilter filter = new FileFilter(new FileFilterProvider());

			filter.Accepts("Untitled1.plcproj").Should().BeTrue();
			filter.Accepts("Untitled1.tsproj").Should().BeTrue();
			filter.Accepts("POUs/MAIN.TcPOU").Should().BeTrue();
			filter.Accepts("Untitled1.library").Should().BeTrue();
		}

		private static FileFilter Filter(params string[] patterns) =>
			new FileFilter(patterns.ToList());
	}
}
