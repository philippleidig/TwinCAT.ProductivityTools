using System;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Helpers;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>
	/// Switches the TwinCAT project to relative AmsNetIDs so that the solution no longer carries
	/// the AmsNetID of the machine it was engineered on.
	/// </summary>
	[Command(PackageIds.UseRelativeNetIdsCommandId)]
	internal sealed class UseRelativeNetIdsCommand : BaseCommand<UseRelativeNetIdsCommand>
	{
		protected override void BeforeQueryStatus(EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			bool canEnable = false;

			try
			{
				ITcSysManager2 systemManager = VS.Solutions.GetActiveTwinCATProjectSystemManager();

				canEnable = systemManager != null && !systemManager.IsUseRelativeNetIdsEnabled();
			}
			catch (Exception)
			{
				// Reading the routing settings can fail while a project is still loading.
			}

			Command.Visible = canEnable;
			Command.Enabled = canEnable;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			ITcSysManager2 systemManager =
				await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();

			if (systemManager == null)
			{
				await VS.MessageBox.ShowAsync(
					Vsix.Name,
					"The solution does not contain an active TwinCAT XAE project."
				);
				return;
			}

			try
			{
				systemManager.EnableUseRelativeNetIds();

				await VS.Solutions.SaveAsync();

				await Report.ShowStatusAsync("Relative AmsNetIDs are now enabled.");
			}
			catch (Exception ex)
			{
				await Report.FailureAsync("Failed to enable relative AmsNetIDs.", ex);
			}
		}
	}
}
