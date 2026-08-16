using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Imaging;

namespace TwinCAT.ProductivityTools.ToolWindows
{
	internal class IOMappingToolWindow : BaseToolWindow<IOMappingToolWindow>
	{
		public override string GetTitle(int toolWindowId) => "TwinCAT IO Mapping";

		public override Type PaneType => typeof(Pane);

		public override Task<FrameworkElement> CreateAsync(
			int toolWindowId,
			CancellationToken cancellationToken
		)
		{
			return Task.FromResult<FrameworkElement>(new IOMappingToolWindowControl());
		}

		[Guid("0b8deafd-fbaf-4ada-9c19-154c6524d70b")]
		internal class Pane : ToolkitToolWindowPane
		{
			public Pane()
			{
				BitmapImageMoniker = KnownMonikers.ToolWindow;
			}
		}
	}
}
