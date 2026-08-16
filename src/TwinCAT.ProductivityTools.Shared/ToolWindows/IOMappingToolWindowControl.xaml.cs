using System;
using System.Windows;
using System.Windows.Controls;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Helpers;

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
			// The tool window can be reopened from the last IDE session before a solution is
			// loaded. Reporting to the output window keeps that case from greeting the user with a
			// modal dialog on every start.
			BackgroundWork.Run(
				async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					ITcSysManager2 systemManager =
						await VS.Solutions.GetActiveTwinCATProjectSystemManagerAsync();

					await ViewModel.InitializeAsync(systemManager);
				},
				"Failed to read the I/O mapping."
			);
		}
	}
}
