using System;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Extensions;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	[Command(PackageIds.EnableAdsServerCommandId)]
	internal class EnableAdsServerCommand : BaseCommand<EnableAdsServerCommand>
	{
		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			EnvDTE.DTE dte = await VS.GetRequiredServiceAsync<DTE, DTE>();
			EnvDTE.ProjectItem selectedItem = dte?.SelectedItems?.Item(1).ProjectItem;

			if (selectedItem == null || !(selectedItem.Object is ITcSmTreeItem treeItem))
				return;

			try
			{
				if (treeItem.IsEtherCATMaster())
				{
					foreach (ITcSmTreeItem6 child in treeItem)
					{
						if (child.IsEtherCATMasterProcessImage())
						{
							uint adsPort = 27904U + (uint)child.TreeItemId;
							string xml = string.Format(
								"<TreeItem><ImageDef><AdsServer><Port>{0}</Port><CreateSymbols>true</CreateSymbols></AdsServer></ImageDef></TreeItem>",
								adsPort
							);
							child.ConsumeXml(xml);
						}
					}

					selectedItem.Save();
				}
			}
			catch (Exception ex)
			{
				await VS.StatusBar.ShowMessageAsync($"");

				IOutputWindowPane outputWindowPane = await VS.GetRequiredServiceAsync<
					IOutputWindowPane,
					IOutputWindowPane
				>();
				await outputWindowPane.WriteLineAsync(ex.Message);
			}
			finally
			{
				dte.UndoContext.Close();
			}
		}
	}
}
