using System;

namespace TwinCAT.ProductivityTools.Remote
{
	/// <summary>
	/// Checks the address of a target before it is handed to a command line or to a URL.
	/// </summary>
	/// <remarks>
	/// The address comes from a route file the extension does not own, so it is input, not a
	/// constant. Without this check a route whose address reads <c>10.0.0.5 -oProxyCommand=calc</c>
	/// would end up as two arguments of the SSH client.
	/// </remarks>
	public static class TargetAddress
	{
		/// <summary>
		/// Reports whether the value is usable as a host: an IPv4 or IPv6 literal, or a name.
		/// </summary>
		public static bool IsUsable(string address)
		{
			return Normalize(address) != null;
		}

		/// <summary>
		/// Returns the trimmed address, or <c>null</c> when it is not usable as a host.
		/// </summary>
		public static string Normalize(string address)
		{
			if (string.IsNullOrWhiteSpace(address))
			{
				return null;
			}

			string trimmed = address.Trim();

			return Uri.CheckHostName(trimmed) == UriHostNameType.Unknown ? null : trimmed;
		}

		/// <summary>
		/// Returns the address as it belongs into a URL, or <c>null</c> when it is not usable.
		/// </summary>
		/// <remarks>
		/// An IPv6 literal has to be bracketed, otherwise its colons are read as the port
		/// separator.
		/// </remarks>
		public static string ForUrl(string address)
		{
			string host = Normalize(address);

			if (host == null)
			{
				return null;
			}

			return Uri.CheckHostName(host) == UriHostNameType.IPv6 ? "[" + host + "]" : host;
		}
	}
}
