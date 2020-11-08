using System;
using System.Windows;
using TwinCAT.Ads;

namespace TwinCAT.Remote.ProductivityTools
{
    public partial class DeviceInfoView : BaseDialogWindow
    {
        public DeviceInfoView(AmsNetId target)
        {
            InitializeComponent();

            this.Title = "Device Info Remote";

            viewModel = new DeviceInfoViewModel(target);
            DataContext = viewModel;

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await viewModel.InitializeAsync();
        }

        private DeviceInfoViewModel viewModel;
    }
}
