using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace TwinCAT.ProductivityTools.QuickInfo
{
	[Export(typeof(IAsyncQuickInfoSourceProvider))]
	[Name("Line Async Quick Info Provider")]
	[ContentType("any")]
	[Order]
	internal sealed class LineAsyncQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
	{
		public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
		{
			// This ensures only one instance per textbuffer is created
			return textBuffer.Properties.GetOrCreateSingletonProperty(
				() => new LineAsyncQuickInfoSource(textBuffer)
			);
		}
	}
}
