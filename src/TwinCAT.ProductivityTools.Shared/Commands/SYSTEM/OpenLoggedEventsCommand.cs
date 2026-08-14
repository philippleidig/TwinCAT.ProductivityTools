using System;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Helpers;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>
	/// Opens the "TwinCAT Logged Events" tool window that ships with the TwinCAT XAE shell.
	/// </summary>
	[Command(PackageIds.OpenLoggedEventsCommandId)]
	internal sealed class OpenLoggedEventsCommand : BaseCommand<OpenLoggedEventsCommand>
	{
		/// <summary>Tool window of TwinCAT XAE Base. Not part of this extension.</summary>
		private static readonly Guid EventLoggerToolWindow = new Guid(
			"6abb20ef-aeaf-486e-a9c4-09dd7e17c809"
		);

		protected override void BeforeQueryStatus(EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			bool isTwinCATSolution = false;

			try
			{
				isTwinCATSolution = VS.Solutions.IsTwinCATProjectLoaded();
			}
			catch (Exception)
			{
				// A solution that is still loading hides the command.
			}

			Command.Visible = isTwinCATSolution;
			Command.Enabled = isTwinCATSolution;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			try
			{
				await VS.Windows.ShowToolWindowAsync(EventLoggerToolWindow);
			}
			catch (Exception ex)
			{
				// The window belongs to TwinCAT XAE Base. It is missing in a plain Visual Studio
				// without the TwinCAT integration.
				await Report.FailureAsync(
					"The TwinCAT logged events window is not available in this IDE.",
					ex
				);
			}
		}
	}
}
