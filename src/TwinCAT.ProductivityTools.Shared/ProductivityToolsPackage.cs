using System;
using System.Runtime.InteropServices;
using System.Threading;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Services;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools
{
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version, IconResourceID = 400)]
	[ProvideAutoLoad(
		VSConstants.UICONTEXT.SolutionHasMultipleProjects_string,
		PackageAutoLoadFlags.BackgroundLoad
	)]
	[ProvideAutoLoad(
		VSConstants.UICONTEXT.SolutionHasSingleProject_string,
		PackageAutoLoadFlags.BackgroundLoad
	)]
	[Guid(PackageGuids.ProductivityToolsCmdSetString)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[ProvideService((typeof(ITargetSystemService)), IsAsyncQueryable = true)]
	[ProvideService((typeof(IOutputWindowPane)), IsAsyncQueryable = true)]
	public sealed class ProductivityToolsPackage : ToolkitPackage
	{
		protected override async Task InitializeAsync(
			CancellationToken cancellationToken,
			IProgress<ServiceProgressData> progress
		)
		{
			await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			await this.RegisterServicesAsync();
			await this.RegisterCommandsAsync();
		}

		private async Task RegisterServicesAsync()
		{
			EnvDTE.DTE dte = await VS.GetRequiredServiceAsync<EnvDTE.DTE, EnvDTE.DTE>();

			this.AddService<OutputWindow, IOutputWindowPane>(new OutputWindow());
			this.AddService<TargetSystemService, ITargetSystemService>(
				new TargetSystemService(dte)
			);
		}
	}
}
