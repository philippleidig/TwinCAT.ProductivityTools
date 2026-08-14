using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Extensions;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	[Command(PackageIds.RemoteDesktopCommandId)]
	internal sealed class RemoteDesktopCommand : BaseCommand<RemoteDesktopCommand>
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

			string ipAddress = AmsRouter
				.ListRoutes()
				.FirstOrDefault(route => route.NetId == target)
				?.Address;

			if (string.IsNullOrEmpty(ipAddress))
			{
				await VS.MessageBox.ShowErrorAsync(
					"TwinCAT ProductivityTools",
					"No route with an IP address was found for target <" + target + ">."
				);
				return;
			}

			RemoteDesktop.Connect(ipAddress);
		}
	}
}
