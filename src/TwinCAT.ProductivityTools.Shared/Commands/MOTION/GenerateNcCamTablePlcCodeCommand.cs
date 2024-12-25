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
	[Command(PackageIds.GenerateCamTablePlcCodeCommandId)]
	internal sealed class GenerateNcCamTablePlcCodeCommand
		: BaseCommand<GenerateNcCamTablePlcCodeCommand>
	{
		protected override void BeforeQueryStatus(EventArgs e)
		{
			var dte = VS.GetRequiredService<DTE, DTE>();
			var selectedItem = dte?.SelectedItems?.Item(1).ProjectItem;

			if (!(selectedItem?.Object is ITcSmTreeItem treeItem))
				return;

			Command.Visible = VS.Solutions.IsTwinCATProjectLoaded() && treeItem.IsNcCamTableSlave();
			Command.Enabled = treeItem.IsNcCamTableSlave();
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var dte = await VS.GetRequiredServiceAsync<DTE, DTE>();
			var selectedItem = dte?.SelectedItems?.Item(1).ProjectItem;

			if (!(selectedItem?.Object is ITcSmTreeItem treeItem))
				return;

			try { }
			catch (Exception ex)
			{
				await VS.MessageBox.ShowErrorAsync(
					"Failed to generate NC cam Table PLC code. See output window for detailed informations."
				);

				IOutputWindowPane outputWindowPane = await VS.GetRequiredServiceAsync<
					IOutputWindowPane,
					IOutputWindowPane
				>();
				await outputWindowPane.WriteLineAsync(ex.Message);
			}
		}
	}
}
