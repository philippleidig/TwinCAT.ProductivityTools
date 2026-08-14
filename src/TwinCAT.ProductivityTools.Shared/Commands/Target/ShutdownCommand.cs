using System.Threading;
using Community.VisualStudio.Toolkit;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Helpers;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>Shuts the target system down.</summary>
	[Command(PackageIds.ShutdownCommandId)]
	internal sealed class ShutdownCommand : TargetCommandBase<ShutdownCommand>
	{
		protected override string OperationName => "Shutdown";

		protected override string ConfirmationFor(string targetName) =>
			$"Shut down the target <{targetName}>?";

		protected override async Task ExecuteAsync(AmsNetId target, string targetName)
		{
			await RemoteControl.ShutdownAsync(target, CancellationToken.None);

			await Report.ShowStatusAsync($"Shutdown triggered on target <{targetName}>.");
		}
	}
}
