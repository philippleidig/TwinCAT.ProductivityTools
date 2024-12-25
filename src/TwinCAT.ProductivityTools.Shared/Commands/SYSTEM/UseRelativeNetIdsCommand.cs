using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Extensions;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	[Command(PackageIds.UseRelativeNetIdsCommandId)]
	internal class UseRelativeNetIdsCommand : BaseCommand<UseRelativeNetIdsCommand>
	{
		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			ITcSysManager2 systemManager =
				await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();
			systemManager.EnableUseRelativeNetIds();

			await VS.Solutions.SaveAsync();
		}
	}
}
