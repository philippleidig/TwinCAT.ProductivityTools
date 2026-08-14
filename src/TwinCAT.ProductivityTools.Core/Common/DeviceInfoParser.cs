using System;
using System.Text;

namespace TwinCAT.ProductivityTools
{
	/// <summary>
	/// Turns the device information payload of the TwinCAT system service into a
	/// <see cref="DeviceInfo"/>.
	/// </summary>
	/// <remarks>
	/// The payload looks like XML but is not well formed, so it is scanned for tags instead of
	/// being parsed. A target may omit any of the tags, which is why every value is optional and a
	/// missing one yields an empty string rather than an exception.
	/// </remarks>
	public static class DeviceInfoParser
	{
		/// <summary>
		/// Parses the payload returned by index group 700, index offset 1 of the system service.
		/// </summary>
		/// <param name="payload">Raw payload bytes.</param>
		/// <param name="length">Number of valid bytes in <paramref name="payload"/>.</param>
		public static DeviceInfo Parse(byte[] payload, int length)
		{
			if (payload == null)
			{
				throw new ArgumentNullException(nameof(payload));
			}

			if (length < 0 || length > payload.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(length));
			}

			// The service does not report how much of the buffer it filled, so everything behind
			// the first terminator is padding and would end up inside the last tag value.
			int end = Array.IndexOf(payload, (byte)0, 0, length);

			return Parse(Encoding.ASCII.GetString(payload, 0, end < 0 ? length : end));
		}

		/// <summary>
		/// Parses the already decoded payload.
		/// </summary>
		public static DeviceInfo Parse(string payload)
		{
			var device = new DeviceInfo
			{
				TargetType = ReadTag(payload, "TargetType"),
				HardwareModel = ReadTag(payload, "Model"),
				HardwareSerialNo = ReadTag(payload, "SerialNo"),
				HardwareVersion = ReadTag(payload, "CPUArchitecture"),
				HardwareDate = ReadTag(payload, "Date"),
				HardwareCPU = ReadTag(payload, "CPUVersion"),
				ImageDevice = ReadTag(payload, "ImageDevice"),
				ImageVersion = ReadTag(payload, "ImageVersion"),
				ImageLevel = ReadTag(payload, "ImageLevel"),
				ImageOsName = ReadTag(payload, "OsName"),
				ImageOsVersion = ReadTag(payload, "OsVersion"),
				TwinCATVersion = ReadVersion(payload)
			};

			return device;
		}

		/// <summary>
		/// Returns the content of a tag, or an empty string when the tag is absent or malformed.
		/// </summary>
		public static string ReadTag(string payload, string name)
		{
			if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(name))
			{
				return string.Empty;
			}

			string open = "<" + name + ">";

			int start = payload.IndexOf(open, StringComparison.Ordinal);

			if (start < 0)
			{
				return string.Empty;
			}

			start += open.Length;

			int end = payload.IndexOf("</", start, StringComparison.Ordinal);

			if (end < 0)
			{
				return string.Empty;
			}

			return payload.Substring(start, end - start);
		}

		/// <summary>
		/// Assembles the TwinCAT version from its three separate tags.
		/// </summary>
		/// <remarks>
		/// A target that reports none of them, or reports something that is not a number, yields
		/// version 0.0.0 rather than failing the whole request. The version is a detail of the
		/// dialog, not its purpose.
		/// </remarks>
		private static Version ReadVersion(string payload)
		{
			int major = ReadNumber(payload, "Version");
			int minor = ReadNumber(payload, "Revision");
			int build = ReadNumber(payload, "Build");

			return new Version(major, minor, build);
		}

		private static int ReadNumber(string payload, string name)
		{
			return int.TryParse(ReadTag(payload, name), out int value) && value >= 0 ? value : 0;
		}
	}
}
