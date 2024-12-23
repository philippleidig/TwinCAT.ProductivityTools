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
using TwinCAT.ProductivityTools.Extensions;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	[Command(PackageIds.SetTickCommandId)]
	internal sealed class SetTickCommand : BaseCommand<SetTickCommand>
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

			if (
				!await VS.MessageBox.ShowConfirmAsync(
					"Execute win8settick.bat on target <" + target + "> ?",
					"win8settick.bat"
				)
			)
				return;

			try
			{
				var path = @"C:\TwinCAT\3.1\System\win8settick.bat";
				var dir = @"C:\TwinCAT\3.1\System";

				await RemoteControl.StartProcessAsync(
					new Ads.AmsNetId(target),
					path,
					dir,
					string.Empty,
					CancellationToken.None
				);

				await VS.StatusBar.ShowMessageAsync(
					"win8settick.bat successfully executed on target <" + target + ">"
				);
			}
			catch (Exception ex)
			{
				await VS.MessageBox.ShowErrorAsync(
					"Execution of win8settick.bat on target <" + target + "> failed!",
					ex.Message
				);
			}
		}
	}
}
