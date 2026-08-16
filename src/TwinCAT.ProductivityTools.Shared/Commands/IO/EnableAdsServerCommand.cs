using System;
using System.Collections.Generic;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Helpers;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>
	/// Publishes the process image of an EtherCAT master through an ADS server so that the image
	/// can be read by name from outside the PLC.
	/// </summary>
	[Command(PackageIds.EnableAdsServerCommandId)]
	internal sealed class EnableAdsServerCommand : BaseCommand<EnableAdsServerCommand>
	{
		protected override void BeforeQueryStatus(EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			bool isEtherCATMaster = false;

			try
			{
				DTE dte = VS.GetRequiredService<DTE, DTE>();
				isEtherCATMaster = dte.GetSelectedObject<ITcSmTreeItem>().IsEtherCATMaster();
			}
			catch (Exception)
			{
				// An unreadable selection hides the command instead of breaking the context menu.
			}

			Command.Visible = isEtherCATMaster;
			Command.Enabled = isEtherCATMaster;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			DTE dte = await VS.GetRequiredServiceAsync<DTE, DTE>();
			ProjectItem selectedItem = dte.GetSelectedProjectItem();

			if (!(selectedItem?.Object is ITcSmTreeItem treeItem) || !treeItem.IsEtherCATMaster())
			{
				return;
			}

			try
			{
				int enabled;

				using (UndoContextScope.Open(dte, "Enable ADS server"))
				{
					enabled = EnableAdsServer(treeItem);
				}

				selectedItem.Save();

				await Report.ShowStatusAsync(
					enabled == 0
						? "No process image was found below the selected EtherCAT master."
						: $"ADS server enabled for {enabled} process image(s)."
				);
			}
			catch (Exception ex)
			{
				await Report.FailureAsync("Failed to enable the ADS server.", ex);
			}
		}

		private static int EnableAdsServer(ITcSmTreeItem master)
		{
			int enabled = 0;

			foreach (ITcSmTreeItem6 processImage in ProcessImagesOf(master))
			{
				uint adsPort = FirstAdsPort + (uint)processImage.TreeItemId;

				((ITcSmTreeItem)processImage).ConsumeXml(
					"<TreeItem><ImageDef><AdsServer>"
						+ $"<Port>{adsPort}</Port><CreateSymbols>true</CreateSymbols>"
						+ "</AdsServer></ImageDef></TreeItem>"
				);

				enabled++;
			}

			return enabled;
		}

		private static IEnumerable<ITcSmTreeItem6> ProcessImagesOf(ITcSmTreeItem master)
		{
			List<ITcSmTreeItem6> processImages = new List<ITcSmTreeItem6>();

			foreach (ITcSmTreeItem child in master)
			{
				if (child.IsEtherCATMasterProcessImage() && child is ITcSmTreeItem6 processImage)
				{
					processImages.Add(processImage);
				}
			}

			return processImages;
		}

		/// <summary>
		/// Base of the ADS port range TwinCAT reserves for image servers. The tree item id keeps
		/// the ports of several images apart.
		/// </summary>
		private const uint FirstAdsPort = 27904U;
	}
}
