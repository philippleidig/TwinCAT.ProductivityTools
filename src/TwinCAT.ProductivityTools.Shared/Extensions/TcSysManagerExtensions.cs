using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using EnvDTE;
using TCatSysManagerLib;

namespace TwinCAT.ProductivityTools.Extensions
{
	internal static class TcSysManagerExtensions
	{
		public static bool IsUseRelativeNetIdsEnabled(this ITcSysManager systemManager)
		{
			ITcSmTreeItem routing = systemManager.LookupTreeItem("TIRR"); // Routing

			// "<TreeItem><RoutePrj><UseRelativeNetIds>true</UseRelativeNetIds><RoutePrj><TreeItem>";
			string xml = routing.ProduceXml();

			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(xml);

			XmlNode useRelativeNetIdsNode = xmlDocument.SelectSingleNode("//UseRelativeNetIds");

			if (useRelativeNetIdsNode == null)
			{
				return false;
			}

			string value = useRelativeNetIdsNode.InnerText;
			return bool.Parse(value);
		}

		public static void EnableUseRelativeNetIds(this ITcSysManager systemManager)
		{
			ITcSmTreeItem routing = systemManager.LookupTreeItem("TIRR"); // Routing

			string xml =
				"<TreeItem><RoutePrj><UseRelativeNetIds>true</UseRelativeNetIds></RoutePrj></TreeItem>";
			routing.ConsumeXml(xml);
		}
	}
}
