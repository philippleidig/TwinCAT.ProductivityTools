using System;
using System.Collections.Generic;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Services;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>
	/// Pins the TwinCAT version, the compiler version and the library placeholders of a project so
	/// that it builds the same way on another engineering machine.
	/// </summary>
	[Command(PackageIds.FreezeProjectCommandId)]
	internal sealed class FreezeProjectCommand : BaseCommand<FreezeProjectCommand>
	{
		private readonly IProjectFreezerService projectFreezer = new ProjectFreezerService();

		protected override void BeforeQueryStatus(EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			bool hasProject = false;

			try
			{
				hasProject = VS.Solutions.GetActiveTwinCATProjectSystemManager() != null;
			}
			catch (Exception)
			{
				// A project that is still loading hides the command instead of breaking the menu.
			}

			Command.Visible = hasProject;
			Command.Enabled = hasProject;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			EnvDTE.Project project = await VS.Solutions.GetActiveTwinCATProjectAsync();

			if (project == null)
			{
				await VS.MessageBox.ShowAsync(
					Vsix.Name,
					"The solution does not contain an active TwinCAT XAE project."
				);
				return;
			}

			try
			{
				await projectFreezer.FreezeProjectsAsync(new[] { project });

				await VS.Solutions.SaveAsync();

				await Report.ShowStatusAsync("The TwinCAT project was frozen.");
			}
			catch (Exception ex)
			{
				await Report.FailureAsync("Failed to freeze the TwinCAT project.", ex);
			}
		}
	}
}
