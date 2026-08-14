using FluentAssertions;
using TwinCAT.ProductivityTools.Plc;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Plc
{
	public class RegionRemoverTests
	{
		private readonly RegionRemover remover = new RegionRemover();

		[Fact]
		public void Removes_the_region_markers_and_keeps_the_code_inside()
		{
			string text = "{region \"init\"}\r\nnValue := 1;\r\n{endregion}";

			remover.Remove(text).Should().Be("nValue := 1;");
		}

		[Fact]
		public void Removes_indented_markers()
		{
			string text = "\t{region \"init\"}\r\n\tnValue := 1;\r\n\t{endregion}";

			remover.Remove(text).Should().Be("\tnValue := 1;");
		}

		[Fact]
		public void Removes_nested_regions()
		{
			string text =
				"{region \"outer\"}\r\n{region \"inner\"}\r\na := 1;\r\n{endregion}\r\n{endregion}";

			remover.Remove(text).Should().Be("a := 1;");
		}

		[Fact]
		public void Accepts_single_quoted_region_names()
		{
			remover.Remove("{region 'init'}\r\na := 1;").Should().Be("a := 1;");
		}

		[Fact]
		public void Ignores_case_of_the_markers()
		{
			remover.Remove("{REGION \"init\"}\r\na := 1;\r\n{EndRegion}").Should().Be("a := 1;");
		}

		[Fact]
		public void Keeps_other_pragmas()
		{
			string text = "{attribute 'hide'}\r\na := 1;";

			remover.Remove(text).Should().Be(text);
		}

		[Fact]
		public void Keeps_a_line_that_only_mentions_a_region_in_code()
		{
			string text = "sText := '{region \"x\"}';";

			remover.Remove(text).Should().Be(text);
		}

		[Fact]
		public void Preserves_unix_line_endings()
		{
			remover.Remove("{region \"a\"}\na := 1;\n{endregion}").Should().Be("a := 1;");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		public void Passes_empty_input_through(string text)
		{
			remover.Remove(text).Should().Be(text);
		}
	}
}
