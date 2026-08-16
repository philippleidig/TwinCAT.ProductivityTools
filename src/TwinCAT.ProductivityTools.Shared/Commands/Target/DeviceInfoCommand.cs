using Community.VisualStudio.Toolkit;
using TwinCAT.Ads;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>Shows hardware, image and TwinCAT version information of the target.</summary>
	[Command(PackageIds.DeviceInfoCommandId)]
	internal sealed class DeviceInfoCommand : TargetCommandBase<DeviceInfoCommand>
	{
		protected override string OperationName => "Device info";

		protected override Task ExecuteAsync(AmsNetId target, string targetName)
		{
			new DeviceInfoView(target).ShowModal();

			return Task.CompletedTask;
		}
	}
}
