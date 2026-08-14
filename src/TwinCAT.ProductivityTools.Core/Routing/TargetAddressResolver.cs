using System;
using System.Linq;
using System.Net;

namespace TwinCAT.ProductivityTools.Routing
{
	/// <summary>
	/// Finds the IP address that belongs to an AmsNetID.
	/// </summary>
	public sealed class TargetAddressResolver
	{
		private readonly IRouteReader routeReader;

		public TargetAddressResolver()
			: this(new StaticRoutesReader()) { }

		public TargetAddressResolver(IRouteReader routeReader)
		{
			this.routeReader = routeReader ?? throw new ArgumentNullException(nameof(routeReader));
		}

		/// <summary>
		/// Prefers the address of a configured route, because a route may point at a host name or
		/// at an address that has nothing to do with the AmsNetID. When no route matches, the
		/// first four octets are used - deriving the address that way is the convention TwinCAT
		/// itself follows when it generates an AmsNetID for a network adapter.
		/// </summary>
		/// <returns>The address, or <c>null</c> when none could be determined.</returns>
		public string Resolve(string amsNetId)
		{
			if (string.IsNullOrWhiteSpace(amsNetId))
			{
				return null;
			}

			string fromRoute = FromRoute(amsNetId);

			return string.IsNullOrWhiteSpace(fromRoute) ? FromNetId(amsNetId) : fromRoute;
		}

		private string FromRoute(string amsNetId)
		{
			try
			{
				return routeReader
					.ListRoutes()
					?.FirstOrDefault(
						route =>
							string.Equals(
								route?.NetId,
								amsNetId,
								StringComparison.OrdinalIgnoreCase
							)
					)
					?.Address;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// The first four octets of an AmsNetID form an IPv4 address for every AmsNetID TwinCAT
		/// derives from an adapter. Only a syntactically valid address is returned.
		/// </summary>
		public static string FromNetId(string amsNetId)
		{
			string[] parts = amsNetId?.Split('.');

			if (parts == null || parts.Length != 6)
			{
				return null;
			}

			string candidate = string.Join(".", parts.Take(4));

			return
				IPAddress.TryParse(candidate, out IPAddress address)
				&& address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
				? candidate
				: null;
		}
	}
}
