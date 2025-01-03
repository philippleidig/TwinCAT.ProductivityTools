using System;
using System.Windows;
using System.Windows.Controls;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Extensions;

namespace TwinCAT.ProductivityTools.ToolWindows
{
	public partial class IOMappingToolWindowControl : UserControl
	{
		private readonly IOMappingViewModel ViewModel;

		public IOMappingToolWindowControl()
		{
			InitializeComponent();
			ViewModel = new IOMappingViewModel();
			DataContext = ViewModel;

			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				try
				{
					ITcSysManager2 systemManager =
						await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();
					await ViewModel.InitializeAsync(systemManager);
				}
				catch (Exception ex)
				{
					VS.MessageBox.ShowError(
						Vsix.Name,
						"Failed to open TwinCAT IO tool window. \n" + ex.Message
					);
				}
			});
		}
	}
}
