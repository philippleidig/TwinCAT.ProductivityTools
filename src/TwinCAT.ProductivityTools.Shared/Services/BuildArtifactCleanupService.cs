using System;
using System.IO;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TwinCAT.ProductivityTools.Build;
using TwinCAT.ProductivityTools.Helpers;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Services
{
	/// <summary>
	/// Deletes the build output of a project from disk after Visual Studio cleaned it, as long as
	/// the corresponding option is switched on.
	/// </summary>
	/// <remarks>
	/// Exactly one instance owns the subscription so that toggling the option cannot accumulate
	/// handlers, and so that the handler is attached again after the option was changed in the
	/// options dialog rather than through the command.
	/// </remarks>
	internal sealed class BuildArtifactCleanupService
	{
		public static BuildArtifactCleanupService Instance { get; } =
			new BuildArtifactCleanupService();

		private readonly BuildArtifactCleaner cleaner = new BuildArtifactCleaner();
		private bool isSubscribed;

		private BuildArtifactCleanupService() { }

		/// <summary>
		/// Attaches or detaches the handler so that it matches the current option value.
		/// </summary>
		public async Task ApplyOptionsAsync()
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			bool shouldClean;

			try
			{
				shouldClean = (
					await Options.Build.GetLiveInstanceAsync()
				).DeleteBuildArtifactsOnClean;
			}
			catch (Exception)
			{
				// Unreadable settings must not keep the package from loading.
				return;
			}

			if (shouldClean == isSubscribed)
			{
				return;
			}

			if (shouldClean)
			{
				VS.Events.BuildEvents.ProjectCleanDone += OnProjectCleanDone;
			}
			else
			{
				VS.Events.BuildEvents.ProjectCleanDone -= OnProjectCleanDone;
			}

			isSubscribed = shouldClean;
		}

		/// <summary>
		/// Runs after the clean instead of before it, because the project system recreates part of
		/// its output while cleaning and would restore what was deleted too early.
		/// </summary>
		private void OnProjectCleanDone(ProjectBuildDoneEventArgs args)
		{
			BackgroundWork.Run(
				() => CleanAsync(args?.Project),
				"Failed to delete the build artifacts."
			);
		}

		private async Task CleanAsync(Community.VisualStudio.Toolkit.Project project)
		{
			try
			{
				string projectFile = project?.FullPath;

				if (string.IsNullOrEmpty(projectFile))
				{
					return;
				}

				string directory = Path.GetDirectoryName(projectFile);

				BuildArtifactCleanResult result = await Task.Run(() => cleaner.Clean(directory));

				if (!result.DeletedAnything && result.FailedEntries.Count == 0)
				{
					return;
				}

				await Report.WriteAsync($"Clean {project.Name}: {result}");

				foreach (string failed in result.FailedEntries)
				{
					await Report.WriteAsync($"  could not be deleted: {failed}");
				}
			}
			catch (Exception ex)
			{
				await Report.FailureAsync(
					"Failed to delete the build artifacts.",
					ex,
					silent: true
				);
			}
		}
	}
}
