using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using TwinCAT.Ads;
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
	[ProvideService((typeof(ITargetSystemService)), IsAsyncQueryable = true)]
	[ProvideService((typeof(IOutputWindowPane)), IsAsyncQueryable = true)]
	[ProvideService((typeof(ITwinCATEventListenerService)), IsAsyncQueryable = true)]
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
			await this.RegisterInfoBarsAsync();

			//ITargetSystemService targetSystemService = await VS.GetServiceAsync<ITargetSystemService, ITargetSystemService>();

			//ITwinCATEventListenerService eventListenerService = await VS.GetServiceAsync<ITwinCATEventListenerService, ITwinCATEventListenerService>();
			//eventListenerService.Connect(AmsNetId.Local);
			//eventListenerService.MessageOccured += OnTwinCATMessageOccured;
		}

		private void OnTwinCATMessageOccured(object sender, MessageOccuredEventArgs e)
		{
			if (e.Event.Severity == DataTypes.EventSeverity.ERROR)
			{
				VS.MessageBox.ShowError(e.Event.Message);
			}
		}

		private async Task RegisterServicesAsync()
		{
			EnvDTE.DTE dte = await VS.GetRequiredServiceAsync<EnvDTE.DTE, EnvDTE.DTE>();

			this.AddService<OutputWindow, IOutputWindowPane>(new OutputWindow());

			this.AddService<TargetSystemService, ITargetSystemService>(
				new TargetSystemService(dte)
			);

			this.AddService<TwinCATEventListenerService, ITwinCATEventListenerService>(
				new TwinCATEventListenerService()
			);
		}

		private async Task RegisterInfoBarsAsync()
		{
			UseRelativeNetIdsInfoBar infoBar = new UseRelativeNetIdsInfoBar();
			await infoBar.ShowAsync();
		}
	}
}
