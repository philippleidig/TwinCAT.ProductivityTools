using System;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Extensions;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>
	/// Shared behaviour of the commands that talk to the target system of the active TwinCAT
	/// project.
	/// </summary>
	/// <remarks>
	/// Every one of these commands needs the same three things: an active TwinCAT project, a
	/// target AmsNetID that actually parses, and an error path that reports instead of throwing.
	/// Doing that in one place also removed several copies of an ignored
	/// <c>AmsNetId.TryParse</c> result, which used to hand a null id to the ADS client.
	/// </remarks>
	internal abstract class TargetCommandBase<TCommand> : BaseCommand<TCommand>
		where TCommand : TargetCommandBase<TCommand>, new()
	{
		/// <summary>Short description used in the status bar and in error messages.</summary>
		protected abstract string OperationName { get; }

		protected abstract Task ExecuteAsync(AmsNetId target, string targetName);

		/// <summary>
		/// Asks the user before the target is touched. Returns <c>null</c> when no confirmation is
		/// required.
		/// </summary>
		protected virtual string ConfirmationFor(string targetName) => null;

		protected override void BeforeQueryStatus(EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			bool hasTarget = false;

			try
			{
				ITcSysManager2 systemManager = VS.Solutions.GetActiveTwinCATProjectSystemManager();

				hasTarget = AmsNetId.TryParse(systemManager?.GetTargetNetId(), out AmsNetId _);
			}
			catch (Exception)
			{
				// The system manager throws while a project is loading or unloading.
			}

			Command.Visible = hasTarget;
			Command.Enabled = hasTarget;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			ITcSysManager2 systemManager =
				await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();

			if (systemManager == null)
			{
				await VS.MessageBox.ShowAsync(
					Vsix.Name,
					"The solution does not contain an active TwinCAT XAE project."
				);
				return;
			}

			string targetName = systemManager.GetTargetNetId();

			if (!AmsNetId.TryParse(targetName, out AmsNetId target))
			{
				await VS.MessageBox.ShowErrorAsync(
					Vsix.Name,
					"The active TwinCAT project does not have a valid target AmsNetID."
				);
				return;
			}

			string confirmation = ConfirmationFor(targetName);

			if (
				confirmation != null
				&& !await VS.MessageBox.ShowConfirmAsync(Vsix.Name, confirmation)
			)
			{
				return;
			}

			try
			{
				await ExecuteAsync(target, targetName);
			}
			catch (Exception ex)
			{
				await Helpers.Report.FailureAsync(
					$"{OperationName} failed on target <{targetName}>.",
					ex
				);
			}
		}
	}
}
