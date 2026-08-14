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
			ThreadHelper
				.JoinableTaskFactory.RunAsync(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					try
					{
						ITcSysManager2 systemManager =
							await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();

						if (systemManager == null)
						{
							return;
						}

						systemManager.EnableUseRelativeNetIds();

						await VS.Solutions.SaveAsync();

						await Report.ShowStatusAsync("Relative AmsNetIDs are now enabled.");
					}
					catch (Exception ex)
					{
						await Report.FailureAsync("Failed to enable relative AmsNetIDs.", ex);
					}
				})
				.FireAndForget();

			ThreadHelper.ThrowIfNotOnUIThread();
			args.InfoBarUIElement?.Close();
		}
	}
}
