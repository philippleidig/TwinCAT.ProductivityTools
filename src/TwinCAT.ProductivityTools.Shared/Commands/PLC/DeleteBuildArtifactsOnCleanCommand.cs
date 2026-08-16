using System;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Services;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>
	/// Toggles whether the build output of a TwinCAT project is deleted from disk when the project
	/// is cleaned.
	/// </summary>
	[Command(PackageIds.DeleteBuildArtifactsOnCleanCommandId)]
	internal sealed class DeleteBuildArtifactsOnCleanCommand
		: BaseCommand<DeleteBuildArtifactsOnCleanCommand>
	{
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
			Command.Checked = Options.Build.Instance.DeleteBuildArtifactsOnClean;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			Options.Build options = await Options.Build.GetLiveInstanceAsync();

			options.DeleteBuildArtifactsOnClean = !options.DeleteBuildArtifactsOnClean;

			await options.SaveAsync();

			// The service owns the single subscription. Subscribing here would add one more
			// handler per toggle and delete the artefacts as many times as the command was used.
			await BuildArtifactCleanupService.Instance.ApplyOptionsAsync();
		}
	}
}
