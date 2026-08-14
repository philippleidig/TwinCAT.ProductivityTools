using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TwinCAT.ProductivityTools.Plc
{
	/// <summary>
	/// Removes comments from Structured Text.
	/// </summary>
	public interface ICommentRemover
	{
		string Remove(string text);
	}

	/// <summary>
	/// Strips <c>//</c> line comments and <c>(* ... *)</c> block comments from Structured Text.
	/// </summary>
	/// <remarks>
	/// Implemented as a single pass scanner instead of a regular expression because Structured Text
	/// allows nested block comments and uses <c>$</c> as the escape character inside string
	/// literals. Comment markers that appear inside a string literal are code, not comments, and
	/// have to survive untouched.
	/// </remarks>
	public sealed class CommentRemover : ICommentRemover
	{
		public string Remove(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return text;
			}

			var result = new StringBuilder(text.Length);
			int index = 0;
			int blockCommentDepth = 0;

			while (index < text.Length)
			{
				char current = text[index];

				if (blockCommentDepth > 0)
				{
					if (StartsWith(text, index, "(*"))
					{
						blockCommentDepth++;
						index += 2;
					}
					else if (StartsWith(text, index, "*)"))
					{
						blockCommentDepth--;
						index += 2;
					}
					else
					{
						index++;
					}

					continue;
				}

				if (StartsWith(text, index, "(*"))
				{
					blockCommentDepth = 1;
					index += 2;
					continue;
				}

				if (StartsWith(text, index, "//"))
				{
					while (index < text.Length && text[index] != '\n' && text[index] != '\r')
					{
						index++;
					}

					continue;
				}

				if (current == '"' || current == '\'')
				{
					index = CopyStringLiteral(text, index, result);
					continue;
				}

				result.Append(current);
				index++;
			}

			return TrimLineEnds(result.ToString(), LineEndings.Detect(text));
		}

		/// <summary>
		/// Copies a string literal verbatim, honouring the <c>$</c> escape so that a literal such as
		/// <c>'it$'s'</c> is not terminated too early.
		/// </summary>
		private static int CopyStringLiteral(string text, int index, StringBuilder result)
		{
			char quote = text[index];

			result.Append(quote);
			index++;

			while (index < text.Length)
			{
				char current = text[index];

				result.Append(current);
				index++;

				if (current == '$' && index < text.Length)
				{
					result.Append(text[index]);
					index++;
					continue;
				}

				if (current == quote || current == '\n')
				{
					break;
				}
			}

			return index;
		}

		/// <summary>
		/// Removes the whitespace a stripped trailing comment leaves behind, without collapsing the
		/// blank lines that keep the remaining code on recognisable line numbers.
		/// </summary>
		private static string TrimLineEnds(string text, string lineEnding)
		{
			IEnumerable<string> lines = text.Split(new[] { lineEnding }, StringSplitOptions.None)
				.Select(line => line.TrimEnd());

			return string.Join(lineEnding, lines);
		}

		private static bool StartsWith(string text, int index, string value) =>
			index + value.Length <= text.Length
			&& string.CompareOrdinal(text, index, value, 0, value.Length) == 0;
	}
}
