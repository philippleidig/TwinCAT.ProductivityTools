using System.Windows;
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

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await viewModel.InitializeAsync();
        }

        private TcRteInstallViewModel viewModel;
    }
}
