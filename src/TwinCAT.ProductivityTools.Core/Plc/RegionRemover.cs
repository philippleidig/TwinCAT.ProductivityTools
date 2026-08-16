using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TwinCAT.ProductivityTools.Plc
{
	/// <summary>
	/// Removes the folding region pragmas from Structured Text.
	/// </summary>
	public interface IRegionRemover
	{
		string Remove(string text);
	}

	/// <summary>
	/// Drops <c>{region "..."}</c> and <c>{endregion}</c> lines, including the ones that are
	/// indented, because the code inside a region keeps its own indentation.
	/// </summary>
	public sealed class RegionRemover : IRegionRemover
	{
		private static readonly Regex RegionPattern = new Regex(
			@"^\s*\{\s*region\b[^}]*\}\s*$",
			RegexOptions.Compiled | RegexOptions.IgnoreCase
		);

		private static readonly Regex EndRegionPattern = new Regex(
			@"^\s*\{\s*endregion\s*\}\s*$",
			RegexOptions.Compiled | RegexOptions.IgnoreCase
		);

		public string Remove(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return text;
			}

			string lineEnding = LineEndings.Detect(text);

			IEnumerable<string> lines = text.Split(new[] { lineEnding }, StringSplitOptions.None)
				.Where(line => !IsRegionMarker(line));

			return string.Join(lineEnding, lines);
		}

		public static bool IsRegionMarker(string line) =>
			RegionPattern.IsMatch(line) || EndRegionPattern.IsMatch(line);
	}
}
