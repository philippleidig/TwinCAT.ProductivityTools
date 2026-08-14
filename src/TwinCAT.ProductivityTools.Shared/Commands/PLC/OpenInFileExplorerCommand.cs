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
			ThreadHelper.ThrowIfNotOnUIThread();

			var dte = VS.GetRequiredService<DTE, DTE>();
			ITcSmTreeItem treeItem = dte.GetSelectedObject<ITcSmTreeItem>();

			bool isPlcProjectFolder = treeItem != null && treeItem.IsPlcProjectFolder();

			Command.Visible = VS.Solutions.IsTwinCATProjectLoaded() && isPlcProjectFolder;
			Command.Enabled = isPlcProjectFolder;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var dte = VS.GetRequiredService<DTE, DTE>();
			ProjectItem selectedItem = dte.GetSelectedProjectItem();

			string filePath = selectedItem?.Properties?.Item("FullPath")?.Value?.ToString();

			if (string.IsNullOrEmpty(filePath) || !Directory.Exists(filePath))
			{
				return;
			}

			System.Diagnostics.Process.Start("explorer.exe", filePath);
		}
	}
}
