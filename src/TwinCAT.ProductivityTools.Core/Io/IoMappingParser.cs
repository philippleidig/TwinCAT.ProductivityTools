using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace TwinCAT.ProductivityTools.Io
{
	/// <summary>
	/// One end of an I/O link.
	/// </summary>
	public class Variable
	{
		public string Name { get; set; } = string.Empty;
		public string Path { get; set; } = string.Empty;
		public int Size { get; set; }
		public int Offset { get; set; }
	}

	/// <summary>
	/// Turns the mapping information of a TwinCAT configuration into linked variables.
	/// </summary>
	public interface IIoMappingParser
	{
		IDictionary<string, List<Variable>> Parse(string mappingInfoXml);
	}

	/// <summary>
	/// Parses the document produced by <c>ITcSysManager3.ProduceMappingInfo</c>.
	/// </summary>
	/// <remarks>
	/// The document nests <c>OwnerA</c> (usually the PLC side) over <c>OwnerB</c> (usually the
	/// fieldbus side) over the individual <c>Link</c> elements. The result is keyed by the owner A
	/// variable so that the tool window can group every link of a variable.
	/// </remarks>
	public sealed class IoMappingParser : IIoMappingParser
	{
		public IDictionary<string, List<Variable>> Parse(string mappingInfoXml)
		{
			var result = new Dictionary<string, List<Variable>>(StringComparer.OrdinalIgnoreCase);

			if (string.IsNullOrWhiteSpace(mappingInfoXml))
			{
				return result;
			}

			XDocument document;

			try
			{
				document = XDocument.Parse(mappingInfoXml);
			}
			catch (System.Xml.XmlException)
			{
				return result;
			}

			foreach (XElement ownerA in document.Descendants("OwnerA"))
			{
				string ownerAName = Attribute(ownerA, "Name");

				foreach (XElement ownerB in ownerA.Elements("OwnerB"))
				{
					string ownerBName = Attribute(ownerB, "Name");

					foreach (XElement link in ownerB.Elements("Link"))
					{
						string variableA = Attribute(link, "VarA");
						string variableB = Attribute(link, "VarB");
						string key = $"{ownerAName}^{variableA}";

						if (!result.TryGetValue(key, out List<Variable> variables))
						{
							variables = new List<Variable>();
							result[key] = variables;
						}

						variables.Add(
							new Variable
							{
								Name = $"{ownerBName}^{variableB}",
								Path = variableB,
								Size = Number(link, "Size"),
								Offset = Number(link, "OffsA", "OffsB"),
							}
						);
					}
				}
			}

			return result;
		}

		private static string Attribute(XElement element, string name) =>
			element.Attribute(name)?.Value ?? string.Empty;

		/// <summary>
		/// Reads the first attribute that is present and numeric. Mapping documents omit offsets for
		/// links that are not byte aligned, and a non numeric value must not take the tool window
		/// down.
		/// </summary>
		private static int Number(XElement element, params string[] names)
		{
			foreach (string name in names)
			{
				string value = element.Attribute(name)?.Value;

				if (
					value != null
					&& int.TryParse(
						value,
						NumberStyles.Integer,
						CultureInfo.InvariantCulture,
						out int number
					)
				)
				{
					return number;
				}
			}

			return 0;
		}
	}
}
