using System;
using System.Collections.Generic;
using System.Text;
using TCatSysManagerLib;

namespace TwinCAT.ProductivityTools.Extensions
{
	internal static class TcSmTreeItemExtensions
	{
		public static bool IsEtherCATMaster(this ITcSmTreeItem treeItem)
		{
			return treeItem.ItemType == 2 && treeItem.ItemSubType == 111;
		}

		public static bool IsEtherCATMasterProcessImage(this ITcSmTreeItem treeItem)
		{
			return treeItem.ItemType == 3 && treeItem.ItemSubType == 3;
		}

		public static bool IsNcCamTableSlave(this ITcSmTreeItem treeItem)
		{
			return treeItem.ItemType == 42;
		}
	}
}
