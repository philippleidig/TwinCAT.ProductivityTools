using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using TwinCAT.ProductivityTools.Extensions;

namespace TwinCAT.ProductivityTools.Helpers
{
	internal static class RegexPatterns
	{
		public static readonly Regex MatchEmptyRegex = new Regex("$^", RegexOptions.Compiled);
		public static readonly Regex RangeRegex = new Regex(
			@"^((?:[^\[\\]|(?:\\.))*)\[((?:[^\]\\]|(?:\\.))*)\]",
			RegexOptions.Compiled
		);
		public static readonly Regex BackslashRegex = new Regex(@"\\(.)", RegexOptions.Compiled);
		public static readonly Regex SpecialCharactersRegex = new Regex(
			@"[\-\[\]\{\}\(\)\+\.\\\^\$\|]",
			RegexOptions.Compiled
		);
		public static readonly Regex QuestionMarkRegex = new Regex(@"\?", RegexOptions.Compiled);
		public static readonly Regex SlashDoubleAsteriksSlashRegex = new Regex(
			@"\/\*\*\/",
			RegexOptions.Compiled
		);
		public static readonly Regex DoubleAsteriksSlashRegex = new Regex(
			@"^\*\*\/",
			RegexOptions.Compiled
		);
		public static readonly Regex SlashDoubleAsteriksRegex = new Regex(
			@"\/\*\*$",
			RegexOptions.Compiled
		);
		public static readonly Regex DoubleAsteriksRegex = new Regex(
			@"\*\*",
			RegexOptions.Compiled
		);
		public static readonly Regex SlashAsteriksEndOrSlashRegex = new Regex(
			@"\/\*(\/|$)",
			RegexOptions.Compiled
		);
		public static readonly Regex AsteriksRegex = new Regex(@"\*", RegexOptions.Compiled);
		public static readonly Regex SlashRegex = new Regex(@"\/", RegexOptions.Compiled);
	}

	public sealed class FileFilter
	{
		private readonly (Regex Merged, Regex[] Individual) Positives;
		private readonly (Regex Merged, Regex[] Individual) Negatives;

		private FileFilter(IEnumerable<string> filters)
		{
			(Positives, Negatives) = Parse(filters);
		}

		private static (
			(Regex Merged, Regex[] Individual) positives,
			(Regex Merged, Regex[] Individual) negatives
		) Parse(IEnumerable<string> filters)
		{
			(List<string> positive, List<string> negative) = filters
				.Select(line => line.Trim())
				.Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
				.Aggregate<string, (List<string>, List<string>), (List<string>, List<string>)>(
					(new List<string>(), new List<string>()),
					((List<string> positive, List<string> negative) lists, string line) =>
					{
						if (line.StartsWith("!"))
							lists.negative.Add(line.Substring(1));
						else
							lists.positive.Add(line);
						return (lists.positive, lists.negative);
					},
					((List<string> positive, List<string> negative) lists) => lists
				);

			return (Submatch(positive), Submatch(negative));
		}

		private static (Regex Merged, Regex[] Individual) Submatch(List<string> list)
		{
			if (list.Count == 0)
			{
				return (RegexPatterns.MatchEmptyRegex, new Regex[0]);
			}
			else
			{
				var reList = list.OrderBy(str => str).Select(PrepareRegexPattern).ToList();
				return (
					new Regex($"(?:{string.Join(")|(?:", reList)})"),
					reList.Select(s => new Regex(s)).ToArray()
				);
			}
		}

		public static (IEnumerable<string> Accepted, IEnumerable<string> Denied) Parse(
			IEnumerable<string> filters,
			string directoryPath
		)
		{
			FileFilter parser = new FileFilter(filters);
			DirectoryInfo directory = new DirectoryInfo(directoryPath);
			return (parser.Accepted(directory), parser.Denied(directory));
		}

		static List<string> ListFiles(DirectoryInfo directory, string rootPath = "")
		{
			if (rootPath.Length == 0)
				rootPath = directory.FullName;

			List<string> files = new List<string>()
			{
				directory.FullName.Substring(rootPath.Length) + '/'
			};
			foreach (FileInfo file in directory.GetFiles())
				files.Add(file.FullName.Substring(rootPath.Length + 1));

			foreach (DirectoryInfo subDir in directory.GetDirectories())
				files.AddRange(ListFiles(subDir, rootPath));

			return files;
		}

		public bool Accepts(string input)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				input = input.Replace('\\', '/');

			if (!input.StartsWith("/"))
				input = "/" + input;

			var acceptTest = Negatives.Merged.IsMatch(input);
			var denyTest = Positives.Merged.IsMatch(input);
			var returnVal = acceptTest || !denyTest;

			if (acceptTest && denyTest)
			{
				int acceptLength = 0,
					denyLength = 0;
				foreach (var re in Negatives.Individual)
				{
					var m = re.Match(input);
					if (m.Success && acceptLength < m.Value.Length)
					{
						acceptLength = m.Value.Length;
					}
				}
				foreach (var re in Positives.Individual)
				{
					var m = re.Match(input);
					if (m.Success && denyLength < m.Value.Length)
					{
						denyLength = m.Value.Length;
					}
				}
				returnVal = acceptLength >= denyLength;
			}

			return returnVal;
		}

		public IEnumerable<string> Accepted(IEnumerable<string> input)
		{
			return input.Where(f => Accepts(f));
		}

		public IEnumerable<string> Accepted(DirectoryInfo directory)
		{
			var files = ListFiles(directory);
			return files.Where(f => Accepts(f));
		}

		public bool Denies(string input)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				input = input.Replace('\\', '/');

			if (!input.StartsWith("/"))
				input = "/" + input;

			var acceptTest = Negatives.Merged.IsMatch(input);
			var denyTest = Positives.Merged.IsMatch(input);
			var returnVal = !acceptTest && denyTest;

			if (acceptTest && denyTest)
			{
				int acceptLength = 0,
					denyLength = 0;
				foreach (var re in Negatives.Individual)
				{
					var m = re.Match(input);
					if (m.Success && acceptLength < m.Value.Length)
					{
						acceptLength = m.Value.Length;
					}
				}
				foreach (var re in Positives.Individual)
				{
					var m = re.Match(input);
					if (m.Success && denyLength < m.Value.Length)
					{
						denyLength = m.Value.Length;
					}
				}
				returnVal = acceptLength < denyLength;
			}
			return returnVal;
		}

		public IEnumerable<string> Denied(IEnumerable<string> input)
		{
			return input.Where(f => Denies(f));
		}

		public IEnumerable<string> Denied(DirectoryInfo directory)
		{
			var files = ListFiles(directory);
			return files.Where(f => Denies(f));
		}

		public bool Inspects(string input)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				input = input.Replace('\\', '/');

			if (!input.StartsWith("/"))
				input = "/" + input;

			var acceptTest = Negatives.Merged.IsMatch(input);
			var denyTest = Positives.Merged.IsMatch(input);
			var returnVal = acceptTest || denyTest;

			return returnVal;
		}

		private static string PrepareRegexPattern(string pattern)
		{
			var reBuilder = new StringBuilder();
			bool rooted = false,
				directory = false;
			if (pattern.StartsWith("/"))
			{
				rooted = true;
				pattern = pattern.Substring(1);
			}
			if (pattern.EndsWith("/"))
			{
				directory = true;
				pattern = pattern.Substring(0, pattern.Length - 1);
			}

			string transpileRegexPart(string _re)
			{
				if (_re.Length == 0)
					return _re;
				_re = RegexPatterns.BackslashRegex.Replace(_re, "$1");
				_re = RegexPatterns.SpecialCharactersRegex.Replace(_re, @"\$&");
				_re = RegexPatterns.QuestionMarkRegex.Replace(_re, "[^/]");
				_re = RegexPatterns.SlashDoubleAsteriksSlashRegex.Replace(_re, "(?:/|(?:/.+/))");
				_re = RegexPatterns.DoubleAsteriksSlashRegex.Replace(_re, "(?:|(?:.+/))");
				_re = RegexPatterns.SlashDoubleAsteriksRegex.Replace(
					_re,
					_ =>
					{
						directory = true;
						return "(?:|(?:/.+))";
					}
				);
				_re = RegexPatterns.DoubleAsteriksRegex.Replace(_re, ".*");
				_re = RegexPatterns.SlashAsteriksEndOrSlashRegex.Replace(_re, "/[^/]+$1");
				_re = RegexPatterns.AsteriksRegex.Replace(_re, "[^/]*");
				_re = RegexPatterns.SlashRegex.Replace(_re, @"\/");
				return _re;
			}

			Regex rangeRe = RegexPatterns.RangeRegex;
			for (Match match; (match = rangeRe.Match(pattern)).Success; )
			{
				if (match.Groups[1].Value.Contains('/'))
				{
					rooted = true;
				}
				reBuilder.Append(transpileRegexPart(match.Groups[1].Value));
				reBuilder.Append('[').Append(match.Groups[2].Value).Append(']');

				pattern = pattern.Substring(match.Length);
			}
			if (!string.IsNullOrWhiteSpace(pattern))
			{
				if (pattern.Contains('/'))
				{
					rooted = true;
				}
				reBuilder.Append(transpileRegexPart(pattern));
			}

			reBuilder.Preappend(rooted ? @"^\/" : @"\/");
			reBuilder.Append(directory ? @"\/" : @"(?:$|\/)");

			string re = reBuilder.ToString();

			return re;
		}
	}
}
