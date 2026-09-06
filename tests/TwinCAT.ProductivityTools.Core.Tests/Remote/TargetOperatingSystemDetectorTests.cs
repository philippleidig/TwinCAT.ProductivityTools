using FluentAssertions;
using TwinCAT.ProductivityTools.Remote;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Remote
{
	/// <summary>
	/// The classification decides whether a target is offered a remote desktop or an SSH session,
	/// so a wrong answer is worse than no answer - everything unrecognised has to stay unknown.
	/// </summary>
	public class TargetOperatingSystemDetectorTests
	{
		[Theory]
		[InlineData("Windows 10 IoT")]
		[InlineData("WINDOWS 10 IOT ENTERPRISE")]
		[InlineData("Windows 7")]
		[InlineData("Windows Embedded Standard")]
		public void Recognises_a_windows_image(string osName)
		{
			Detect(osName).Should().Be(TargetOperatingSystem.Windows);
		}

		[Theory]
		[InlineData("Windows CE")]
		[InlineData("Windows Embedded Compact 7")]
		[InlineData("WinCE 6.0")]
		public void Keeps_windows_ce_apart_from_windows(string osName)
		{
			// "Windows CE" contains "Windows", so the order of the markers is what makes this
			// work. A CE image serves neither remote desktop nor SSH.
			Detect(osName).Should().Be(TargetOperatingSystem.WindowsCe);
		}

		[Theory]
		[InlineData("TwinCAT/BSD")]
		[InlineData("FreeBSD 13.2")]
		[InlineData("tcbsd")]
		public void Recognises_a_bsd_image(string osName)
		{
			Detect(osName).Should().Be(TargetOperatingSystem.Bsd);
		}

		[Theory]
		[InlineData("TwinCAT/Linux")]
		[InlineData("Debian GNU/Linux 12")]
		[InlineData("Linux 6.1.0")]
		public void Recognises_a_linux_image(string osName)
		{
			Detect(osName).Should().Be(TargetOperatingSystem.Linux);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		[InlineData("Plan 9")]
		[InlineData("?")]
		public void Reports_an_unrecognised_name_as_unknown(string osName)
		{
			Detect(osName).Should().Be(TargetOperatingSystem.Unknown);
		}

		[Fact]
		public void Reports_a_device_that_says_nothing_as_unknown()
		{
			TargetOperatingSystemDetector
				.Detect(new DeviceInfo())
				.Should()
				.Be(TargetOperatingSystem.Unknown);
		}

		[Fact]
		public void Reports_a_missing_device_as_unknown()
		{
			TargetOperatingSystemDetector
				.Detect(null)
				.Should()
				.Be(TargetOperatingSystem.Unknown);
		}

		[Fact]
		public void Falls_back_to_the_target_type_when_the_image_names_no_operating_system()
		{
			DeviceInfo device = new DeviceInfo { ImageOsName = "", TargetType = "TCBSD" };

			TargetOperatingSystemDetector.Detect(device).Should().Be(TargetOperatingSystem.Bsd);
		}

		[Fact]
		public void Falls_back_to_the_image_device_last()
		{
			DeviceInfo device = new DeviceInfo { ImageDevice = "TCBSD_x64" };

			TargetOperatingSystemDetector.Detect(device).Should().Be(TargetOperatingSystem.Bsd);
		}

		[Fact]
		public void Prefers_the_operating_system_name_over_the_hardware_fields()
		{
			// The fields are examined one after the other rather than as one text, so a hardware
			// name cannot overrule the image.
			DeviceInfo device = new DeviceInfo
			{
				ImageOsName = "Windows 10 IoT",
				ImageDevice = "TCBSD_x64",
			};

			TargetOperatingSystemDetector.Detect(device).Should().Be(TargetOperatingSystem.Windows);
		}

		private static TargetOperatingSystem Detect(string osName)
		{
			return TargetOperatingSystemDetector.Detect(new DeviceInfo { ImageOsName = osName });
		}
	}
}
