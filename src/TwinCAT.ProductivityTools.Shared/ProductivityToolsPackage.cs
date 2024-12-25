using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using TCatSysManagerLib;
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

			//jOnSolutionOpened();

			//HideMenuItems();
		}

		private async void HideMenuItems()
		{
			var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

			if (mcs != null)
			{
				// Command ID aus einer anderen Extension
				CommandID otherExtensionCommandId = new CommandID(
					Guid.Parse("74D21311-2AEE-11D1-8BFB-00A0-00A0C90F26F7"),
					0x3100
				);

				// Command abrufen und Sichtbarkeit steuern
				var menuCommand = mcs.FindCommand(otherExtensionCommandId);
				if (menuCommand != null)
				{
					menuCommand.Visible = false; // Command verstecken
				}
			}
		}

		private async void OnSolutionOpened()
		{
			var model = new InfoBarModel(
				new[]
				{
					new InfoBarTextSpan("Activate relative AmsNetIDs."),
					new InfoBarHyperlink("Go to ")
				},
				KnownMonikers.SettingsGroupWarning,
				true
			);

			InfoBar infoBar = await VS.InfoBar.CreateAsync(model);

			infoBar.ActionItemClicked += (s, e) =>
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				e.InfoBarUIElement.Close();

				// systemManager.EnableUseRelativeNetIds();
			};

			await infoBar.TryShowInfoBarUIAsync();
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
