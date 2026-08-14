using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Routing;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools
{
	/// <summary>
	/// Lists the network adapters of a target and installs the real time driver on one of them.
	/// </summary>
	internal class TcRteInstallViewModel : ObservableObject
	{
		private readonly AmsNetId target;

		/// <summary>
		/// TwinCAT version of the target. It decides where <c>TcRteInstall.exe</c> lives, because
		/// 4026 moved the system directory. It is read together with the adapter list.
		/// </summary>
		private Version targetVersion;

		public TcRteInstallViewModel(string target)
		{
			Target = target ?? string.Empty;

			AmsNetId netId;

			if (AmsNetIdParser.TryParse(Target, out netId))
			{
				this.target = netId;
			}

			TargetName = ResolveName(this.target);

			Connections = new ObservableCollection<LocalAreaConnection>();

			InstallCommand = new AsyncRelayCommand(InstallAsync, CanInstall);
			SearchCommand = new AsyncRelayCommand(SearchAsync, CanSearch);
		}

		public Task InitializeAsync()
		{
			return SearchAsync();
		}

		public IAsyncRelayCommand InstallCommand { get; }

		public IAsyncRelayCommand SearchCommand { get; }

		public ObservableCollection<LocalAreaConnection> Connections { get; }

		private bool isBusy;

		public bool IsBusy
		{
			get => isBusy;
			private set
			{
				if (SetProperty(ref isBusy, value))
				{
					// The commands are bound to buttons that have to grey out while a request is
					// running. Without this the user can start a second install on top of the
					// first one.
					InstallCommand.NotifyCanExecuteChanged();
					SearchCommand.NotifyCanExecuteChanged();
				}
			}
		}

		private LocalAreaConnection selectedItem;

		public LocalAreaConnection SelectedItem
		{
			get => selectedItem;
			set
			{
				if (SetProperty(ref selectedItem, value))
				{
					InstallCommand.NotifyCanExecuteChanged();
				}
			}
		}

		private string targetText = string.Empty;

		public string Target
		{
			get => targetText;
			private set => SetProperty(ref targetText, value);
		}

		private string targetName = string.Empty;

		public string TargetName
		{
			get => targetName;
			private set => SetProperty(ref targetName, value);
		}

		private string status = string.Empty;

		public string Status
		{
			get => status;
			private set => SetProperty(ref status, value);
		}

		/// <summary>
		/// Resolves the route name of the target, falling back to its AmsNetId.
		/// </summary>
		private static string ResolveName(AmsNetId netId)
		{
			if (netId == null)
			{
				return string.Empty;
			}

			if (netId.Equals(AmsNetId.Local))
			{
				return "Local";
			}

			try
			{
				string name = AmsRouter
					.ListRoutes()
					.FirstOrDefault(route => netId.ToString().Equals(route.NetId))
					?.Name;

				return string.IsNullOrEmpty(name) ? netId.ToString() : name;
			}
			catch (Exception)
			{
				// The static routes file is optional and may be unreadable. The AmsNetId is a
				// perfectly usable caption on its own.
				return netId.ToString();
			}
		}

		private async Task InstallAsync()
		{
			LocalAreaConnection adapter = SelectedItem;

			if (target == null || adapter == null)
			{
				return;
			}

			try
			{
				IsBusy = true;
				Status = $"Installing the real time driver on {adapter.Name}...";

				using (var manager = new NetworkManager(target))
				{
					await manager.RteInstallAsync(adapter, targetVersion, CancellationToken.None);
				}

				Status =
					$"The real time driver was requested for {adapter.Name}. "
					+ "The target applies it on its next restart.";
			}
			catch (Exception exception)
			{
				Status = "The installation failed.";

				await Report.FailureAsync("Failed to install the real time driver.", exception);
			}
			finally
			{
				IsBusy = false;
			}
		}

		private bool CanInstall()
		{
			return !IsBusy && target != null && SelectedItem != null;
		}

		private async Task SearchAsync()
		{
			if (target == null)
			{
				Status = $"'{Target}' is not a valid AmsNetId.";
				return;
			}

			try
			{
				IsBusy = true;
				Status = "Reading the network configuration...";

				Connections.Clear();
				SelectedItem = null;

				DeviceInfo device = await RemoteControl.GetDeviceInfoAsync(
					target,
					CancellationToken.None
				);

				targetVersion = device.TwinCATVersion;

				using (var manager = new NetworkManager(target))
				{
					foreach (
						LocalAreaConnection connection in await manager.ListConnectionsAsync(
							CancellationToken.None
						)
					)
					{
						Connections.Add(connection);
					}
				}

				Status = $"{Connections.Count} adapter(s) found.";
			}
			catch (Exception exception)
			{
				Status = "The network configuration could not be read.";

				await Report.FailureAsync("Failed to read the network configuration.", exception);
			}
			finally
			{
				IsBusy = false;
			}
		}

		private bool CanSearch()
		{
			return !IsBusy && target != null;
		}
	}
}
