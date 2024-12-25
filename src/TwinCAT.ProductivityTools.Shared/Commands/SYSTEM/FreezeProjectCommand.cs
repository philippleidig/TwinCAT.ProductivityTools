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
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			EnvDTE.Project project = await VS.Solutions.GetActiveTwinCATProjectAsync();

			//EnvDTE.DTE dte = await VS.GetRequiredServiceAsync<EnvDTE.DTE, EnvDTE.DTE>();
			//dte.SuppressUI = true;

			await projectFreezer.FreezeProjectAsync(project);

			//dte.SuppressUI = false;

			await VS.Solutions.SaveAsync();
		}
	}
}
