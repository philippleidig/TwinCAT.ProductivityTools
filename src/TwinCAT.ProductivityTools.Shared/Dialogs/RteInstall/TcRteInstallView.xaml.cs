using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using TwinCAT.Ads;

namespace TwinCAT.ProductivityTools
{
	/// <summary>
	/// Interaktionslogik für TcRteInstallView.xaml
	/// </summary>
	public partial class TcRteInstallView : BaseDialogWindow
	{
		public TcRteInstallView(string target)
		{
			InitializeComponent();

			this.Title = "TcRteInstall Remote";

			viewModel = new TcRteInstallViewModel(target);
			DataContext = viewModel;

			Loaded += OnLoaded;
		}

		// A dialog must not be opened with "async void". An exception from the load would
		// otherwise be raised on the message pump and take the whole IDE down instead of the
		// dialog.
		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Microsoft
				.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
				{
					try
					{
						await viewModel.InitializeAsync();
					}
					catch (System.Exception ex)
					{
						await TwinCAT.ProductivityTools.Helpers.Report.FailureAsync(
							"Failed to read the target information.",
							ex
						);
					}
				})
				.FireAndForget();
		}

		private TcRteInstallViewModel viewModel;
	}
}
