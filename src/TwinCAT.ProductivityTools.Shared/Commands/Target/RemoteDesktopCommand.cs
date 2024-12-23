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
			Command.Visible = VS.Solutions.IsTwinCATProjectLoaded();
			Command.Enabled = true;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
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

			var ipAddress = AmsRouter
				.ListRoutes()
				.Where(route => route.NetId == target)
				.FirstOrDefault()
				.Address;

			if (!string.IsNullOrEmpty(ipAddress))
				RemoteDesktop.Connect(ipAddress);
		}
	}
}
