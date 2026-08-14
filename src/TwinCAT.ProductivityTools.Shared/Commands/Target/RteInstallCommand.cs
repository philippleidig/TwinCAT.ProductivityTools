using Community.VisualStudio.Toolkit;
using TwinCAT.Ads;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>
	/// Lists the network adapters of the target that are compatible with the TwinCAT real time
	/// driver and allows installing the driver on them.
	/// </summary>
	[Command(PackageIds.RteInstallCommandId)]
	internal sealed class RteInstallCommand : TargetCommandBase<RteInstallCommand>
	{
		protected override string OperationName => "Real time ethernet driver";

		protected override Task ExecuteAsync(AmsNetId target, string targetName)
		{
			new TcRteInstallView(targetName).ShowModal();

			return Task.CompletedTask;
		}
	}
}
