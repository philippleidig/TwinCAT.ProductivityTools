using TCatSysManagerLib;

namespace TwinCAT.ProductivityTools.Extensions
{
	/// <summary>
	/// Type checks for the items of the TwinCAT system manager tree.
	/// </summary>
	/// <remarks>
	/// Every check tolerates <c>null</c> and a failing COM call. They are used from
	/// <c>BeforeQueryStatus</c>, where an exception would break the whole context menu of Visual
	/// Studio, and the underlying objects do throw while a project is loading or unloading.
	/// </remarks>
	internal static class TcSmTreeItemExtensions
	{
		public static bool IsEtherCATMaster(this ITcSmTreeItem treeItem)
		{
			return Is(treeItem, EtherCATMasterItemType, EtherCATMasterItemSubType);
		}

		public static bool IsEtherCATMasterProcessImage(this ITcSmTreeItem treeItem)
		{
			return Is(treeItem, ProcessImageItemType, ProcessImageItemSubType);
		}

		public static bool IsPlcProjectFolder(this ITcSmTreeItem treeItem)
		{
			return Is(treeItem, (int)TREEITEMTYPES.TREEITEMTYPE_PLCFOLDER);
		}

		public static bool IsPlcProject(this ITcSmTreeItem treeItem)
		{
			return Is(treeItem, (int)TREEITEMTYPES.TREEITEMTYPE_PLCAPP);
		}

		public static bool IsPlcFunctionBlock(this ITcSmTreeItem treeItem)
		{
			return Is(treeItem, (int)TREEITEMTYPES.TREEITEMTYPE_PLCPOUFB);
		}

		public static bool IsPlcTask(this ITcSmTreeItem treeItem)
		{
			return Is(treeItem, (int)TREEITEMTYPES.TREEITEMTYPE_PLCTASK);
		}

		/// <summary>
		/// The node that represents a PLC project inside the "PLC" folder of the solution tree.
		/// Its nested project holds the actual code.
		/// </summary>
		public static bool IsPlcProjectRoot(this ITcSmTreeItem treeItem)
		{
			return Is(treeItem, (int)TREEITEMTYPES.TREEITEMTYPE_PLCPROJECTDEF);
		}

		/// <summary>
		/// Tells whether structured text can be rewritten below this item. That is the case for
		/// every object that carries text itself and for the containers that hold such objects.
		/// </summary>
		public static bool CarriesPlcText(this ITcSmTreeItem treeItem)
		{
			if (treeItem == null)
			{
				return false;
			}

			return treeItem is ITcPlcDeclaration
				|| treeItem is ITcPlcImplementation
				|| treeItem.IsPlcProjectFolder()
				|| treeItem.IsPlcProject()
				|| treeItem.IsPlcProjectRoot();
		}

		private static bool Is(ITcSmTreeItem treeItem, int itemType)
		{
			if (treeItem == null)
			{
				return false;
			}

			try
			{
				return treeItem.ItemType == itemType;
			}
			catch (System.Exception)
			{
				return false;
			}
		}

		private static bool Is(ITcSmTreeItem treeItem, int itemType, int itemSubType)
		{
			if (treeItem == null)
			{
				return false;
			}

			try
			{
				return treeItem.ItemType == itemType && treeItem.ItemSubType == itemSubType;
			}
			catch (System.Exception)
			{
				return false;
			}
		}

		private const int EtherCATMasterItemType = 2;
		private const int EtherCATMasterItemSubType = 111;
		private const int ProcessImageItemType = 3;
		private const int ProcessImageItemSubType = 3;
	}
}
