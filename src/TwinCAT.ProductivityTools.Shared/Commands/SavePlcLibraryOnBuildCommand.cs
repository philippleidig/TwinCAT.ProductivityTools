using System;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Options;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	//[Command(PackageIds.SavePlcLibraryOnBuildCommandId)]
	//internal sealed class SavePlcLibraryOnBuildCommand : BaseCommand<SavePlcLibraryOnBuildCommand>
	//{
	//	public SavePlcLibraryOnBuildCommand()
	//	{
	//		//VS.Events.BuildEvents.SolutionBuildDone += OnSolutionBuildDone;
	//	}
	//
	//	private void OnSolutionBuildDone(bool success)
	//	{
	//		if (!success)
	//		{
	//			return;
	//		}
	//
	//		VS.MessageBox.Show("Solution build finished");
	//	}
	//
	//	protected override void BeforeQueryStatus(EventArgs e)
	//	{
	//		Command.Visible = VS.Solutions.IsTwinCATProjectLoaded();
	//		Command.Enabled = true;
	//		Command.Checked = General.Instance.AutoSaveLibraryAfterBuild;
	//	}
	//
	//	protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
	//	{
	//		Command.Checked = !Command.Checked;
	//		General options = await General.GetLiveInstanceAsync();
	//		options.AutoSaveLibraryAfterBuild = Command.Checked;
	//		await options.SaveAsync();
	//	}
	//}
}
