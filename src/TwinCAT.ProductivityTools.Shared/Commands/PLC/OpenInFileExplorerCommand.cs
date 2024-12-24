using System;
using System.IO;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Extensions;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands.PLC
{
	[Command(PackageIds.OpenInFileExplorerCommandId)]
	internal class OpenInFileExplorerCommand : BaseCommand<OpenInFileExplorerCommand>
	{
		protected override void BeforeQueryStatus(EventArgs e)
		{
			var dte = VS.GetRequiredService<DTE, DTE>();
			var selectedItem = dte?.SelectedItems?.Item(1).ProjectItem;

			if (!(selectedItem?.Object is ITcSmTreeItem treeItem))
			{
				return;
			}

			Command.Visible =
				VS.Solutions.IsTwinCATProjectLoaded() && treeItem.IsPlcProjectFolder();
			Command.Enabled = treeItem.IsPlcProjectFolder();
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var dte = VS.GetRequiredService<DTE, DTE>();
			var selectedItem = dte?.SelectedItems?.Item(1).ProjectItem;

			string filePath = selectedItem.Properties.Item("FullPath").Value.ToString();

			if (!Directory.Exists(filePath))
			{
				return;
			}

			System.Diagnostics.Process.Start("explorer.exe", filePath);
		}
	}
}
