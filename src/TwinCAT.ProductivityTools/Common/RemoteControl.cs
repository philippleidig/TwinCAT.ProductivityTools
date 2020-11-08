using System;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace TwinCAT.Remote
{
    public static class RemoteControl
    {
        public static void StartProcess(AmsNetId target, string path, string dir, string args)
        {
            byte[] Data = new byte[777];

            BitConverter.GetBytes(path.Length).CopyTo(Data, 0);
            BitConverter.GetBytes(dir.Length).CopyTo(Data, 4);
            BitConverter.GetBytes(args.Length).CopyTo(Data, 8);

            System.Text.Encoding.ASCII.GetBytes(path).CopyTo(Data, 12);
            System.Text.Encoding.ASCII.GetBytes(dir).CopyTo(Data, 12 + path.Length + 1);
            System.Text.Encoding.ASCII.GetBytes(args).CopyTo(Data, 12 + path.Length + 1 + dir.Length + 1);

            ReadOnlyMemory<byte> buffer = new ReadOnlyMemory<byte>(Data);

            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));
                client.Write(500, 0, buffer);
                client.Dispose();
            }
        }
        public static async Task StartProcessAsync(AmsNetId target, string path, string directory, string args, CancellationToken cancel)
        {
            byte[] Data = new byte[777];

            BitConverter.GetBytes(path.Length).CopyTo(Data, 0);
            BitConverter.GetBytes(directory.Length).CopyTo(Data, 4);
            BitConverter.GetBytes(args.Length).CopyTo(Data, 8);

            System.Text.Encoding.ASCII.GetBytes(path).CopyTo(Data, 12);
            System.Text.Encoding.ASCII.GetBytes(directory).CopyTo(Data, 12 + path.Length + 1);
            System.Text.Encoding.ASCII.GetBytes(args).CopyTo(Data, 12 + path.Length + 1 + directory.Length + 1);

            ReadOnlyMemory<byte> buffer = new ReadOnlyMemory<byte>(Data);

            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));
                var res = await client.WriteAsync(500, 0, buffer, cancel);
                res.ThrowOnError();
            }
        }
        public static void Shutdown(AmsNetId target, TimeSpan delay)
        {
            var data = BitConverter.GetBytes((int)delay.TotalSeconds);
            var buffer = new ReadOnlyMemory<byte>(data);

            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));
                client.WriteControl(new StateInfo(AdsState.Shutdown, 0), buffer);
            }
        }
        public static async Task ShutdownAsync(AmsNetId target, TimeSpan delay, CancellationToken cancel)
        {
            var data = BitConverter.GetBytes((int)delay.TotalSeconds);
            var buffer = new ReadOnlyMemory<byte>(data);

            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));
                var res = await client.WriteControlAsync(AdsState.Shutdown, 0, buffer, cancel);
                res.ThrowOnError();
            }
        }
        public static void Reboot(AmsNetId target, TimeSpan delay)
        {
            var data = BitConverter.GetBytes((int)delay.TotalSeconds);
            var buffer = new ReadOnlyMemory<byte>(data);

            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));
                client.WriteControl(new StateInfo(AdsState.Shutdown, 1), buffer);
            }
        }
        public static async Task RebootAsync(AmsNetId target, TimeSpan delay, CancellationToken cancel)
        {
            var data = BitConverter.GetBytes((int)delay.TotalSeconds);
            var buffer = new ReadOnlyMemory<byte>(data);

            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));
                await client.WriteControlAsync(AdsState.Shutdown, 1, buffer, cancel);
            }
        }
        public static void Config(AmsNetId target)
        {
            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));
                client.WriteControl(new StateInfo(AdsState.Config, 0));
            }
        }
        public static async Task ConfigAsync(AmsNetId target, CancellationToken cancel)
        {
            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));
                await client.WriteControlAsync(AdsState.Config, 0, cancel);
            }
        }
        public static void Stop(AmsNetId target)
        {
            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));
                client.WriteControl(new StateInfo(AdsState.Stop, 0));
            }
        }
        public static async Task StopAsync(AmsNetId target, CancellationToken cancel)
        {
            using (AdsClient client = new AdsClient())
            {
                await client.ConnectAsync(new AmsAddress(target, AmsPort.SystemService), cancel);
                await client.WriteControlAsync(AdsState.Stop, 0, cancel);
            }
        }
        public static void Restart(AmsNetId target)
        {
            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));
                client.WriteControl(new StateInfo(AdsState.Reset, 0));
            }
        }
        public static async Task RestartAsync(AmsNetId target, CancellationToken cancel)
        {
            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));
                await client.WriteControlAsync(AdsState.Reset, 0, cancel);
            }
        }
        public static async Task<DateTime> GetTimeAsync(AmsNetId target, CancellationToken cancel)
        {
            var buffer = new Memory<byte>(new byte[16]);
            using (AdsClient client = new AdsClient())
            {
                await client.ConnectAsync(new AmsAddress(target, AmsPort.SystemService), cancel);
                var result = await client.ReadAsync(400, 1, buffer, cancel);
                result.ThrowOnError();
            }

            return DateTime.FromBinary(BitConverter.ToInt64(buffer.ToArray(), 0));
        }
        public static async Task SetLocalTimeAsync(AmsNetId target, DateTime time, CancellationToken cancel)
        {
            var buffer = new ReadOnlyMemory<byte>(BitConverter.GetBytes(time.Ticks));

            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));
                var result = await client.WriteAsync(400, 1, buffer, cancel);
                result.ThrowOnError();
            }
        }
        public static int GetCpuUsage(AmsNetId target)
        {
            var buffer = new Memory<byte>(new byte[4]);

            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.R0_Realtime));
                client.Read(0x01, 0x06, buffer);
            }

            return BitConverter.ToInt32(buffer.ToArray(), 0);
        }
        public static async Task<int> GetCpuUsageAsync(AmsNetId target, CancellationToken cancel)
        {
            var buffer = new Memory<byte>(new byte[4]);

            using (AdsClient client = new AdsClient())
            {
                await client.ConnectAsync(new AmsAddress(target, AmsPort.R0_Realtime), cancel);
                var result = await client.ReadAsync(0x01, 0x06, buffer, cancel);
                result.ThrowOnError();
            }

            return BitConverter.ToInt32(buffer.ToArray(), 0);
        }
        public static int GetSystemLatencyActual(AmsNetId target)
        {
            var buffer = new Memory<byte>(new byte[8]);

            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.R0_Realtime));
                client.Read(0x01, 0x02, buffer);
            }

            return BitConverter.ToInt32(buffer.ToArray(), 0);
        }
        public static async Task<int> GetSystemLatencyActualAsync(AmsNetId target, CancellationToken cancel)
        {
            var buffer = new Memory<byte>(new byte[8]);

            using (AdsClient client = new AdsClient())
            {
                await client.ConnectAsync(new AmsAddress(target, AmsPort.R0_Realtime), cancel);
                var result = await client.ReadAsync(0x01, 0x02, buffer, cancel);
                result.ThrowOnError();
            }

            return BitConverter.ToInt32(buffer.ToArray(), 0);
        }
        public static int GetSystemLatencyMaximum(AmsNetId target)
        {
            var buffer = new Memory<byte>(new byte[8]);

            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.R0_Realtime));
                client.Read(0x01, 0x02, buffer);
            }

            return BitConverter.ToInt32(buffer.ToArray(), 4);
        }
        public static async Task<int> GetSystemLatencyMaximumAsync(AmsNetId target, CancellationToken cancel)
        {
            var buffer = new Memory<byte>(new byte[8]);

            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.R0_Realtime));
                var result = await client.ReadAsync(0x01, 0x02, buffer, cancel);
                result.ThrowOnError();
            }

            return BitConverter.ToInt32(buffer.ToArray(), 4);
        }

        public static bool IsTargetReachable (AmsNetId target)
        {
            using (AdsClient client = new AdsClient())
            {
                try
                {
                    client.Connect(new AmsAddress(target, AmsPort.R0_Realtime));
                    client.ReadState();

                    return true;
                }
                catch (Exception ex)
                {
                    
                }              
            }

            return false;
        }

        public static async Task<DeviceInfo> GetDeviceInfoAsync (AmsNetId target, CancellationToken cancel)
        {
            var buffer = new Memory<byte>(new byte[2048]);

            DeviceInfo device = new DeviceInfo();

            using (AdsClient client = new AdsClient())
            {
                client.Connect(new AmsAddress(target, AmsPort.SystemService));
                var result = await client.ReadAsync(700, 1, buffer, cancel);
                result.ThrowOnError();

                String data = System.Text.Encoding.ASCII.GetString(buffer.ToArray());

                device.TargetType = GetValueFromTag("<TargetType>", data);
                device.HardwareModel = GetValueFromTag("<Model>", data);
                device.HardwareSerialNo = GetValueFromTag("<SerialNo>", data);
                device.HardwareVersion = GetValueFromTag("<CPUArchitecture>", data);
                device.HardwareDate = GetValueFromTag("<Date>", data);
                device.HardwareCPU = GetValueFromTag("<CPUVersion>", data);
                
                device.ImageDevice = GetValueFromTag("<ImageDevice>", data);
                device.ImageVersion = GetValueFromTag("<ImageVersion>", data);
                device.ImageLevel = GetValueFromTag("<ImageLevel>", data);
                device.ImageOsName = GetValueFromTag("<OsName>", data);
                device.ImageOsVersion = GetValueFromTag("<OsVersion>", data);
                
                var major = GetValueFromTag("<Version>", data);
                var minor = GetValueFromTag("<Revision>", data);
                var build = GetValueFromTag("<Build>", data);
                device.TwinCATVersion = Version.Parse(major + "." + minor + "." + build);
            }

            return device;
        }

        private static string GetValueFromTag(string tag, string value)
        {
            try
            {
                int idxstart = value.IndexOf(tag) + tag.Length;
                int endidx = value.IndexOf("</", idxstart);
                String res = value.Substring(idxstart, endidx - idxstart);
                return res;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
    }
}
