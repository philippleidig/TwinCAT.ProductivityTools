using System;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Extensions;

namespace TwinCAT.ProductivityTools.Services
{
	internal class TargetSelectionMonitor
	{
		private const string TwinCATXaeBaseCommandSet = "{40EE08E0-8FB4-46E9-BAAB-100E60019B7B}";
		private const int TwinCATXaeBaseTargetSelectionID = 4373;

		private ITcSysManager2 systemManager;
		private AmsNetId targetSystem;

		private EnvDTE.DTE _dte;
		private EnvDTE.CommandEvents _events;

		public event EventHandler<TargetSystemChangedEventArgs> TargetSystemChanged;

		protected virtual void OnTargetSystemChanged(AmsNetId target, ITcSysManager2 systemManager)
		{
			TargetSystemChanged?.Invoke(
				this,
				new TargetSystemChangedEventArgs
				{
					TargetSystem = target,
					SystemManager = systemManager
				}
			);
		}

		public TargetSelectionMonitor(EnvDTE.DTE dte)
		{
			_dte = dte;
			_events = dte.Events.CommandEvents;
			_events.AfterExecute += OnAfterExecute;

			UpdateTargetSystem();
		}

		private void OnAfterExecute(string Guid, int ID, object CustomIn, object CustomOut)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				// TwinCAT Target Selection Changed
				if (
					!Guid.Equals(TwinCATXaeBaseCommandSet)
					|| !ID.Equals(TwinCATXaeBaseTargetSelectionID)
				)
				{
					return;
				}

				UpdateTargetSystem();
			}
			catch (ArgumentException) { }
		}

		private void UpdateTargetSystem()
		{
			ThreadHelper
				.JoinableTaskFactory.RunAsync(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					systemManager = await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();

					if (systemManager != null)
					{
						AmsNetId.TryParse(systemManager.GetTargetNetId(), out AmsNetId target);

						if (target != null)
						{
							if (!target.Equals(targetSystem))
							{
								targetSystem = target;
								OnTargetSystemChanged(targetSystem, systemManager);
							}
						}
					}
				})
				.FireAndForget();
		}
	}

	public class TargetSystemChangedEventArgs : EventArgs
	{
		public AmsNetId TargetSystem { get; set; }
		public ITcSysManager2 SystemManager { get; set; }
	}
}
