using System;
using System.Threading;
using TCatSysManagerLib;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Services
{
	public class TargetSystemService : ITargetSystemService
	{
		private AmsNetId targetSystem;
		private ITcSysManager2 systemManager;

		//private SystemService systemService;

		private readonly TargetSelectionMonitor targetSelectionMonitor;

		public AmsNetId ActiveTargetSystem => targetSystem;

		public TargetSystemService(EnvDTE.DTE dte)
		{
			targetSelectionMonitor = new TargetSelectionMonitor(dte);
			targetSelectionMonitor.TargetSystemChanged += OnTargetSelectionChanged;
		}

		private void OnTargetSelectionChanged(object sender, TargetSystemChangedEventArgs e)
		{
			systemManager = e.SystemManager;
			targetSystem = e.TargetSystem;
		}

		public async Task ShutdownAsync()
		{
			if (systemManager == null)
			{
				return;
			}

			if (AmsNetId.Empty.Equals(targetSystem))
			{
				return;
			}

			await RemoteControl.ShutdownAsync(targetSystem, CancellationToken.None);
		}
	}
}
