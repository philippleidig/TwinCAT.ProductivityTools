using System;
using System.IO;
using System.Linq;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Options;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	//[Command(PackageIds.CleanProjectCommandId)]
	internal class CleanProjectCommand : BaseCommand<CleanProjectCommand>
	{
		public CleanProjectCommand()
		{
			VS.Events.BuildEvents.ProjectCleanDone += OnProjectCleanDone;
			General.Saved += OnOptionsSaved;
		}

		private void OnOptionsSaved(General obj)
		{
			// get option for auto clean project when Clean SOlution is executed
		}

		private void OnProjectCleanDone(ProjectBuildDoneEventArgs obj)
		{
			throw new NotImplementedException();
		}

		protected override void BeforeQueryStatus(EventArgs e)
		{
			Command.Visible = VS.Solutions.IsTwinCATProjectLoaded();
			Command.Enabled = true;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			var dte = await VS.GetRequiredServiceAsync<EnvDTE.DTE, EnvDTE.DTE>();

			string projectFile = dte?.Solution?.FullName;
			var directory = Path.GetDirectoryName(projectFile);

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
	}
}
