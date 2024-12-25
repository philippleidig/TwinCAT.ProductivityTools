using System;
using System.IO;
using System.Linq;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Options;
using static System.Windows.Forms.Design.AxImporter;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	//[Command(PackageIds.DeleteBuildArtifactsOnCleanCommandId)]
	internal class DeleteBuildArtifactsOnCleanCommand : BaseCommand<DeleteBuildArtifactsOnCleanCommand>
	{
		private void OnProjectCleanStarted(Project project)
		{
			var directory = Path.GetDirectoryName(project.FullPath);

			var fileSystem = new FileSystem();
			var fileFilter = new FileFilterProvider();

			var (accepted, denied) = fileSystem.GetFilesFiltered(directory, fileFilter);

			var filesDeleted = 0;
			var diretoriesDeleted = 0;

			if (!denied.Any())
			{
				return;
			}

			foreach (string file in denied)
			{
				var filePath = Path.Combine(directory, file);

				if (File.Exists(filePath))
				{
					filesDeleted++;
					fileSystem.DeleteFile(filePath);
				}
			}

			foreach (string file in denied)
			{
				var filePath = string.Concat(directory, file.Replace("/", ""));

				if (Directory.Exists(filePath))
				{
					diretoriesDeleted++;
					fileSystem.DeleteDirectory(filePath, true);
				}
			}
		}

		protected override void BeforeQueryStatus(EventArgs e)
		{
			Command.Visible = VS.Solutions.IsTwinCATProjectLoaded();
			Command.Enabled = true;
			Command.Checked = Options.Build.Instance.DeleteBuildArtifactsOnClean;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			var isChecked = Options.Build.Instance.DeleteBuildArtifactsOnClean;
			Options.Build.Instance.DeleteBuildArtifactsOnClean = !isChecked;

			await Options.Build.Instance.SaveAsync();

			if (!isChecked)
			{
				VS.Events.BuildEvents.ProjectCleanStarted += OnProjectCleanStarted;
			} else
			{
				VS.Events.BuildEvents.ProjectCleanStarted -= OnProjectCleanStarted;
			}
		}

	}
}
