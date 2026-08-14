using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using FluentAssertions;
using Xunit;

namespace TwinCAT.ProductivityTools.Core.Tests.Common
{
	public class NetworkAdapterParserTests
	{
		/// <summary>
		/// Builds one adapter record the way the TwinCAT system service reports it.
		/// </summary>
		private static byte[] Record(
			string instanceId = "\\DEVICE\\{2C0DE22F-7E3C-4C1B-9A31-2C8B0F8A2F11}",
			string description = "Intel(R) Ethernet Connection",
			string ip = "192.168.1.10",
			string subnet = "255.255.255.0",
			string gateway = "192.168.1.1",
			short type = NetworkAdapterParser.EthernetInterfaceType,
			bool dhcp = false,
			byte[] mac = null
		)
		{
			byte[] record = new byte[NetworkAdapterParser.RecordSize];

			Write(record, 8, instanceId);
			Write(record, 268, description);

			(mac ?? new byte[] { 0x00, 0x01, 0x05, 0x0A, 0x0B, 0x0C }).CopyTo(record, 404);

			BitConverter.GetBytes(type).CopyTo(record, 416);

			record[420] = (byte)(dhcp ? 1 : 0);

			Write(record, 432, ip);
			Write(record, 448, subnet);
			Write(record, 472, gateway);

			return record;
		}

		private static void Write(byte[] record, int offset, string value)
		{
			Encoding.UTF8.GetBytes(value).CopyTo(record, offset);
		}

		private static byte[] Concat(params byte[][] records)
		{
			var buffer = new List<byte>();

			foreach (byte[] record in records)
			{
				buffer.AddRange(record);
			}

			return buffer.ToArray();
		}

		[Fact]
		public void Parse_reads_an_ethernet_adapter()
		{
			byte[] response = Record();

			IReadOnlyList<LocalAreaConnection> adapters = NetworkAdapterParser.Parse(
				response,
				response.Length
			);

			adapters.Should().HaveCount(1);

			LocalAreaConnection adapter = adapters[0];

			adapter.InstanceId.Should().Be(new Guid("2C0DE22F-7E3C-4C1B-9A31-2C8B0F8A2F11"));
			adapter.Description.Should().Be("Intel(R) Ethernet Connection");
			adapter.IpAddress.Should().Be(IPAddress.Parse("192.168.1.10"));
			adapter.SubnetMask.Should().Be(IPAddress.Parse("255.255.255.0"));
			adapter.Gateway.Should().Be(IPAddress.Parse("192.168.1.1"));
			adapter.DHCP.Should().BeFalse();
			adapter.MacAddress.GetAddressBytes().Should().Equal(0x00, 0x01, 0x05, 0x0A, 0x0B, 0x0C);
		}

		[Fact]
		public void Parse_reads_the_dhcp_flag()
		{
			byte[] response = Record(dhcp: true);

			NetworkAdapterParser.Parse(response, response.Length)[0].DHCP.Should().BeTrue();
		}

		[Fact]
		public void Parse_skips_an_adapter_that_is_not_ethernet()
		{
			byte[] response = Record(type: 24);

			NetworkAdapterParser.Parse(response, response.Length).Should().BeEmpty();
		}

		[Fact]
		public void Parse_skips_an_adapter_without_an_instance_id()
		{
			byte[] response = Record(instanceId: string.Empty);

			NetworkAdapterParser.Parse(response, response.Length).Should().BeEmpty();
		}

		[Fact]
		public void Parse_keeps_an_adapter_that_has_no_address_yet()
		{
			// A single unconfigured adapter used to make IPAddress.Parse throw and lost the whole
			// listing, which is exactly the adapter the user wants to install the driver on.
			byte[] response = Record(ip: string.Empty, subnet: string.Empty, gateway: string.Empty);

			IReadOnlyList<LocalAreaConnection> adapters = NetworkAdapterParser.Parse(
				response,
				response.Length
			);

			adapters.Should().HaveCount(1);
			adapters[0].IpAddress.Should().Be(IPAddress.None);
			adapters[0].SubnetMask.Should().Be(IPAddress.None);
			adapters[0].Gateway.Should().Be(IPAddress.None);
		}

		[Fact]
		public void Parse_reads_every_record_of_a_response()
		{
			byte[] response = Concat(
				Record(instanceId: "{11111111-1111-1111-1111-111111111111}"),
				Record(instanceId: "{22222222-2222-2222-2222-222222222222}"),
				Record(instanceId: "{33333333-3333-3333-3333-333333333333}")
			);

			NetworkAdapterParser.Parse(response, response.Length).Should().HaveCount(3);
		}

		[Fact]
		public void Parse_drops_a_truncated_trailing_record()
		{
			byte[] response = Concat(Record(), new byte[100]);

			NetworkAdapterParser.Parse(response, response.Length).Should().HaveCount(1);
		}

		[Fact]
		public void Parse_honours_the_reported_length()
		{
			byte[] response = Concat(Record(), Record());

			NetworkAdapterParser
				.Parse(response, NetworkAdapterParser.RecordSize)
				.Should()
				.HaveCount(1);
		}

		[Theory]
		[InlineData("\\DEVICE\\{2C0DE22F-7E3C-4C1B-9A31-2C8B0F8A2F11}", true)]
		[InlineData("{2C0DE22F-7E3C-4C1B-9A31-2C8B0F8A2F11}", true)]
		[InlineData("2C0DE22F-7E3C-4C1B-9A31-2C8B0F8A2F11", true)]
		[InlineData("\\DEVICE\\{not-a-guid}", false)]
		[InlineData("\\DEVICE\\", false)]
		[InlineData("", false)]
		[InlineData(null, false)]
		public void TryReadGuid_reports_whether_a_value_holds_a_guid(string value, bool expected)
		{
			Guid instanceId;

			NetworkAdapterParser.TryReadGuid(value, out instanceId).Should().Be(expected);
		}

		[Fact]
		public void ReadString_stops_at_the_terminator()
		{
			byte[] record = new byte[32];

			Encoding.UTF8.GetBytes("name").CopyTo(record, 0);

			NetworkAdapterParser.ReadString(record, 0, record.Length).Should().Be("name");
		}

		[Fact]
		public void Parse_rejects_a_length_beyond_the_buffer()
		{
			Action parse = () => NetworkAdapterParser.Parse(new byte[4], 5);

			parse.Should().Throw<ArgumentOutOfRangeException>();
		}
	}
}
