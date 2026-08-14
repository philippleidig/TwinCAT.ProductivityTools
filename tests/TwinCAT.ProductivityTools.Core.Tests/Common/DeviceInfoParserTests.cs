using System;
using System.Text;
using FluentAssertions;
using Xunit;

namespace TwinCAT.ProductivityTools.Core.Tests.Common
{
	public class DeviceInfoParserTests
	{
		private const string Payload =
			"<Info>"
			+ "<TargetType>CX2042</TargetType>"
			+ "<Model>CX2042-0125</Model>"
			+ "<SerialNo>0012345</SerialNo>"
			+ "<CPUArchitecture>x64</CPUArchitecture>"
			+ "<Date>2023-05-04</Date>"
			+ "<CPUVersion>Intel Core i7</CPUVersion>"
			+ "<ImageDevice>CX20x0</ImageDevice>"
			+ "<ImageVersion>6.03e</ImageVersion>"
			+ "<ImageLevel>HPS</ImageLevel>"
			+ "<OsName>Windows 10 IoT</OsName>"
			+ "<OsVersion>21H2</OsVersion>"
			+ "<Version>3</Version>"
			+ "<Revision>1</Revision>"
			+ "<Build>4026</Build>"
			+ "</Info>";

		[Fact]
		public void Parse_reads_every_field()
		{
			DeviceInfo device = DeviceInfoParser.Parse(Payload);

			device.TargetType.Should().Be("CX2042");
			device.HardwareModel.Should().Be("CX2042-0125");
			device.HardwareSerialNo.Should().Be("0012345");
			device.HardwareVersion.Should().Be("x64");
			device.HardwareDate.Should().Be("2023-05-04");
			device.HardwareCPU.Should().Be("Intel Core i7");
			device.ImageDevice.Should().Be("CX20x0");
			device.ImageVersion.Should().Be("6.03e");
			device.ImageLevel.Should().Be("HPS");
			device.ImageOsName.Should().Be("Windows 10 IoT");
			device.ImageOsVersion.Should().Be("21H2");
			device.TwinCATVersion.Should().Be(new Version(3, 1, 4026));
		}

		[Fact]
		public void Parse_stops_at_the_padding_of_the_response_buffer()
		{
			byte[] buffer = new byte[2048];

			Encoding.ASCII.GetBytes(Payload).CopyTo(buffer, 0);

			DeviceInfo device = DeviceInfoParser.Parse(buffer, buffer.Length);

			device.ImageOsVersion.Should().Be("21H2");
			device.TargetType.Should().Be("CX2042");
		}

		[Fact]
		public void Parse_survives_a_target_that_reports_nothing()
		{
			DeviceInfo device = DeviceInfoParser.Parse(string.Empty);

			device.TargetType.Should().BeEmpty();
			device.TwinCATVersion.Should().Be(new Version(0, 0, 0));
		}

		[Fact]
		public void Parse_survives_a_missing_version()
		{
			DeviceInfo device = DeviceInfoParser.Parse("<TargetType>CX9020</TargetType>");

			device.TargetType.Should().Be("CX9020");
			device.TwinCATVersion.Should().Be(new Version(0, 0, 0));
		}

		[Fact]
		public void Parse_survives_a_version_that_is_not_a_number()
		{
			DeviceInfo device = DeviceInfoParser.Parse(
				"<Version>x</Version><Revision>1</Revision><Build>4026</Build>"
			);

			device.TwinCATVersion.Should().Be(new Version(0, 1, 4026));
		}

		[Theory]
		[InlineData("<A>value</A>", "A", "value")]
		[InlineData("<A>value</A>", "B", "")]
		[InlineData("<A>", "A", "")]
		[InlineData("", "A", "")]
		[InlineData(null, "A", "")]
		public void ReadTag_never_throws_on_a_malformed_payload(
			string payload,
			string name,
			string expected
		)
		{
			DeviceInfoParser.ReadTag(payload, name).Should().Be(expected);
		}

		[Fact]
		public void ReadTag_returns_an_empty_string_when_the_tag_is_absent()
		{
			// The previous implementation added the tag length to the -1 of a failed search and
			// silently returned a value from the middle of the payload.
			DeviceInfoParser.ReadTag("<Model>CX2042</Model>", "SerialNo").Should().BeEmpty();
		}

		[Fact]
		public void Parse_rejects_a_length_beyond_the_buffer()
		{
			Action parse = () => DeviceInfoParser.Parse(new byte[4], 5);

			parse.Should().Throw<ArgumentOutOfRangeException>();
		}

		[Fact]
		public void Parse_rejects_a_missing_buffer()
		{
			Action parse = () => DeviceInfoParser.Parse(null, 0);

			parse.Should().Throw<ArgumentNullException>();
		}
	}
}
