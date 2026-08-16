using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace TwinCAT.ProductivityTools
{
	/// <summary>
	/// Controls a remote TwinCAT target over the system service port.
	/// </summary>
	public static class RemoteControl
	{
		/// <summary>
		/// Size of the request the system service expects for "start process". The layout is three
		/// 32 bit lengths followed by three null terminated ASCII strings.
		/// </summary>
		public const int StartProcessRequestSize = 777;

		private const int StartProcessHeaderSize = 12;

		/// <summary>
		/// Size of the buffer the device information is read into.
		/// </summary>
		private const int DeviceInfoBufferSize = 2048;

		/// <summary>
		/// Builds the "start process" request of the system service.
		/// </summary>
		/// <exception cref="ArgumentException">
		/// The combined length of the arguments does not fit into the request the system service
		/// accepts. Without this check the request would be built past the end of the buffer and
		/// the caller would see an <see cref="IndexOutOfRangeException"/> from deep inside.
		/// </exception>
		public static byte[] BuildStartProcessRequest(string path, string directory, string args)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentException("A path is required.", nameof(path));
			}

			directory = directory ?? string.Empty;
			args = args ?? string.Empty;

			// Every string is stored null terminated, so each one costs its length plus one byte.
			int required =
				StartProcessHeaderSize + path.Length + directory.Length + args.Length + 3;

			if (required > StartProcessRequestSize)
			{
				throw new ArgumentException(
					$"The command is {required - StartProcessRequestSize} character(s) too long "
						+ $"for the {StartProcessRequestSize} byte request of the system service."
				);
			}

			byte[] request = new byte[StartProcessRequestSize];

			BitConverter.GetBytes(path.Length).CopyTo(request, 0);
			BitConverter.GetBytes(directory.Length).CopyTo(request, 4);
			BitConverter.GetBytes(args.Length).CopyTo(request, 8);

			int offset = StartProcessHeaderSize;

			offset += Write(request, offset, path);
			offset += Write(request, offset, directory);

			Write(request, offset, args);

			return request;
		}

		/// <summary>
		/// Starts a process on the target.
		/// </summary>
		public static async Task StartProcessAsync(
			AmsNetId target,
			string path,
			string directory,
			string args,
			CancellationToken cancel
		)
		{
			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			byte[] request = BuildStartProcessRequest(path, directory, args);

			using (var client = new AdsClient())
			{
				client.Connect(new AmsAddress(target, AmsPort.SystemService));

				ResultWrite result = await client.WriteAsync(
					500,
					0,
					new ReadOnlyMemory<byte>(request),
					cancel
				);

				result.ThrowOnError();
			}
		}

		/// <summary>
		/// Shuts the target down.
		/// </summary>
		public static async Task ShutdownAsync(AmsNetId target, CancellationToken cancel)
		{
			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			using (var client = new AdsClient())
			{
				client.Connect(new AmsAddress(target, AmsPort.SystemService));

				ResultAds result = await client.WriteControlAsync(AdsState.Shutdown, 0, cancel);

				result.ThrowOnError();
			}
		}

		/// <summary>
		/// Reboots the target.
		/// </summary>
		/// <remarks>
		/// A reboot is the shutdown command with parameter 1. That parameter is the only
		/// difference to <see cref="ShutdownAsync"/>.
		/// </remarks>
		public static async Task RebootAsync(AmsNetId target, CancellationToken cancel)
		{
			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			using (var client = new AdsClient())
			{
				client.Connect(new AmsAddress(target, AmsPort.SystemService));

				ResultAds result = await client.WriteControlAsync(AdsState.Shutdown, 1, cancel);

				result.ThrowOnError();
			}
		}

		/// <summary>
		/// Reports whether the real time system of the target answers.
		/// </summary>
		public static bool IsTargetReachable(AmsNetId target)
		{
			if (target == null)
			{
				return false;
			}

			try
			{
				using (var client = new AdsClient())
				{
					client.Connect(new AmsAddress(target, AmsPort.R0_Realtime));
					client.ReadState();

					return true;
				}
			}
			catch (Exception)
			{
				// Every failure means the same thing to the caller: the target cannot be reached.
				// A route can be missing, the target can be off, ADS can be blocked by a firewall.
				return false;
			}
		}

		/// <summary>
		/// Reads the hardware, image and TwinCAT version information of the target.
		/// </summary>
		public static async Task<DeviceInfo> GetDeviceInfoAsync(
			AmsNetId target,
			CancellationToken cancel
		)
		{
			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			byte[] buffer = new byte[DeviceInfoBufferSize];

			using (var client = new AdsClient())
			{
				client.Connect(new AmsAddress(target, AmsPort.SystemService));

				ResultReadBytes result = await client.ReadAsync(700, 1, buffer.Length, cancel);

				result.ThrowOnError();

				result.Data.CopyTo(buffer);

				return DeviceInfoParser.Parse(buffer, result.ReadBytes);
			}
		}

		/// <summary>
		/// Writes a null terminated ASCII string and returns how many bytes it consumed.
		/// </summary>
		private static int Write(byte[] destination, int offset, string value)
		{
			int written = Encoding.ASCII.GetBytes(value, 0, value.Length, destination, offset);

			// The terminator is already zero because the buffer starts out cleared. Only the
			// offset has to move past it.
			return written + 1;
		}
	}
}
