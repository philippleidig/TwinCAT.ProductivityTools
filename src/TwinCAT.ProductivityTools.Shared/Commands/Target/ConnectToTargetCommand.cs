using System;
using System.Threading;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Infrastructure;
using TwinCAT.ProductivityTools.Options;
using TwinCAT.ProductivityTools.Remote;
using TwinCAT.ProductivityTools.Routing;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>
	/// Opens an interactive session on the target system - over remote desktop or over SSH,
	/// depending on the operating system the target runs.
	/// </summary>
	[Command(PackageIds.ConnectToTargetCommandId)]
	internal sealed class ConnectToTargetCommand : TargetCommandBase<ConnectToTargetCommand>
	{
		/// <summary>
		/// How long the target is given to describe itself before the user is asked instead.
		/// </summary>
		private static readonly TimeSpan OperatingSystemTimeout = TimeSpan.FromSeconds(5);

		protected override string OperationName => "Connect to target";

		protected override async Task ExecuteAsync(AmsNetId target, string targetName)
		{
			string address = TargetAddress.Normalize(
				new TargetAddressResolver().Resolve(targetName)
			);

			if (address == null)
			{
				await VS.MessageBox.ShowErrorAsync(
					Vsix.Name,
					$"No usable address could be determined for the target <{targetName}>. "
						+ "Add a route to the target, or give its route an address."
				);
				return;
			}

			TargetOperatingSystem operatingSystem = await ReadOperatingSystemAsync(target);

			if (operatingSystem == TargetOperatingSystem.WindowsCe)
			{
				await VS.MessageBox.ShowAsync(
					Vsix.Name,
					$"The target <{targetName}> runs Windows CE, which serves neither remote "
						+ "desktop nor SSH. Use the Device Manager to configure it."
				);
				return;
			}

			if (operatingSystem == TargetOperatingSystem.Unknown)
			{
				operatingSystem = await AskForOperatingSystemAsync(targetName);

				if (operatingSystem == TargetOperatingSystem.Unknown)
				{
					return;
				}
			}

			General options = await General.GetLiveInstanceAsync();

			ITargetConnection connection = new TargetConnectionFactory().For(
				operatingSystem,
				options.SshUserName
			);

			ProcessLaunch launch = connection.Build(address);

			if (launch == null)
			{
				await VS.MessageBox.ShowErrorAsync(Vsix.Name, connection.UnavailableMessage);
				return;
			}

			SystemProcessLauncher.Instance.Start(launch);

			await Report.ShowStatusAsync(
				$"{connection.DisplayName} session to target <{targetName}> ({address}) started."
			);
		}

		/// <summary>
		/// Reads the operating system of the target, reporting
		/// <see cref="TargetOperatingSystem.Unknown"/> for a target that does not answer.
		/// </summary>
		/// <remarks>
		/// The ADS client connects synchronously and this runs on the main thread, so an
		/// unreachable target would freeze the IDE until the ADS timeout expires. The call goes to
		/// a background thread and gets a deadline of its own.
		/// </remarks>
		private static async System.Threading.Tasks.Task<TargetOperatingSystem> ReadOperatingSystemAsync(
			AmsNetId target
		)
		{
			try
			{
				using (
					CancellationTokenSource timeout = new CancellationTokenSource(
						OperatingSystemTimeout
					)
				)
				{
					DeviceInfo device = await Task.Run(
						() => RemoteControl.GetDeviceInfoAsync(target, timeout.Token)
					);

					return TargetOperatingSystemDetector.Detect(device);
				}
			}
			catch (Exception)
			{
				// An offline target, a missing route, a firewall or an image whose system service
				// does not answer the device information request all mean the same thing here: the
				// operating system is unknown, and the user is the one who knows it.
				return TargetOperatingSystem.Unknown;
			}
		}

		/// <summary>
		/// Asks which session to open, reporting <see cref="TargetOperatingSystem.Unknown"/> when
		/// the user cancelled.
		/// </summary>
		private static async System.Threading.Tasks.Task<TargetOperatingSystem> AskForOperatingSystemAsync(
			string targetName
		)
		{
			VSConstants.MessageBoxResult answer = await VS.MessageBox.ShowAsync(
				Vsix.Name,
				$"The operating system of the target <{targetName}> could not be determined - "
					+ "the target may be offline."
					+ Environment.NewLine
					+ Environment.NewLine
					+ "Yes\tconnect with SSH (TwinCAT/BSD, TwinCAT/Linux)"
					+ Environment.NewLine
					+ "No\tconnect with remote desktop (Windows)",
				OLEMSGICON.OLEMSGICON_QUERY,
				OLEMSGBUTTON.OLEMSGBUTTON_YESNOCANCEL,
				OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND
			);

			switch (answer)
			{
				case VSConstants.MessageBoxResult.IDYES:
					return TargetOperatingSystem.Bsd;

				case VSConstants.MessageBoxResult.IDNO:
					return TargetOperatingSystem.Windows;

				default:
					return TargetOperatingSystem.Unknown;
			}
		}
	}
}
