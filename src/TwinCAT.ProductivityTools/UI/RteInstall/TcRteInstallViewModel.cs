using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TwinCAT.Ads;
using TwinCAT.Remote;
using WPF.MVVM.Base;

namespace TwinCAT.Remote.ProductivityTools
{
    class TcRteInstallViewModel : ViewModelBase
    {
        public TcRteInstallViewModel(string target)
        {
            Target = target;

            try
            {
                TargetName = AmsRouter.ListRoutes().Where(x => x.NetId == Target).FirstOrDefault().Name;
            }
            catch { }

            InstallCommand = new AsyncRelayCommand(InstallAsync, CanInstall);
            SearchCommand = new AsyncRelayCommand(SearchAsync, CanSearch);

            Connections = new ObservableCollection<LocalAreaConnection>();
        }

        public async Task InitializeAsync()
        {
            await SearchAsync();
        }

        public IAsyncCommand InstallCommand { get; private set; }
        public IAsyncCommand SearchCommand { get; private set; }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;
            }   
        }

        private ObservableCollection<LocalAreaConnection> _connectionsList;
        public ObservableCollection<LocalAreaConnection> Connections
        {
            get => _connectionsList;
            private set
            {
                _connectionsList = value;
                OnPropertyChanged("Connections");
            }
        }

        private LocalAreaConnection _selectedItem;
        public LocalAreaConnection SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged("SelectedItem");
            }
        }

        private string _target = string.Empty;
        public string Target
        {
            get => _target;
            private set
            {
                _target = value;
                OnPropertyChanged("Target");
            }
        }

        private string _targetName = string.Empty;
        public string TargetName
        {
            get => _targetName;
            private set
            {
                _targetName = value;
                OnPropertyChanged("TargetName");
            }
        }

        private async Task InstallAsync()
        {
            try
            {
                IsBusy = true;

                using (var nm = new NetworkManager(Target))
                {
                    await nm.RteInstallAsync(SelectedItem);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanInstall()
        {
            return !IsBusy;
        }

        private async Task SearchAsync()
        {
            try
            {
                Connections.Clear();
                IsBusy = true;

                using (var nm = new NetworkManager(Target))
                {
                    var connections = await nm.ListConnectionsAsync(CancellationToken.None);

                    foreach (var conn in connections)
                    {
                        Connections.Add(conn);
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }

        }

        private bool CanSearch()
        {
            return !IsBusy;
        }
    }
}
