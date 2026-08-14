using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace TwinCAT.ProductivityTools
{
	public enum RegistryValueType
	{
		NONE = 0, /* No value TYPE */
		SZ, /* Unicode nul terminated STRING */
		EXPAND_SZ, /* Unicode nul terminated STRING (with environment variable references) */
		BINARY, /* Free form binary */
		DWORD, /* 32-bit number and REG_DWORD_LITTLE_ENDIAN (same as REG_DWORD) */
		DWORD_BIG_ENDIAN, /* 32-bit number */
		LINK, /* Symbolic Link (unicode) */
		MULTI_SZ, /* Multiple Unicode strings */
		RESOURCE_LIST, /* Resource list in the resource map */
		FULL_RESOURCE_DESCRIPTOR, /* Resource list in the hardware description */
		RESOURCE_REQUIREMENTS_LIST, /* */
		QWORD /* 64-bit number and REG_QWORD_LITTLE_ENDIAN (same as REG_QWORD) */
	}

	public static class AdsRegistry
	{
		/// <summary>
		/// Reads a registry value from a remote target.
		/// </summary>
		/// <remarks>
		/// The request is the sub key and the value name, each null terminated. The response is
		/// the raw value, which the caller expects to be a string.
		/// </remarks>
		public static async Task<string> QueryValueAsync(
			AmsNetId target,
			string subKey,
			string valueName,
			CancellationToken cancel = default(CancellationToken)
		)
		{
			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			if (string.IsNullOrEmpty(subKey))
			{
				throw new ArgumentException("A sub key is required.", nameof(subKey));
			}

			using (var client = new AdsClient())
			{
				client.Connect(new AmsAddress(target, AmsPort.SystemService));

				var request = new List<byte>();

				request.AddRange(System.Text.Encoding.UTF8.GetBytes(subKey));
				request.Add(0); // End delimiter
				request.AddRange(System.Text.Encoding.UTF8.GetBytes(valueName ?? string.Empty));
				request.Add(0);

				byte[] response = new byte[255];

				ResultReadWriteBytes result = await client.ReadWriteAsync(
					200,
					0,
					response.Length,
					new ReadOnlyMemory<byte>(request.ToArray()),
					cancel
				);

				result.ThrowOnError();

				result.Data.CopyTo(response);

				// The target terminates the value, and everything behind it is whatever the
				// buffer happened to contain.
				int end = Array.IndexOf(response, (byte)0, 0, result.ReadBytes);

				return System.Text.Encoding.UTF8.GetString(
					response,
					0,
					end < 0 ? result.ReadBytes : end
				);
			}
		}

		/// <summary>
		/// Writes a registry value on a remote target.
		/// </summary>
		public static async Task SetValueAsync(
			AmsNetId target,
			string subKey,
			string valueName,
			RegistryValueType type,
			IEnumerable<byte> data,
			CancellationToken cancel = default(CancellationToken)
		)
		{
			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			if (string.IsNullOrEmpty(subKey))
			{
				throw new ArgumentException("A sub key is required.", nameof(subKey));
			}

			using (var client = new AdsClient())
			{
				client.Connect(new AmsAddress(target, AmsPort.SystemService));

				var request = new List<byte>();

				request.AddRange(System.Text.Encoding.UTF8.GetBytes(subKey));
				request.Add(0); // End delimiter
				request.AddRange(System.Text.Encoding.UTF8.GetBytes(valueName ?? string.Empty));
				request.Add(0);

				if (data != null)
				{
					request.AddRange(data);
				}

				ResultWrite result = await client.WriteAsync(
					200,
					0,
					new ReadOnlyMemory<byte>(request.ToArray()),
					cancel
				);

				result.ThrowOnError();
			}
		}
	}
}
