using System;
using System.IO;
using System.Threading;
using Community.VisualStudio.Toolkit;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Installation;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>
	/// Runs <c>win8settick.bat</c> on the target, which sets the Windows timer resolution to 1 ms
	/// and is a prerequisite for a stable real time behaviour on many IPCs.
	/// </summary>
	[Command(PackageIds.SetTickCommandId)]
	internal sealed class SetTickCommand : TargetCommandBase<SetTickCommand>
	{
		protected override string OperationName => "Windows set tick";

		protected override string ConfirmationFor(string targetName) =>
			$"Run {TargetPaths.SetTickScriptName} on the target <{targetName}>?";

		protected override async Task ExecuteAsync(AmsNetId target, string targetName)
		{
			// The script lives in a different directory on 4024 and on 4026, and the target is a
			// remote machine whose file system cannot be probed. Its TwinCAT version decides.
			Version version = await ReadTwinCatVersionAsync(target);

			string script = TargetPaths.SetTickScript(version);

			await RemoteControl.StartProcessAsync(
				target,
				script,
				Path.GetDirectoryName(script),
				string.Empty,
				CancellationToken.None
			);

			await Report.ShowStatusAsync(
				$"{TargetPaths.SetTickScriptName} started on target <{targetName}>."
			);
		}

		private static async System.Threading.Tasks.Task<Version> ReadTwinCatVersionAsync(
			AmsNetId target
		)
		{
			try
			{
				DeviceInfo deviceInfo = await RemoteControl.GetDeviceInfoAsync(
					target,
					CancellationToken.None
				);

				return deviceInfo?.TwinCATVersion;
			}
			catch (Exception)
			{
				// An unreachable device info service must not stop the command. The caller falls
				// back to the 4024 layout, which is still the most common one.
				return null;
			}
		}
	}
}
