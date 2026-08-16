using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Extensions
{
	/// <summary>
	/// Finds the TwinCAT projects of the current solution.
	/// </summary>
	/// <remarks>
	/// Every lookup tolerates a failing COM call. The TwinCAT project system throws while a
	/// project is loading, unloading or being reloaded after a target change, and these helpers
	/// are used from <c>BeforeQueryStatus</c>, where an exception breaks the whole context menu.
	/// </remarks>
	internal static class SolutionExtensions
	{
		public static bool IsTwinCATProjectLoaded(this Solutions solutions)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (!HierarchyUtilities.IsSolutionOpen)
			{
				return false;
			}

			try
			{
				DTE dte = VS.GetRequiredService<DTE, DTE>();

				return AllProjects(dte?.Solution).Any(project => SystemManagerOf(project) != null);
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static async Task<ITcSysManager2> GetActiveTwinCATProjectSystemManagerAsync(
			this Solutions solutions
		)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			DTE dte = await VS.GetRequiredServiceAsync<DTE, DTE>();

			return SystemManagerOf(ActiveProject(dte));
		}

		/// <summary>
		/// Synchronous counterpart of <see cref="GetActiveTwinCATProjectSystemManagerAsync"/>.
		/// Required by <c>BeforeQueryStatus</c>, which the shell calls synchronously while the
		/// menu is being built. An asynchronous lookup would complete after the menu item has
		/// already been rendered and therefore could never affect its visibility or enabled state.
		/// </summary>
		public static ITcSysManager2 GetActiveTwinCATProjectSystemManager(this Solutions solutions)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (!HierarchyUtilities.IsSolutionOpen)
			{
				return null;
			}

			try
			{
				DTE dte = VS.GetRequiredService<DTE, DTE>();

				return SystemManagerOf(ActiveProject(dte));
			}
			catch (Exception)
			{
				return null;
			}
		}

		public static async Task<EnvDTE.Project> GetActiveTwinCATProjectAsync(
			this Solutions solutions
		)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			DTE dte = await VS.GetRequiredServiceAsync<DTE, DTE>();

			EnvDTE.Project project = ActiveProject(dte);

			return SystemManagerOf(project) == null ? null : project;
		}

		public static async Task<IEnumerable<EnvDTE.Project>> GetAllTwinCATProjectsAsync(
			this Solutions solutions
		)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			DTE dte = await VS.GetRequiredServiceAsync<DTE, DTE>();

			return AllProjects(dte?.Solution)
				.Where(project => SystemManagerOf(project) != null)
				.ToList();
		}

		/// <summary>
		/// Saves the solution and everything in it.
		/// </summary>
		/// <remarks>
		/// <c>Solution.SaveAs</c> with the path of the solution itself is the documented way to
		/// save a solution, but it throws for a solution that has never been saved, because its
		/// <c>FullName</c> is empty. Such a solution has nothing to save either.
		/// </remarks>
		public static async Task SaveAsync(this Solutions solutions)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			try
			{
				DTE dte = await VS.GetRequiredServiceAsync<DTE, DTE>();

				EnvDTE.Solution solution = dte?.Solution;
				string path = solution?.FullName;

				if (string.IsNullOrEmpty(path))
				{
					return;
				}

				solution.SaveAs(path);
			}
			catch (Exception)
			{
				// Saving is a convenience after a modification. The modification itself has
				// already been applied to the in memory project.
			}
		}

		private static EnvDTE.Project ActiveProject(DTE dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				if (
					dte?.ActiveSolutionProjects is Array activeProjects
					&& activeProjects.Length > 0
				)
				{
					return activeProjects.GetValue(0) as EnvDTE.Project;
				}
			}
			catch (Exception) { }

			return null;
		}

		private static ITcSysManager2 SystemManagerOf(EnvDTE.Project project)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				return project?.Object as ITcSysManager2;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Flattens the solution, because projects inside a solution folder are not part of
		/// <c>Solution.Projects</c> themselves - the folder is, and it carries them as items.
		/// </summary>
		private static IEnumerable<EnvDTE.Project> AllProjects(EnvDTE.Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			List<EnvDTE.Project> projects = new List<EnvDTE.Project>();

			try
			{
				foreach (EnvDTE.Project project in solution?.Projects ?? EmptyProjects())
				{
					Collect(project, projects, 0);
				}
			}
			catch (Exception) { }

			return projects;
		}

		private static void Collect(
			EnvDTE.Project project,
			List<EnvDTE.Project> projects,
			int depth
		)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (project == null || depth > MaximumSolutionFolderDepth)
			{
				return;
			}

			projects.Add(project);

			try
			{
				if (project.Kind != SolutionFolderKind || project.ProjectItems == null)
				{
					return;
				}

				foreach (ProjectItem item in project.ProjectItems)
				{
					Collect(item?.SubProject, projects, depth + 1);
				}
			}
			catch (Exception) { }
		}

		private static System.Collections.IEnumerable EmptyProjects() =>
			Array.Empty<EnvDTE.Project>();

		private const string SolutionFolderKind = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
		private const int MaximumSolutionFolderDepth = 16;
	}
}
