using System;
using System.Threading;
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
		protected override void BeforeQueryStatus(EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			ITcSysManager2 systemManager = VS.Solutions.GetActiveTwinCATProjectSystemManager();

			Command.Visible = VS.Solutions.IsTwinCATProjectLoaded();
			Command.Enabled = !string.IsNullOrEmpty(systemManager?.GetTargetNetId());
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			ITcSysManager2 systemManager =
				await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();

			if (systemManager is null)
			{
				await VS.MessageBox.ShowAsync(
					"TwinCAT ProductivityTools",
					"Solution does not contain a TwinCAT XAE project!"
				);
				return;
			}

			string target = systemManager.GetTargetNetId();

			if (!AmsNetId.TryParse(target, out AmsNetId amsNetId))
			{
				await VS.MessageBox.ShowErrorAsync(
					"TwinCAT ProductivityTools",
					"The active TwinCAT project has no valid target AmsNetId."
				);
				return;
			}

			if (
				!await VS.MessageBox.ShowConfirmAsync(
					Vsix.Name,
					"Shutdown Target <" + target + "> ?"
				)
			)
				return;

			try
			{
				await RemoteControl.ShutdownAsync(amsNetId, CancellationToken.None);
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
