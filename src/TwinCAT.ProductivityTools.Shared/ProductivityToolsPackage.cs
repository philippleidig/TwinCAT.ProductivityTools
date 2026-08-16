using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.InfoBars;
using TwinCAT.ProductivityTools.Services;
using TwinCAT.ProductivityTools.ToolWindows;
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
	[ProvideToolWindow(
		typeof(IOMappingToolWindow.Pane),
		Orientation = ToolWindowOrientation.Right,
		Window = EnvDTE.Constants.vsWindowKindMainWindow,
		Style = VsDockStyle.Tabbed
	)]
	[ProvideService((typeof(IOutputWindowPane)), IsAsyncQueryable = true)]
	[ProvideOptionPage(
		typeof(Options.OptionsProvider.GeneralOptions),
		Vsix.Name,
		"General",
		0,
		0,
		true,
		SupportsProfiles = true
	)]
	[ProvideOptionPage(
		typeof(Options.OptionsProvider.BuildOptions),
		Vsix.Name,
		"Build",
		0,
		0,
		true,
		SupportsProfiles = true
	)]
	public sealed class ProductivityToolsPackage : ToolkitPackage
	{
		protected override async Task InitializeAsync(
			CancellationToken cancellationToken,
			IProgress<ServiceProgressData> progress
		)
		{
			this.RegisterToolWindows();

			await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			await this.RegisterServicesAsync();
			await this.RegisterCommandsAsync();

			// Neither an info bar nor the artefact cleanup is essential. A failure while setting
			// them up must never abort package initialization, because a failed SetSite disables
			// every command of this package for the whole IDE session.
			await this.SafelyAsync(this.RegisterInfoBarsAsync);
			await this.SafelyAsync(BuildArtifactCleanupService.Instance.ApplyOptionsAsync);
		}

		private async Task SafelyAsync(Func<Task> action)
		{
			try
			{
				await action();
			}
			catch (Exception ex)
			{
				await this.LogFailureAsync(ex);
			}
		}

		private async Task LogFailureAsync(Exception exception)
		{
			await JoinableTaskFactory.SwitchToMainThreadAsync();

			ActivityLog.TryLogError(nameof(ProductivityToolsPackage), exception.ToString());
		}

		private Task RegisterServicesAsync()
		{
			this.AddService<OutputWindow, IOutputWindowPane>(new OutputWindow());

			return Task.CompletedTask;
		}

		private async Task RegisterInfoBarsAsync()
		{
			UseRelativeNetIdsInfoBar infoBar = new UseRelativeNetIdsInfoBar();
			await infoBar.ShowAsync();
		}
	}
}
