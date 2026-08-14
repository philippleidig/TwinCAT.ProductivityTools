using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace TwinCAT.ProductivityTools
{
	/// <summary>
	/// Reads the network adapter list the TwinCAT system service returns.
	/// </summary>
	/// <remarks>
	/// The response is an array of fixed size records without any header. The field offsets below
	/// are the layout of that record. Every field is optional in practice: a target can report an
	/// adapter that has no address yet, and a single such adapter must not fail the whole listing.
	/// </remarks>
	public static class NetworkAdapterParser
	{
		/// <summary>
		/// Size of one adapter record in the response.
		/// </summary>
		public const int RecordSize = 640;

		private const int InstanceIdOffset = 8;
		private const int InstanceIdLength = 260;
		private const int DescriptionOffset = 268;
		private const int DescriptionLength = 131;
		private const int MacAddressOffset = 404;
		private const int MacAddressLength = 6;
		private const int TypeOffset = 416;
		private const int DhcpOffset = 420;
		private const int IpAddressOffset = 432;
		private const int SubnetMaskOffset = 448;
		private const int GatewayOffset = 472;
		private const int AddressLength = 15;

		/// <summary>
		/// Interface type of an Ethernet adapter. Everything else, for example a loopback or a
		/// tunnel, is of no use to the real time driver.
		/// </summary>
		public const short EthernetInterfaceType = 6;

		/// <summary>
		/// Parses every Ethernet adapter out of the response.
		/// </summary>
		/// <param name="response">Raw response of index group 701.</param>
		/// <param name="length">Number of valid bytes in <paramref name="response"/>.</param>
		public static IReadOnlyList<LocalAreaConnection> Parse(byte[] response, int length)
		{
			if (response == null)
			{
				throw new ArgumentNullException(nameof(response));
			}

			if (length < 0 || length > response.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(length));
			}

			var adapters = new List<LocalAreaConnection>();

			// A truncated trailing record is dropped rather than parsed from padding bytes.
			for (int offset = 0; offset + RecordSize <= length; offset += RecordSize)
			{
				LocalAreaConnection adapter = ParseRecord(response, offset);

				if (adapter != null)
				{
					adapters.Add(adapter);
				}
			}

			return adapters;
		}

		/// <summary>
		/// Parses a single record, or returns <c>null</c> when it is not a usable Ethernet adapter.
		/// </summary>
		private static LocalAreaConnection ParseRecord(byte[] response, int offset)
		{
			short type = BitConverter.ToInt16(response, offset + TypeOffset);

			if (type != EthernetInterfaceType)
			{
				return null;
			}

			Guid instanceId;

			if (
				!TryReadGuid(
					ReadString(response, offset + InstanceIdOffset, InstanceIdLength),
					out instanceId
				)
			)
			{
				// Without the instance id the adapter cannot be named or addressed later on.
				return null;
			}

			return new LocalAreaConnection
			{
				InstanceId = instanceId,
				Description = ReadString(response, offset + DescriptionOffset, DescriptionLength),
				MacAddress = ReadMacAddress(response, offset + MacAddressOffset),
				IpAddress = ReadAddress(response, offset + IpAddressOffset),
				SubnetMask = ReadAddress(response, offset + SubnetMaskOffset),
				Gateway = ReadAddress(response, offset + GatewayOffset),
				DHCP = response[offset + DhcpOffset] != 0
			};
		}

		/// <summary>
		/// Reads a null terminated ASCII string out of a fixed size field.
		/// </summary>
		public static string ReadString(byte[] response, int offset, int length)
		{
			int end = Array.IndexOf(response, (byte)0, offset, length);

			int count = end < 0 ? length : end - offset;

			return Encoding.UTF8.GetString(response, offset, count).Trim();
		}

		/// <summary>
		/// Extracts the GUID out of a value such as <c>\DEVICE\{0C6...}</c>.
		/// </summary>
		public static bool TryReadGuid(string value, out Guid instanceId)
		{
			instanceId = Guid.Empty;

			if (string.IsNullOrEmpty(value))
			{
				return false;
			}

			int start = value.IndexOf('{');
			int end = value.IndexOf('}', start + 1);

			if (start < 0 || end < 0)
			{
				return Guid.TryParse(value, out instanceId);
			}

			return Guid.TryParse(value.Substring(start, end - start + 1), out instanceId);
		}

		private static PhysicalAddress ReadMacAddress(byte[] response, int offset)
		{
			var address = new byte[MacAddressLength];

			Array.Copy(response, offset, address, 0, MacAddressLength);

			return new PhysicalAddress(address);
		}

		/// <summary>
		/// Reads an IPv4 address, returning <see cref="IPAddress.None"/> for an adapter that has
		/// no address configured.
		/// </summary>
		private static IPAddress ReadAddress(byte[] response, int offset)
		{
			string value = ReadString(response, offset, AddressLength);

			IPAddress address;

			return IPAddress.TryParse(value, out address) ? address : IPAddress.None;
		}
	}
}
