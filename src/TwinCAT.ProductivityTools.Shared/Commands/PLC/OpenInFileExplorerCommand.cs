using System;
using System.Diagnostics;
using System.IO;
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
	/// Opens the directory of the selected PLC folder in the Windows file explorer.
	/// </summary>
	[Command(PackageIds.OpenInFileExplorerCommandId)]
	internal sealed class OpenInFileExplorerCommand : BaseCommand<OpenInFileExplorerCommand>
	{
		protected override void BeforeQueryStatus(EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			bool isPlcProjectFolder = false;

			try
			{
				DTE dte = VS.GetRequiredService<DTE, DTE>();
				isPlcProjectFolder = dte.GetSelectedObject<ITcSmTreeItem>().IsPlcProjectFolder();
			}
			catch (Exception)
			{
				// An unreadable selection hides the command instead of breaking the context menu.
			}

			Command.Visible = isPlcProjectFolder;
			Command.Enabled = isPlcProjectFolder;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			DTE dte = await VS.GetRequiredServiceAsync<DTE, DTE>();

			string path = dte.GetSelectedProjectItem().GetFullPath();

			if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
			{
				await VS.MessageBox.ShowErrorAsync(
					Vsix.Name,
					"The selected item does not have a directory on disk."
				);
				return;
			}

			try
			{
				// The path is quoted because the explorer would otherwise treat a space as a
				// separator between two arguments.
				using (System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"")) { }
			}
			catch (Exception ex)
			{
				await Report.FailureAsync("Failed to open the file explorer.", ex);
			}
		}
	}
}
