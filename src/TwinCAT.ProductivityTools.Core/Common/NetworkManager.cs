using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Installation;

namespace TwinCAT.ProductivityTools
{
	/// <summary>
	/// Reads the network configuration of a TwinCAT target and installs the real time driver on
	/// one of its adapters.
	/// </summary>
	public class NetworkManager : IDisposable
	{
		private const string RteInstallExecutable = "TcRteInstall.exe";

		private AdsClient client;

		public AmsNetId Target { get; }

		public NetworkManager(AmsNetId target)
		{
			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			Target = target;

			Connect();
		}

		public NetworkManager(string target)
			: this(Parse(target)) { }

		private static AmsNetId Parse(string target)
		{
			AmsNetId netId;

			if (!Routing.AmsNetIdParser.TryParse(target, out netId))
			{
				throw new ArgumentException($"'{target}' is not an AmsNetId.", nameof(target));
			}

			return netId;
		}

		private void Connect()
		{
			client = new AdsClient();

			try
			{
				client.Connect(Target, AmsPort.SystemService);

				if (!client.IsConnected)
				{
					throw new AdsException($"Could not connect to target {Target}.");
				}
			}
			catch
			{
				// The client owns a socket. Leaking it would keep an ADS port allocated for the
				// rest of the session.
				client.Dispose();
				client = null;

				throw;
			}
		}

		private AdsClient RequireClient()
		{
			if (client == null)
			{
				throw new ObjectDisposedException(nameof(NetworkManager));
			}

			return client;
		}

		/// <summary>
		/// Returns every Ethernet adapter of the target, including its Windows connection name.
		/// </summary>
		public async Task<List<LocalAreaConnection>> ListConnectionsAsync(CancellationToken cancel)
		{
			AdsClient ads = RequireClient();

			int count = await GetAdaptersCountAsync(cancel);

			byte[] response = new byte[count * NetworkAdapterParser.RecordSize];

			ResultReadBytes result = await ads.ReadAsync(701, 1, response.Length, cancel);

			result.ThrowOnError();

			result.Data.CopyTo(response);

			var adapters = new List<LocalAreaConnection>(
				NetworkAdapterParser.Parse(response, result.ReadBytes)
			);

			foreach (LocalAreaConnection adapter in adapters)
			{
				// The service reports the adapter description, not the name the user sees in the
				// network settings. That name only exists in the registry of the target.
				adapter.Name = await GetAdapterNameAsync(adapter.InstanceId, cancel);
			}

			return adapters;
		}

		/// <summary>
		/// Returns how many adapter records the target will report.
		/// </summary>
		public async Task<int> GetAdaptersCountAsync(
			CancellationToken cancel = default(CancellationToken)
		)
		{
			ResultValue<int> result = await RequireClient().ReadAnyAsync<int>(701, 1, cancel);

			result.ThrowOnError();

			int count = result.Value / NetworkAdapterParser.RecordSize;

			if (count <= 0)
			{
				throw new AdsException($"Target {Target} reports no network adapter.");
			}

			return count;
		}

		/// <summary>
		/// Installs the real time driver on an adapter.
		/// </summary>
		public Task RteInstallAsync(
			LocalAreaConnection adapter,
			Version twinCatVersion,
			CancellationToken cancel = default(CancellationToken)
		)
		{
			if (adapter == null)
			{
				throw new ArgumentNullException(nameof(adapter));
			}

			return RteInstallAsync(adapter.Name, twinCatVersion, cancel);
		}

		/// <summary>
		/// Installs the real time driver on an adapter.
		/// </summary>
		/// <param name="adapterName">Windows connection name of the adapter.</param>
		/// <param name="twinCatVersion">
		/// TwinCAT version of the target. It decides where the tool lives, because 4026 moved the
		/// system directory out of <c>C:\TwinCAT</c>.
		/// </param>
		/// <param name="cancel">Cancellation token.</param>
		public Task RteInstallAsync(
			string adapterName,
			Version twinCatVersion,
			CancellationToken cancel = default(CancellationToken)
		)
		{
			if (string.IsNullOrWhiteSpace(adapterName))
			{
				throw new ArgumentException("An adapter name is required.", nameof(adapterName));
			}

			string directory = TargetPaths.SystemDirectory(twinCatVersion);

			return RemoteControl.StartProcessAsync(
				Target,
				Path.Combine(directory, RteInstallExecutable),
				directory,
				$"-r installnic \"{adapterName}\"",
				cancel
			);
		}

		/// <summary>
		/// Reads the connection name of an adapter from the registry of the target.
		/// </summary>
		private async Task<string> GetAdapterNameAsync(Guid instanceId, CancellationToken cancel)
		{
			// The class GUID below is the Windows network adapter class and is the same on every
			// machine.
			string key =
				@"SYSTEM\CurrentControlSet\Control\Network\"
				+ @"{4D36E972-E325-11CE-BFC1-08002bE10318}\"
				+ instanceId.ToString("B").ToUpperInvariant()
				+ @"\Connection";

			try
			{
				return await AdsRegistry.QueryValueAsync(Target, key, "Name", cancel);
			}
			catch (AdsErrorException)
			{
				// A hidden or half removed adapter has no connection key. Its description is still
				// worth showing, so the listing continues without a name.
				return string.Empty;
			}
		}

		public void Dispose()
		{
			client?.Dispose();
			client = null;
		}
	}
}
