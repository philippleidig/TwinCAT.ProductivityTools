using System;
using System.IO;
using System.Linq;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Options;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	//[Command(PackageIds.CleanProjectCommandId)]
	internal class CheckAllObjectsCommand : BaseCommand<CheckAllObjectsCommand>
	{
		private async void OnSolutionBuildDone(bool obj)
		{
			ITcSysManager2 systemManager =
				await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();

			ITcSmTreeItem plcTreeItem = systemManager.LookupTreeItem("TIPC");

			foreach (ITcSmTreeItem plcProject in plcTreeItem)
			{
				ITcProjectRoot projectRoot = plcProject as ITcProjectRoot;
				ITcSmTreeItem nestedProject = projectRoot.NestedProject;

				ITcPlcIECProject2 iecProject = nestedProject as ITcPlcIECProject2;
				iecProject.CheckAllObjects();
			}
		}

		protected override void BeforeQueryStatus(EventArgs e)
		{
			Command.Visible = VS.Solutions.IsTwinCATProjectLoaded();
			Command.Enabled = true;
			//Command.Checked = Options.Build.Instance.CheckAllObjectsOnBuild;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			//var isChecked = Options.Build.Instance.CheckAllObjectsOnBuild;
			//Options.Build.Instance.CheckAllObjectsOnBuild = !isChecked;
			//
			//await Options.Build.Instance.SaveAsync();
			//
			//if (Options.Build.Instance.CheckAllObjectsOnBuild)
			//{
			//	VS.Events.BuildEvents.SolutionBuildDone += OnSolutionBuildDone;
			//}
			//else
			//{
			//	VS.Events.BuildEvents.SolutionBuildDone -= OnSolutionBuildDone;
			//}
		}
	}
}
