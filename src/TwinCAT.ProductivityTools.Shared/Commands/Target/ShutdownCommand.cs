using System;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Extensions;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	[Command(PackageIds.ShutdownCommandId)]
	internal sealed class ShutdownCommand : BaseCommand<ShutdownCommand>
	{
		private ITargetSystemService targetSystemService;

		protected override async void BeforeQueryStatus(EventArgs e)
		{
			ITcSysManager2 systemManager =
				await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();

			Command.Visible = VS.Solutions.IsTwinCATProjectLoaded();
			Command.Enabled = AmsNetId.Local.ToString() == systemManager.GetTargetNetId();
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			if (targetSystemService is null)
			{
				return;
			}

			AmsNetId target = targetSystemService.ActiveTargetSystem;

			if (
				!await VS.MessageBox.ShowConfirmAsync(
					Vsix.Name,
					"Shutdown Target <" + target + "> ?"
				)
			)
				return;

			try
			{
				await targetSystemService.ShutdownAsync();
				await VS.StatusBar.ShowMessageAsync(
					"Shutdown successfully on target <" + target + ">"
				);
			}
			catch (Exception ex)
			{
				await VS.MessageBox.ShowErrorAsync(
					"Shutdown failed on target <"
						+ target
						+ ">. See output window for more information"
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
