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
		public CheckAllObjectsCommand()
		{
			VS.Events.BuildEvents.SolutionBuildDone += OnSolutionBuildDone;
			;
			General.Saved += OnOptionsSaved;
		}

		private async void OnSolutionBuildDone(bool obj)
		{
			ITcSysManager2 systemManager =
				await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();

			ITcSmTreeItem plcTreeItem = systemManager.LookupTreeItem("TIPC");

			foreach (ITcSmTreeItem plcProject in plcTreeItem)
			{
				ITcProjectRoot projectRoot = (ITcProjectRoot)plcProject;
				ITcSmTreeItem nestedProject = projectRoot.NestedProject;

				ITcPlcIECProject2 iecProject = nestedProject as ITcPlcIECProject2;
				iecProject.CheckAllObjects();
			}
		}

		private void OnOptionsSaved(General obj)
		{
			// get option for auto clean project when Clean SOlution is executed
		}

		protected override void BeforeQueryStatus(EventArgs e)
		{
			Command.Visible = VS.Solutions.IsTwinCATProjectLoaded();
			Command.Enabled = true;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e) { }
	}
}
