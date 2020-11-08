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
using WPF.MVVM.Base;
using TwinCAT.Remote;

namespace TwinCAT.Remote.ProductivityTools
{
    class DeviceInfoViewModel : ViewModelBase
    {
        public DeviceInfoViewModel(AmsNetId target)
        {
            Target = target;
            try
            {
                TargetName = AmsRouter.ListRoutes().Where(x => x.NetId == Target.ToString()).FirstOrDefault().Name;
            }
            catch { }
        }

        public async Task InitializeAsync ()
        {
            IsBusy = true;
            Functions = new ObservableCollection<Function>();

            try
            {
                DeviceInfo = await RemoteControl.GetDeviceInfoAsync(Target, CancellationToken.None);
             
                var functions = await Function.ListFunctionsAsync(new Ads.AmsNetId(Target), CancellationToken.None);

                foreach( var function in functions)
                {
                    Functions.Add(function);
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

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;
            }   
        }

        private ObservableCollection<Function> _functions;
        public ObservableCollection<Function> Functions
        {
            get => _functions;
            private set
            {
                _functions = value;
                OnPropertyChanged("Functions");
            }
        }

        private TwinCAT.Remote.DeviceInfo _deviceInfo;
        public TwinCAT.Remote.DeviceInfo DeviceInfo
        {
            get => _deviceInfo;
            set
            {
                _deviceInfo = value;
                OnPropertyChanged("DeviceInfo");
            }
        }

        private AmsNetId _target;
        public AmsNetId Target
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
    }
}
