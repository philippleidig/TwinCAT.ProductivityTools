using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using TCatSysManagerLib;
using Project = Community.VisualStudio.Toolkit.Project;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Extensions
{
	internal static class SolutionExtensions
	{
		public static bool IsTwinCATProjectLoaded(this Solutions solutions)
		{
			if (!HierarchyUtilities.IsSolutionOpen)
			{
				return false;
			}

			EnvDTE.DTE dte = VS.GetRequiredService<DTE, DTE>();

			var projects = dte.Solution.Projects.Cast<EnvDTE.Project>().ToList();

			foreach (EnvDTE.Project project in projects)
			{
				try
				{
					ITcSysManager2 systemManager = project.Object as ITcSysManager2;

					if (systemManager != null)
					{
						return true;
					}
				}
				catch { }
			}

			return false;
		}

		public static async Task<ITcSysManager2> GetActiveTwinCATProjectSystemManagerAsync(
			this Solutions solutions
		)
		{
			EnvDTE.DTE dte = await VS.GetRequiredServiceAsync<DTE, DTE>();

			if (
				dte?.ActiveSolutionProjects is Array activeSolutionProjects
				&& activeSolutionProjects?.Length > 0
			)
			{
				var project = activeSolutionProjects?.GetValue(0) as EnvDTE.Project;
				try
				{
					ITcSysManager2 systemManager = project.Object as ITcSysManager2;

					if (systemManager != null)
					{
						return systemManager;
					}
				}
				catch { }
			}

			return null;
		}

		public static async Task<EnvDTE.Project> GetActiveTwinCATProjectAsync(
			this Solutions solutions
		)
		{
			EnvDTE.DTE dte = await VS.GetRequiredServiceAsync<DTE, DTE>();

			if (
				dte?.ActiveSolutionProjects is Array activeSolutionProjects
				&& activeSolutionProjects?.Length > 0
			)
			{
				var project = activeSolutionProjects?.GetValue(0) as EnvDTE.Project;
				try
				{
					ITcSysManager2 systemManager = project.Object as ITcSysManager2;

					if (systemManager != null)
					{
						return project;
					}
				}
				catch { }
			}

			return null;
		}

		public static async Task<IEnumerable<EnvDTE.Project>> GetAllTwinCATProjectsAsync(
			this Solutions solutions
		)
		{
			var twincatProjects = new List<EnvDTE.Project>();

			EnvDTE.DTE dte = await VS.GetRequiredServiceAsync<DTE, DTE>();

			var projects = dte.Solution.Projects.Cast<EnvDTE.Project>().ToList();

			foreach (EnvDTE.Project project in projects)
			{
				try
				{
					ITcSysManager2 systemManager = project.Object as ITcSysManager2;

					if (systemManager != null)
					{
						twincatProjects.Add(project);
					}
				}
				catch { }
			}

			return twincatProjects;
		}

		public static async Task UnloadAlltwinCATProjectsAsync(this Solutions solutions)
		{
			await VS.Solutions.SaveAsync();

			IEnumerable<Project> projects = await VS.Solutions.GetAllProjectsAsync();

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			foreach (var project in projects)
			{
				var isTwinCATProject = project.FullPath.EndsWith(".tsproj");

				if (isTwinCATProject && project.IsLoaded)
				{
					await project.SaveAsync();
					await project.UnloadAsync();
				}
			}
		}

		public static async Task LoadAllTwinCATProjectsAsync(this Solutions solutions)
		{
			await VS.Solutions.SaveAsync();

			IEnumerable<Project> projects = await VS.Solutions.GetAllProjectsAsync();

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			foreach (var project in projects)
			{
				var isTwinCATProject = project.FullPath.EndsWith(".tsproj");

				if (isTwinCATProject && !project.IsLoaded)
				{
					await project.SaveAsync();
					await project.LoadAsync();
				}
			}
		}

		public static async Task SaveAsync(this Solutions solutions)
		{
			EnvDTE.DTE dte = await VS.GetRequiredServiceAsync<DTE, DTE>();
			dte?.Solution?.SaveAs(dte?.Solution?.FullName);
		}
	}
}
