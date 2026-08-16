using Community.VisualStudio.Toolkit;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Routing;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>Opens a remote desktop session to the target system.</summary>
	[Command(PackageIds.RemoteDesktopCommandId)]
	internal sealed class RemoteDesktopCommand : TargetCommandBase<RemoteDesktopCommand>
	{
		protected override string OperationName => "Remote desktop";

		protected override async Task ExecuteAsync(AmsNetId target, string targetName)
		{
			string address = new TargetAddressResolver().Resolve(targetName);

			if (string.IsNullOrEmpty(address))
			{
				await VS.MessageBox.ShowErrorAsync(
					Vsix.Name,
					$"No IP address could be determined for the target <{targetName}>."
				);
				return;
			}

			RemoteDesktop.Connect(address);
		}
	}
}
