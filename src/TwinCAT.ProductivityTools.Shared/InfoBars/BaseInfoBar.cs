using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.InfoBars
{
	public abstract class BaseInfoBar
	{
		public async Task ShowAsync()
		{
			if (!await ShouldShowAsync())
			{
				return;
			}

			var model = BuildModel();

			InfoBar infoBar = await VS.InfoBar.CreateAsync(model);
			infoBar.ActionItemClicked += OnActionItemClicked;

			await infoBar.TryShowInfoBarUIAsync();
		}

		protected abstract void OnActionItemClicked(object sender, InfoBarActionItemEventArgs args);

		protected abstract Task<bool> ShouldShowAsync();

		protected abstract InfoBarModel BuildModel();
	}
}
