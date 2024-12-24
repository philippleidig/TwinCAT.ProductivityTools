﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using TwinCAT.Ads;

namespace TwinCAT.ProductivityTools
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
			System
				.Text.Encoding.ASCII.GetBytes(args)
				.CopyTo(Data, 12 + path.Length + 1 + dir.Length + 1);

			ReadOnlyMemory<byte> buffer = new ReadOnlyMemory<byte>(Data);

			using (AdsClient client = new AdsClient())
			{
				client.Connect(new AmsAddress(target, AmsPort.SystemService));
				client.Write(500, 0, buffer);
				client.Dispose();
			}
		}

		public static async Task StartProcessAsync(
			AmsNetId target,
			string path,
			string directory,
			string args,
			CancellationToken cancel
		)
		{
			byte[] Data = new byte[777];

			BitConverter.GetBytes(path.Length).CopyTo(Data, 0);
			BitConverter.GetBytes(directory.Length).CopyTo(Data, 4);
			BitConverter.GetBytes(args.Length).CopyTo(Data, 8);

			System.Text.Encoding.ASCII.GetBytes(path).CopyTo(Data, 12);
			System.Text.Encoding.ASCII.GetBytes(directory).CopyTo(Data, 12 + path.Length + 1);
			System
				.Text.Encoding.ASCII.GetBytes(args)
				.CopyTo(Data, 12 + path.Length + 1 + directory.Length + 1);

			ReadOnlyMemory<byte> buffer = new ReadOnlyMemory<byte>(Data);

			using (AdsClient client = new AdsClient())
			{
				client.Connect(new AmsAddress(target, AmsPort.SystemService));
				var res = await client.WriteAsync(500, 0, buffer, cancel);
				res.ThrowOnError();
			}
		}

		public static void Shutdown(AmsNetId target)
		{
			using (AdsClient client = new AdsClient())
			{
				client.Connect(new AmsAddress(target, AmsPort.SystemService));
				client.WriteControl(new StateInfo(AdsState.Shutdown, 0));
			}
		}

		public static async Task ShutdownAsync(AmsNetId target, CancellationToken cancel)
		{
			using (AdsClient client = new AdsClient())
			{
				client.Connect(new AmsAddress(target, AmsPort.SystemService));
				var res = await client.WriteControlAsync(AdsState.Shutdown, 0, cancel);
				res.ThrowOnError();
			}
		}

		public static void Reboot(AmsNetId target)
		{
			using (AdsClient client = new AdsClient())
			{
				client.Connect(new AmsAddress(target, AmsPort.SystemService));
				client.WriteControl(new StateInfo(AdsState.Shutdown, 1));
			}
		}

		public static async Task RebootAsync(AmsNetId target, CancellationToken cancel)
		{
			using (AdsClient client = new AdsClient())
			{
				client.Connect(new AmsAddress(target, AmsPort.SystemService));
				await client.WriteControlAsync(AdsState.Shutdown, 1, cancel);
			}
		}

		public static bool IsTargetReachable(AmsNetId target)
		{
			using (AdsClient client = new AdsClient())
			{
				try
				{
					client.Connect(new AmsAddress(target, AmsPort.R0_Realtime));
					client.ReadState();

					return true;
				}
				catch (Exception ex) { }
			}

			return false;
		}

		public static async Task<DeviceInfo> GetDeviceInfoAsync(
			AmsNetId target,
			CancellationToken cancel
		)
		{
			var buffer = new byte[2048];

			DeviceInfo device = new DeviceInfo();

			using (AdsClient client = new AdsClient())
			{
				client.Connect(new AmsAddress(target, AmsPort.SystemService));

				var result = client.Read(700, 1, buffer.AsMemory());

				//  result.ThrowOnError();

				String data = System.Text.Encoding.ASCII.GetString(buffer);

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
