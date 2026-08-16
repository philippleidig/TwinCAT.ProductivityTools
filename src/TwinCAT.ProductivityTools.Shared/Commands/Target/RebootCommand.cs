using System.Threading;
using Community.VisualStudio.Toolkit;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Helpers;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>Reboots the target system.</summary>
	[Command(PackageIds.RebootCommandId)]
	internal sealed class RebootCommand : TargetCommandBase<RebootCommand>
	{
		protected override string OperationName => "Reboot";

		protected override string ConfirmationFor(string targetName) =>
			$"Reboot the target <{targetName}>?";

		protected override async Task ExecuteAsync(AmsNetId target, string targetName)
		{
			await RemoteControl.RebootAsync(target, CancellationToken.None);

			await Report.ShowStatusAsync($"Reboot triggered on target <{targetName}>.");
		}
	}
}
