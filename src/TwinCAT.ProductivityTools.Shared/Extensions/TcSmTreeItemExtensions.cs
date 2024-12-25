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

		public static bool IsPlcProjectFolder(this ITcSmTreeItem treeItem)
		{
			return treeItem.ItemType == (int)TCatSysManagerLib.TREEITEMTYPES.TREEITEMTYPE_PLCFOLDER;
		}

		public static bool IsPlcProject(this ITcSmTreeItem treeItem)
		{
			return treeItem.ItemType == (int)TCatSysManagerLib.TREEITEMTYPES.TREEITEMTYPE_PLCAPP;
		}

		public static bool IsPlcFunctionBlock(this ITcSmTreeItem treeItem)
		{
			return treeItem.ItemType == (int)TCatSysManagerLib.TREEITEMTYPES.TREEITEMTYPE_PLCPOUFB;
		}

		public static bool IsPlcTask(this ITcSmTreeItem treeItem)
		{
			return treeItem.ItemType == (int)TCatSysManagerLib.TREEITEMTYPES.TREEITEMTYPE_PLCTASK;
		}
	}
}
