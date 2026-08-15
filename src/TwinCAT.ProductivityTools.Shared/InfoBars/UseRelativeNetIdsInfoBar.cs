using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Helpers;

namespace TwinCAT.ProductivityTools.InfoBars
{
	internal class UseRelativeNetIdsInfoBar : BaseInfoBar
	{
		protected override async Task<bool> ShouldShowAsync()
		{
			ITcSysManager2 systemManager =
				await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();

			if (systemManager == null)
			{
				return false;
			}

			bool isRelativeNetIdsEnabled = systemManager.IsUseRelativeNetIdsEnabled();

			return !isRelativeNetIdsEnabled;
		}

		protected override InfoBarModel BuildModel()
		{
			return new InfoBarModel(
				new[]
				{
					new InfoBarTextSpan(
						"This TwinCAT project stores absolute AmsNetIDs. Relative AmsNetIDs keep "
							+ "the engineering station out of the project files. "
					),
					new InfoBarButton("Use relative NetIds")
				},
				KnownMonikers.SettingsGroupWarning,
				true
			);
		}

		protected override void OnActionItemClicked(object sender, InfoBarActionItemEventArgs args)
		{
			BackgroundWork.Run(
				async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					ITcSysManager2 systemManager =
						await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();

					if (systemManager == null)
					{
						return;
					}

					systemManager.EnableUseRelativeNetIds();

					await VS.Solutions.SaveAsync();

					await Report.ShowStatusAsync("Relative AmsNetIDs are now enabled.");
				},
				"Failed to enable relative AmsNetIDs."
			);

			ThreadHelper.ThrowIfNotOnUIThread();
			args.InfoBarUIElement?.Close();
		}
	}
}
