using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
	[Command(PackageIds.RestartCommandId)]
	internal sealed class RebootCommand : BaseCommand<RebootCommand>
	{
		protected override async void BeforeQueryStatus(EventArgs e)
		{
			ITcSysManager2 systemManager =
				await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();

			Command.Visible = VS.Solutions.IsTwinCATProjectLoaded();
			Command.Enabled = AmsNetId.Local.ToString() == systemManager.GetTargetNetId();
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
			}

			var target = systemManager.GetTargetNetId();
			AmsNetId.TryParse(target, out AmsNetId amsnetid);

			if (!await VS.MessageBox.ShowConfirmAsync("Target <" + target + ">", "Reboot"))
				return;

			try
			{
				await RemoteControl.RebootAsync(new AmsNetId(target), CancellationToken.None);
				await VS.StatusBar.ShowMessageAsync(
					"Reboot successfully on target <" + target + ">"
				);
			}
			catch (Exception ex)
			{
				await VS.MessageBox.ShowErrorAsync(
					"Reboot failed on target <"
						+ target
						+ ">. See output window for more information."
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
