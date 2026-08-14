using Community.VisualStudio.Toolkit;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Plc;

namespace TwinCAT.ProductivityTools.Commands
{
	[Command(PackageIds.RemoveCommentsCommandId)]
	internal sealed class RemoveCommentsCommand : PlcTextRewriteCommandBase<RemoveCommentsCommand>
	{
		private readonly ICommentRemover commentRemover = new CommentRemover();

		protected override string OperationName => "Remove all comments";

		protected override string Rewrite(string text) => commentRemover.Remove(text);
	}
}
