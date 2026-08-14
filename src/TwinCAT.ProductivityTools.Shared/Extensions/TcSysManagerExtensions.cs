using System;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Installation;

namespace TwinCAT.ProductivityTools.Extensions
{
	internal static class TcSysManagerExtensions
	{
		public static bool IsUseRelativeNetIdsEnabled(this ITcSysManager systemManager)
		{
			ITcSmTreeItem routing = systemManager?.LookupTreeItem(RoutingXml.RoutingTreeItemPath);

			return routing != null && RoutingXml.IsUseRelativeNetIdsEnabled(routing.ProduceXml());
		}

		public static void EnableUseRelativeNetIds(this ITcSysManager systemManager)
		{
			ITcSmTreeItem routing = systemManager?.LookupTreeItem(RoutingXml.RoutingTreeItemPath);

			routing?.ConsumeXml(RoutingXml.EnableUseRelativeNetIds());
		}
	}
}
