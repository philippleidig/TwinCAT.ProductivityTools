using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using TCatSysManagerLib;
using VisualStudio.Extension;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuids.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class ProductivityToolsPackage : AsyncPackage
    {

        private RelayCommand _shutdownCommand { get; set; }
        private RelayCommand _restartCommand { get; set; }
        private RelayCommand _deviceInfoCommand { get; set; }
        private RelayCommand _remoteDesktopCommand { get; set; }
        private RelayCommand _rteInstallCommand { get; set; }
        private RelayCommand _setTickCommand { get; set; }
        private RelayCommand _aboutCommand { get; set; }

        private EnvDTE.DTE _DTE { get; set; }
        private List<EnvDTE.Project> _projects { get; set; }
        private bool _isTwinCATProject { get; set; }
        private ITcSysManager10 _systemManager { get; set; }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            bool isSolutionLoaded = await IsSolutionLoadedAsync();

            if (isSolutionLoaded)
                OnSolutionOpened();

            SolutionEvents.OnAfterOpenSolution += OnSolutionOpened;
            SolutionEvents.OnBeforeCloseSolution += OnSolutionClosing;

            InitializeCommands();

            NotificationProvider.ServiceProvider = this;
        }

        private void InitializeCommands()
        {
            _shutdownCommand = new RelayCommand(
                    this,
                    CommandIds.ShutdownCommand,
                    PackageGuids.GuidCommandPackageCmdSet,
                    ShutdownAsync,
                    IsAvailable
                );

            _restartCommand = new RelayCommand(
                    this,
                    CommandIds.RestartCommand,
                    PackageGuids.GuidCommandPackageCmdSet,
                    RestartAsync,
                    IsAvailable
                );

            _deviceInfoCommand = new RelayCommand(
                    this,
                    CommandIds.DeviceInfoCommand,
                    PackageGuids.GuidCommandPackageCmdSet,
                    OpenDeviceInfo,
                    IsAvailable
                );

            _remoteDesktopCommand = new RelayCommand(
                    this,
                    CommandIds.RemoteDesktopCommand,
                    PackageGuids.GuidCommandPackageCmdSet,
                    OpenRemoteDesktop,
                    IsAvailable
                );

            _rteInstallCommand = new RelayCommand(
                    this,
                    CommandIds.RteInstallCommand,
                    PackageGuids.GuidCommandPackageCmdSet,
                    OpenRteInstall,
                    IsAvailable
                );

            _setTickCommand = new RelayCommand(
                    this,
                    CommandIds.SetTickCommand,
                    PackageGuids.GuidCommandPackageCmdSet,
                    SetTickAsync,
                    IsAvailable
                );

            _aboutCommand = new RelayCommand(
                    this,
                    CommandIds.AboutCommand,
                    PackageGuids.GuidCommandPackageCmdSet,
                    About,
                    IsAvailable
                );
        }

        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var solService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));

            return value is bool isSolOpen && isSolOpen;
        }

        private void OnSolutionOpened(object sender = null, OpenSolutionEventArgs e = null)
        {
            _isTwinCATProject = false;
            _systemManager = null;

            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;

            if (dte != null)
            {
                _DTE = dte;

                // Get all projects in active solution
                _projects = _DTE.Solution.Projects
                                .Cast<EnvDTE.Project>()
                                .ToList();

                // Find TwinCAT Project and save the systemmanager
                foreach (var project in _projects)
                {
                    try
                    {
                        ITcSysManager sysMan = (ITcSysManager)project.Object;

                        if (sysMan != null)
                        {
                            _systemManager = (ITcSysManager10)sysMan;
                            _isTwinCATProject = true;

                            break;
                        }
                    }
                    catch { }
                }
            }

        }
        private void OnSolutionClosing(object sender = null, EventArgs e = null)
        {
            _isTwinCATProject = false;
            _projects.Clear();
            _systemManager = null;
            _DTE = null;
        }

        private async void ShutdownAsync(object sender, EventArgs e)
        {
            var target = _systemManager.GetTargetNetId();

            if (!NotificationProvider.ShowQueryMessage("Target <" + target + ">", "Shutdown")) return;

            try
            {
                await RemoteControl.ShutdownAsync(new Ads.AmsNetId(target), TimeSpan.FromSeconds(1), CancellationToken.None);
                NotificationProvider.DisplayInStatusBar("Shutdown successfully on target <" + target + ">");
            }
            catch (Exception ex)
            {
                NotificationProvider.ShowErrorMessage(ex, "Shutdown failed on target <" + target + ">");
            }
        }
        private async void RestartAsync(object sender, EventArgs e)
        {
            var target = _systemManager.GetTargetNetId();

            if (!NotificationProvider.ShowQueryMessage("Target <" + target + ">", "Reboot")) return;

            try
            {
                await RemoteControl.RebootAsync(new Ads.AmsNetId(target), TimeSpan.FromSeconds(1), CancellationToken.None);
                NotificationProvider.DisplayInStatusBar("Reboot successfully on target <" + target + ">");
            }
            catch (Exception ex)
            {
                NotificationProvider.ShowErrorMessage(ex.Message, "Reboot failed on target <" + target + ">");
            }
        }
        private void OpenDeviceInfo(object sender, EventArgs e)
        {
            var target = _systemManager.GetTargetNetId();

            var deviceInfo = new DeviceInfoView(new Ads.AmsNetId(target));
            deviceInfo.ShowModal();

        }
        private void OpenRemoteDesktop(object sender, EventArgs e)
        {
            var target = _systemManager.GetTargetNetId();

            var ipAddress = AmsRouter.ListRoutes()
                             .Where(route => route.NetId == target)
                             .FirstOrDefault()
                             .Address;

            if (!string.IsNullOrEmpty(ipAddress))
                RemoteDesktop.Connect(ipAddress);
        }
        private void OpenRteInstall(object sender, EventArgs e)
        {
            var target = _systemManager.GetTargetNetId();

            var win = new TcRteInstallView(target);
            win.ShowModal();
        }
        private async void SetTickAsync(object sender, EventArgs e)
        {
            var target = _systemManager.GetTargetNetId();

            if (!NotificationProvider.ShowQueryMessage("Execute win8settick.bat on target <" + target + "> ?", "win8settick.bat")) return;

            try
            {
                var path = @"C:\TwinCAT\3.1\System\win8settick.bat";
                var dir = @"C:\TwinCAT\3.1\System";

                await RemoteControl.StartProcessAsync(new Ads.AmsNetId(target), path, dir, string.Empty, CancellationToken.None);

                NotificationProvider.DisplayInStatusBar("win8settick.bat successfully executed on target <" + target + ">");
            }
            catch (Exception ex)
            {
                NotificationProvider.ShowErrorMessage(ex, "Execution of win8settick.bat on target <" + target + "> failed!");
            }
        }
        private void About(object sender, EventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            NotificationProvider.ShowInfoMessage(string.Concat("Beckhoff Automation\nInternal use only!\nUnsupported Utility!\nVersion: ", version ), "TwinCAT Productivity Tools");
        }

        private void IsAvailable(object sender, EventArgs e)
        {
            var cmd = (OleMenuCommand)sender;
            cmd.Enabled = false;
            cmd.Visible = false;

            if (_isTwinCATProject)
            {
                var target = _systemManager.GetTargetNetId();

                if (!IsLocalTarget(target))
                {
                    cmd.Visible = true;
                    cmd.Enabled = false;
                }
                else
                {
                    cmd.Enabled = true;
                    cmd.Visible = true;
                }
            }
        }

        private bool IsLocalTarget(string target)
        {
            return (target != Ads.AmsNetId.Local.ToString()) ? true : false;
        }
    }
}

