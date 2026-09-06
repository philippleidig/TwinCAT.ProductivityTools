using Community.VisualStudio.Toolkit;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Infrastructure;
using TwinCAT.ProductivityTools.Remote;
using TwinCAT.ProductivityTools.Routing;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>
	/// Opens the Beckhoff Device Manager of the target system in the default browser.
	/// </summary>
	[Command(PackageIds.DeviceManagerCommandId)]
	internal sealed class DeviceManagerCommand : TargetCommandBase<DeviceManagerCommand>
	{
		protected override string OperationName => "Device manager";

		protected override async Task ExecuteAsync(AmsNetId target, string targetName)
		{
			string url = DeviceManagerUrl.For(new TargetAddressResolver().Resolve(targetName));

			if (url == null)
			{
				await VS.MessageBox.ShowErrorAsync(
					Vsix.Name,
					$"No usable address could be determined for the target <{targetName}>. "
						+ "Add a route to the target, or give its route an address."
				);
				return;
			}

			// The shell decides what opens an https address, and that is the default browser.
			SystemProcessLauncher.Instance.Start(new ProcessLaunch(url, string.Empty, true));

			await Report.ShowStatusAsync($"Device Manager of target <{targetName}> opened: {url}.");
		}
	}
}
