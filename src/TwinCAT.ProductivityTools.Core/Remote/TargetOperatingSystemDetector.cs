using System;

namespace TwinCAT.ProductivityTools.Remote
{
	/// <summary>
	/// Derives the operating system family of a target from the information the target reports.
	/// </summary>
	/// <remarks>
	/// The system service answers with free text that differs between images, so the fields are
	/// scanned for markers instead of being compared. Anything that carries no known marker stays
	/// <see cref="TargetOperatingSystem.Unknown"/> - guessing Windows would open a remote desktop
	/// session on a TwinCAT/BSD machine that has no server to answer it.
	/// </remarks>
	public static class TargetOperatingSystemDetector
	{
		// Ordered, and the order carries meaning: "Windows CE" contains "Windows", and a
		// TwinCAT/BSD image may well name Linux somewhere. The more specific marker wins.
		private static readonly string[] BsdMarkers =
		{
			"twincat/bsd",
			"twincat bsd",
			"tcbsd",
			"freebsd",
			"bsd",
		};

		private static readonly string[] LinuxMarkers =
		{
			"twincat/linux",
			"tclinux",
			"debian",
			"ubuntu",
			"yocto",
			"linux",
		};

		private static readonly string[] WindowsCeMarkers =
		{
			"windows ce",
			"windows embedded compact",
			"wince",
			"win ce",
		};

		private static readonly string[] WindowsMarkers = { "windows", "microsoft win" };

		/// <summary>
		/// Classifies a device information, or reports <see cref="TargetOperatingSystem.Unknown"/>
		/// when it is missing or says nothing about the operating system.
		/// </summary>
		public static TargetOperatingSystem Detect(DeviceInfo device)
		{
			return device == null
				? TargetOperatingSystem.Unknown
				: Detect(device.ImageOsName, device.TargetType, device.ImageDevice);
		}

		/// <summary>
		/// Classifies the three fields that can carry the operating system, most authoritative
		/// first.
		/// </summary>
		/// <remarks>
		/// The fields are examined one after the other rather than as one text, so that a hardware
		/// name never overrules the operating system name of the image.
		/// </remarks>
		public static TargetOperatingSystem Detect(
			string osName,
			string targetType,
			string imageDevice
		)
		{
			string[] fields = { osName, targetType, imageDevice };

			foreach (string field in fields)
			{
				TargetOperatingSystem found = DetectIn(field);

				if (found != TargetOperatingSystem.Unknown)
				{
					return found;
				}
			}

			return TargetOperatingSystem.Unknown;
		}

		/// <summary>
		/// Classifies a single piece of text.
		/// </summary>
		public static TargetOperatingSystem DetectIn(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return TargetOperatingSystem.Unknown;
			}

			if (Contains(text, BsdMarkers))
			{
				return TargetOperatingSystem.Bsd;
			}

			if (Contains(text, LinuxMarkers))
			{
				return TargetOperatingSystem.Linux;
			}

			if (Contains(text, WindowsCeMarkers))
			{
				return TargetOperatingSystem.WindowsCe;
			}

			return Contains(text, WindowsMarkers)
				? TargetOperatingSystem.Windows
				: TargetOperatingSystem.Unknown;
		}

		private static bool Contains(string text, string[] markers)
		{
			foreach (string marker in markers)
			{
				if (text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}

			return false;
		}
	}
}
