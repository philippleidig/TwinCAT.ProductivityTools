using System;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	[Command(PackageIds.OpenLoggedEventsCommandId)]
	internal sealed class OpenLoggedEventsCommandCommand
		: BaseCommand<OpenLoggedEventsCommandCommand>
	{
		private readonly Guid EventLoggerToolWindowGuid = Guid.Parse(
			"6abb20ef-aeaf-486e-a9c4-09dd7e17c809"
		);

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await VS.Windows.ShowToolWindowAsync(EventLoggerToolWindowGuid);
		}
	}
}
