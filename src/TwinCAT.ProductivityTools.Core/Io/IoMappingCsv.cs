using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TwinCAT.ProductivityTools.Io
{
	/// <summary>
	/// Renders parsed I/O mappings as comma separated values so that they can be reviewed outside
	/// the engineering environment - TwinCAT XAE itself offers no way to export the link list.
	/// </summary>
	public static class IoMappingCsv
	{
		public const string Header = "Owner;Variable;Path;Size;Offset";

		public static string Build(IDictionary<string, List<Variable>> mappings)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine(Header);

			if (mappings == null)
			{
				return builder.ToString();
			}

			foreach (var mapping in mappings.OrderBy(m => m.Key, StringComparer.OrdinalIgnoreCase))
			{
				string owner = (mapping.Key ?? string.Empty).Replace("^", " / ");

				foreach (Variable variable in mapping.Value ?? new List<Variable>())
				{
					if (variable == null)
					{
						continue;
					}

					builder.AppendLine(
						string.Join(
							";",
							Escape(owner),
							Escape(variable.Name),
							Escape(variable.Path),
							variable.Size.ToString(CultureInfo.InvariantCulture),
							variable.Offset.ToString(CultureInfo.InvariantCulture)
						)
					);
				}
			}

			return builder.ToString();
		}

		/// <summary>
		/// Quotes a value when it carries a character that would otherwise start a new field or a
		/// new record. A quote inside a quoted field is doubled, as defined by RFC 4180.
		/// </summary>
		private static string Escape(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return string.Empty;
			}

			bool needsQuotes =
				value.IndexOfAny(new[] { ';', '"', '\r', '\n' }) >= 0 || value != value.Trim();

			return needsQuotes ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
		}
	}
}
