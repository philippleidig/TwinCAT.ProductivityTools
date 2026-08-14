using Community.VisualStudio.Toolkit;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Plc;

namespace TwinCAT.ProductivityTools.Commands
{
	[Command(PackageIds.RemoveRegionsCommandId)]
	internal sealed class RemoveRegionsCommand : PlcTextRewriteCommandBase<RemoveRegionsCommand>
	{
		private readonly IRegionRemover regionRemover = new RegionRemover();

		protected override string OperationName => "Remove all regions";

		protected override string Rewrite(string text) => regionRemover.Remove(text);
	}
}
