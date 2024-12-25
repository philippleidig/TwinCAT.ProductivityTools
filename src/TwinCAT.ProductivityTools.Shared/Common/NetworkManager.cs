using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace TwinCAT.ProductivityTools
{
    public class NetworkManager : IDisposable
    {
        public AmsNetId Target { get; private set; }

        private AdsClient _adsClient { get; set; }

        public NetworkManager(AmsNetId target)
        {
            Target = target;
            Initialize();
        }

        public NetworkManager(string target)
        {
            Target = new AmsNetId(target);
            Initialize();
        }

        private void Initialize()
        {
            _adsClient = new AdsClient();
            _adsClient.Connect(Target, AmsPort.SystemService);

            if (!_adsClient.IsConnected) throw new Exception("Could not connect to target " + Target.ToString());
        }

        public async Task<List<LocalAreaConnection>> ListConnectionsAsync(CancellationToken cancel)
        {
            var adapters = new List<LocalAreaConnection>();

            var count = await GetAdaptersCountAsync();

            var buffer = new Memory<byte>(new byte[count * 640]);
            var res = await _adsClient.ReadAsync(701, 1, buffer, cancel);
            res.ThrowOnError();

            // Slice byte array into 640 byte parts
            foreach (byte[] slice in buffer.ToArray().Slices(640))
            {
                var guid = new Guid(System.Text.Encoding.UTF8.GetString(slice.Skip<byte>(8).Take<byte>(260).ToArray<byte>()).Split(new char[] { '{', '}' })[1]);
                var desc = ArrayHelpers.ByteArrayToString(slice.Skip<byte>(268).Take<byte>(131).ToArray<byte>());
                var macAddr = new PhysicalAddress(slice.Skip<byte>(404).Take<byte>(6).ToArray<byte>());
                var type = BitConverter.ToInt16(slice.Skip<byte>(416).Take<byte>(4).ToArray<byte>(), 0);
                var ipAddr = ArrayHelpers.ByteArrayToString(slice.Skip<byte>(432).Take<byte>(15).ToArray<byte>());
                var subnet = ArrayHelpers.ByteArrayToString(slice.Skip<byte>(448).Take<byte>(15).ToArray<byte>());
                var gateway = ArrayHelpers.ByteArrayToString(slice.Skip<byte>(472).Take<byte>(15).ToArray<byte>());
                var dhcp = BitConverter.ToBoolean(slice.Skip<byte>(420).Take<byte>(8).ToArray<byte>(), 0);

                if (type == 6) // Ethernet Adapter
                {
                    var adapter = new LocalAreaConnection
                    {
                        Description = desc,
                        InstanceId = guid,
                        MacAddress = macAddr,
                        IpAddress = IPAddress.Parse(ipAddr),
                        SubnetMask = IPAddress.Parse(subnet),
                        Gateway = IPAddress.Parse(gateway),
                        DHCP = dhcp
                    };

                    adapters.Add(adapter);
                }

            }

            await adapters.ForEachAsync(async (adapter) =>
            {
                // Get adapters name out of windows registry
                adapter.Name = await GetAdaptersNameAsync(adapter.InstanceId);
            });

            return adapters;
        }

        public async Task<int> GetAdaptersCountAsync()
        {
            var count = 0;

            var result = await _adsClient.ReadAnyAsync<int>(701, 1, CancellationToken.None);
            result.ThrowOnError();

            count = result.Value / 640;
            if (count == 0) throw new Exception(string.Concat("Can not find any network adapter on target: ", Target.ToString()));

            return count;
        }

        private async Task<string> GetAdaptersNameAsync(Guid instanceId)
        {
            var query = string.Concat(@"SYSTEM\CurrentControlSet\Control\Network\{4D36E972-E325-11CE-BFC1-08002bE10318}\{", instanceId.ToString().ToUpper(), @"}\Connection");
            var result = await TwinCAT.ProductivityTools.AdsRegistry.QueryValueAsync(Target, query, "Name");
            return result;
        }

        public Task RteInstallAsync(LocalAreaConnection adapter)
        {
            return RteInstallAsync(adapter.Name);
        }
        public async Task RteInstallAsync(string adapterName)
        {
            var path = @"C:\TwinCAT\3.1\System\TcRteInstall.exe";
            var dir = @"C:\TwinCAT\3.1\System";
            var cmd = string.Concat("-r installnic \"", adapterName, "\"");

            await RemoteControl.StartProcessAsync(Target, path, dir, cmd, CancellationToken.None);
        }

        ~NetworkManager()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                _adsClient?.Dispose();
                _adsClient = null;
            }
        }
    }
}
