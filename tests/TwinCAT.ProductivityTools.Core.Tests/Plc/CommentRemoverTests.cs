using FluentAssertions;
using TwinCAT.ProductivityTools.Plc;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Plc
{
	public class CommentRemoverTests
	{
		private readonly CommentRemover remover = new CommentRemover();

		[Fact]
		public void Removes_a_trailing_line_comment_and_the_whitespace_before_it()
		{
			remover
				.Remove("nCounter := nCounter + 1; // increment")
				.Should()
				.Be("nCounter := nCounter + 1;");
		}

		[Fact]
		public void Keeps_the_line_of_a_full_line_comment_so_that_line_numbers_stay_stable()
		{
			remover.Remove("// header\r\nbDone := TRUE;").Should().Be("\r\nbDone := TRUE;");
		}

		[Fact]
		public void Removes_a_block_comment_that_stays_on_one_line()
		{
			remover.Remove("a := 1; (* why *) b := 2;").Should().Be("a := 1;  b := 2;");
		}

		[Fact]
		public void Removes_a_block_comment_that_spans_several_lines_completely()
		{
			string text = "a := 1;\r\n(* first\r\nsecond *)\r\nb := 2;";

			remover.Remove(text).Should().Be("a := 1;\r\n\r\n\r\nb := 2;");
		}

		[Fact]
		public void Keeps_the_code_that_surrounds_a_multi_line_block_comment()
		{
			string text = "a := 1; (* start\r\nend *) b := 2;";

			remover.Remove(text).Should().Be("a := 1;\r\n b := 2;");
		}

		[Fact]
		public void Keeps_the_line_count_so_that_reported_line_numbers_stay_correct()
		{
			string text = "a := 1;\r\n(* one\r\ntwo\r\nthree *)\r\nb := 2;";

			remover.Remove(text).Split('\n').Should().HaveCount(text.Split('\n').Length);
		}

		[Fact]
		public void Removes_nested_block_comments_as_one_unit()
		{
			remover
				.Remove("a := 1; (* outer (* inner *) still comment *) b := 2;")
				.Should()
				.Be("a := 1;  b := 2;");
		}

		[Fact]
		public void Keeps_comment_markers_that_belong_to_a_string_literal()
		{
			remover.Remove("sText := 'a // b (* c *)';").Should().Be("sText := 'a // b (* c *)';");
		}

		[Fact]
		public void Keeps_comment_markers_inside_a_double_quoted_literal()
		{
			remover
				.Remove("wsText := \"// not a comment\";")
				.Should()
				.Be("wsText := \"// not a comment\";");
		}

		[Fact]
		public void Honours_the_dollar_escape_of_structured_text_string_literals()
		{
			remover.Remove("sText := 'it$'s'; // gone").Should().Be("sText := 'it$'s';");
		}

		[Fact]
		public void Preserves_unix_line_endings()
		{
			remover.Remove("a := 1; // x\nb := 2;").Should().Be("a := 1;\nb := 2;");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		public void Passes_empty_input_through(string text)
		{
			remover.Remove(text).Should().Be(text);
		}

		[Fact]
		public void Leaves_code_without_comments_untouched()
		{
			string text = "IF bEnable THEN\r\n\tnValue := 42;\r\nEND_IF";

			remover.Remove(text).Should().Be(text);
		}

		[Fact]
		public void Removes_the_rest_of_an_unterminated_block_comment()
		{
			remover.Remove("a := 1;\r\n(* forgot to close").Should().Be("a := 1;\r\n");
		}
	}
}
