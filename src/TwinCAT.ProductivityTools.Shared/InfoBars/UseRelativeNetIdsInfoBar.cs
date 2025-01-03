using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Extensions;

namespace TwinCAT.ProductivityTools.InfoBars
{
	internal class UseRelativeNetIdsInfoBar : BaseInfoBar
	{
		protected override async Task<bool> ShouldShowAsync()
		{
			ITcSysManager2 systemManager =
				await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();

			bool isRelativeNetIdsEnabled = systemManager.IsUseRelativeNetIdsEnabled();

			return !isRelativeNetIdsEnabled;
		}

		protected override InfoBarModel BuildModel()
		{
			return new InfoBarModel(
				new[]
				{
					new InfoBarTextSpan(
						"TwinCAT ProductivityTools Recommendation: Use relative AmsNetIDs !		"
					),
					new InfoBarButton("ACTIVATE")
				},
				KnownMonikers.SettingsGroupWarning,
				true
			);
		}

		protected override void OnActionItemClicked(object sender, InfoBarActionItemEventArgs args)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				try
				{
					ITcSysManager2 systemManager =
						await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();
					systemManager.EnableUseRelativeNetIds();

					await VS.Solutions.SaveAsync();

					await VS.StatusBar.ShowMessageAsync("Successfully enabled relative AmsNetIDs.");
				}
				catch (Exception ex)
				{
					await VS.StatusBar.ShowMessageAsync(
						"Failed to enable relative AmsNetIds. See output window for detailed information."
					);

					IOutputWindowPane outputWindowPane = await VS.GetRequiredServiceAsync<
						IOutputWindowPane,
						IOutputWindowPane
					>();
					await outputWindowPane.WriteLineAsync(ex.Message);
				}
			});

			ThreadHelper.ThrowIfNotOnUIThread();
			args.InfoBarUIElement.Close();
		}
	}
}
