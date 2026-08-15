using System;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using TwinCAT.Ads;

namespace TwinCAT.ProductivityTools
{
	public partial class DeviceInfoView : BaseDialogWindow
	{
		public DeviceInfoView(AmsNetId target)
		{
			InitializeComponent();

			this.Title = "Device Info";

			viewModel = new DeviceInfoViewModel(target);
			DataContext = viewModel;

			Loaded += OnLoaded;
		}

		// A dialog must not be opened with "async void". An exception from the load would
		// otherwise be raised on the message pump and take the whole IDE down instead of the
		// dialog.
		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Helpers.BackgroundWork.Run(
				() => viewModel.InitializeAsync(),
				"Failed to read the target information."
			);
		}

		private DeviceInfoViewModel viewModel;
	}
}
