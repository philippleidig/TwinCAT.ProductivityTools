using System;
using System.Linq;
using System.Xml.Linq;

namespace TwinCAT.ProductivityTools.Installation
{
	/// <summary>
	/// Reads and writes the routing settings of a TwinCAT XAE project.
	/// </summary>
	/// <remarks>
	/// The System Manager exposes the routing node only as an XML document
	/// (<c>ProduceXml</c>/<c>ConsumeXml</c>). Keeping the document handling here makes the behaviour
	/// verifiable without an XAE instance.
	/// </remarks>
	public static class RoutingXml
	{
		/// <summary>Tree item path of the routing node inside an XAE project.</summary>
		public const string RoutingTreeItemPath = "TIRR";

		/// <summary>
		/// Returns whether the project stores relative NetIDs. Anything that is not a well formed
		/// document carrying an explicit <c>true</c> counts as disabled, which matches how the
		/// System Manager treats a missing setting.
		/// </summary>
		public static bool IsUseRelativeNetIdsEnabled(string xml)
		{
			if (string.IsNullOrWhiteSpace(xml))
			{
				return false;
			}

			XElement setting;

			try
			{
				setting = XDocument.Parse(xml).Descendants("UseRelativeNetIds").FirstOrDefault();
			}
			catch (System.Xml.XmlException)
			{
				return false;
			}

			if (setting == null)
			{
				return false;
			}

			string value = setting.Value.Trim();

			return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
		}

		/// <summary>
		/// Builds the partial document that switches the project to relative NetIDs.
		/// </summary>
		public static string EnableUseRelativeNetIds() =>
			"<TreeItem><RoutePrj><UseRelativeNetIds>true</UseRelativeNetIds></RoutePrj></TreeItem>";
	}
}
