using System;
using System.Collections.Generic;
using System.Linq;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Services;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	[Command(PackageIds.FreezeProjectCommandId)]
	internal sealed class FreezeProjectCommand : BaseCommand<FreezeProjectCommand>
	{
		private readonly IProjectFreezerService projectFreezer;

		public FreezeProjectCommand()
		{
			projectFreezer = new ProjectFreezerService();
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			EnvDTE.Project project = await VS.Solutions.GetActiveTwinCATProjectAsync();

			//await VS.Solutions.UnloadAlltwinCATProjectsAsync();

			await projectFreezer.FreezeProjectAsync(project);

			//await VS.Solutions.LoadAllTwinCATProjectsAsync();

			await VS.Solutions.SaveAsync();
		}
	}
}
