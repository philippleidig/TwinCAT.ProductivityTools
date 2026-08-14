using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Plc;

namespace TwinCAT.ProductivityTools.Services
{
	internal class ProjectFreezerService : IProjectFreezerService
	{
		public async Task FreezeProjectsAsync(IEnumerable<EnvDTE.Project> projects)
		{
			if (projects == null)
			{
				return;
			}

			foreach (EnvDTE.Project project in projects)
			{
				try
				{
					await FreezeProjectAsync(project);
				}
				catch (Exception ex)
				{
					// One project that cannot be frozen must not stop the remaining ones, but the
					// reason still has to reach the user.
					await ReportAsync(project?.Name, ex);
				}
			}
		}

		public async Task FreezeProjectAsync(EnvDTE.Project project)
		{
			if (!(project?.Object is ITcSysManager2 systemManager))
			{
				return;
			}

			FreezeSolutionProjectFile(project.FullName);

			if (!(systemManager.LookupTreeItem(PlcTreeItemPath) is ITcSmTreeItem plcProjects))
			{
				return;
			}

			foreach (ITcSmTreeItem plcProject in plcProjects)
			{
				await FreezePlcProjectAsync(plcProject);
			}
		}

		public Task FreezePlcProjectAsync(ITcSmTreeItem project)
		{
			if (!(project is ITcProjectRoot projectRoot))
			{
				return Task.CompletedTask;
			}

			var plcProjectItem = (ITcSmTreeItem)projectRoot.NestedProject;

			if (plcProjectItem == null)
			{
				return Task.CompletedTask;
			}

			// Pinning the placeholders first makes the library references independent of the
			// libraries that happen to be installed on the next machine.
			if (plcProjectItem.LookupChild("References") is ITcPlcLibraryManager references)
			{
				references.FreezePlaceholder();
			}

			string projectFilePath = ProjectFreezeXml.ReadProjectPath(
				XDocument.Parse(project.ProduceXml())
			);
			string compilerVersion = ProjectFreezeXml.ReadActiveCompilerVersion(
				XDocument.Parse(plcProjectItem.ProduceXml())
			);

			if (string.IsNullOrEmpty(projectFilePath) || !File.Exists(projectFilePath))
			{
				return Task.CompletedTask;
			}

			XDocument plcProjectFile = XDocument.Load(projectFilePath);

			if (ProjectFreezeXml.FreezePlcProject(plcProjectFile, compilerVersion))
			{
				plcProjectFile.Save(projectFilePath);
			}

			return Task.CompletedTask;
		}

		private const string PlcTreeItemPath = "TIPC";

		private static void FreezeSolutionProjectFile(string path)
		{
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				return;
			}

			XDocument tsProject = XDocument.Load(path);

			if (ProjectFreezeXml.FreezeSolutionProject(tsProject))
			{
				tsProject.Save(path);
			}
		}

		private static async Task ReportAsync(string projectName, Exception exception)
		{
			try
			{
				IOutputWindowPane outputWindowPane = await VS.GetRequiredServiceAsync<
					IOutputWindowPane,
					IOutputWindowPane
				>();

				await outputWindowPane.WriteLineAsync(
					$"Failed to freeze {projectName ?? "project"}: {exception.Message}"
				);
			}
			catch (Exception)
			{
				// The output window is a convenience; failing to report must not replace the
				// original error with a new one.
			}
		}
	}
}
