using System;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
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

		protected override void BeforeQueryStatus(EventArgs e)
		{
			targetSystemService = VS.GetRequiredService<
				ITargetSystemService,
				ITargetSystemService
			>();

			Command.Visible = VS.Solutions.IsTwinCATProjectLoaded();
			Command.Enabled = !targetSystemService.ActiveTargetSystem.IsLocal;
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
