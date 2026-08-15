using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TwinCAT.Ads;

namespace TwinCAT.ProductivityTools
{
	class DeviceInfoViewModel : ObservableObject
	{
		public DeviceInfoViewModel(AmsNetId target)
		{
			Target = target;
			TargetName = NameOf(target);
		}

		/// <summary>
		/// Resolves the route name of the target, falling back to its AmsNetID.
		/// </summary>
		/// <remarks>
		/// A target does not have to be routed: the local system never is, and a target that was
		/// entered by hand may not be either. Neither case is an error, so the dialog shows the
		/// AmsNetID instead of an empty caption. Reading the routes touches the file system, which
		/// is why the failure is tolerated as well.
		/// </remarks>
		private static string NameOf(AmsNetId target)
		{
			if (target == null)
			{
				return string.Empty;
			}

			if (AmsNetId.Local.Equals(target))
			{
				return "Local";
			}

			try
			{
				string name = AmsRouter
					.ListRoutes()
					.FirstOrDefault(route => route.NetId == target.ToString())
					?.Name;

				return string.IsNullOrEmpty(name) ? target.ToString() : name;
			}
			catch (Exception)
			{
				return target.ToString();
			}
		}

		public async Task InitializeAsync()
		{
			IsBusy = true;
			Functions = new ObservableCollection<Function>();

			try
			{
				DeviceInfo = await RemoteControl.GetDeviceInfoAsync(Target, CancellationToken.None);

				var functions = await Function.ListFunctionsAsync(
					new Ads.AmsNetId(Target),
					CancellationToken.None
				);

				foreach (var function in functions)
				{
					Functions.Add(function);
				}
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
				OnPropertyChanged(nameof(IsBusy));
			}
		}

		private ObservableCollection<Function> _functions;
		public ObservableCollection<Function> Functions
		{
			get => _functions;
			private set
			{
				_functions = value;
				OnPropertyChanged(nameof(Functions));
			}
		}

		private TwinCAT.ProductivityTools.DeviceInfo _deviceInfo;
		public TwinCAT.ProductivityTools.DeviceInfo DeviceInfo
		{
			get => _deviceInfo;
			set
			{
				_deviceInfo = value;
				OnPropertyChanged(nameof(DeviceInfo));
			}
		}

		private AmsNetId _target;
		public AmsNetId Target
		{
			get => _target;
			private set
			{
				_target = value;
				OnPropertyChanged(nameof(Target));
			}
		}

		private string _targetName = string.Empty;
		public string TargetName
		{
			get => _targetName;
			private set
			{
				_targetName = value;
				OnPropertyChanged(nameof(TargetName));
			}
		}
	}
}
