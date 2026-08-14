using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using TwinCAT.ProductivityTools.ToolWindows;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	[Command(PackageIds.ShowIOMappingToolWindowCommandId)]
	internal sealed class ShowIOMappingToolWindowCommand
		: BaseCommand<ShowIOMappingToolWindowCommand>
	{
		protected override Task ExecuteAsync(OleMenuCmdEventArgs e) =>
			IOMappingToolWindow.ShowAsync();
	}
}
