using System;
using System.Linq;

namespace TwinCAT.ProductivityTools.Routing
{
	/// <summary>
	/// Finds the address that belongs to an AmsNetID.
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
		/// Returns the address of the route that belongs to the AmsNetID, falling back to the name
		/// of that route, or <c>null</c> when no route names the target.
		/// </summary>
		/// <remarks>
		/// The route is the only place that knows the address of a target. An AmsNetID looks like
		/// one - it has four leading octets that parse as IPv4 - but for every AmsNetID TwinCAT
		/// generates itself those octets come from the MAC address of an adapter. Deriving an
		/// address from them, as this class used to do, sent remote desktop to a machine that has
		/// nothing to do with the target: <c>5.24.13.37.1.1</c> would become <c>5.24.13.37</c>,
		/// which is public, routable address space belonging to somebody else. A target the user
		/// can reach has a route; a target without one gets an error message that says so.
		/// </remarks>
		public string Resolve(string amsNetId)
		{
			if (string.IsNullOrWhiteSpace(amsNetId))
			{
				return null;
			}

			return FromRoute(amsNetId);
		}

		private string FromRoute(string amsNetId)
		{
			try
			{
				TcConfigRoute route = routeReader
					.ListRoutes()
					?.FirstOrDefault(
						candidate =>
							string.Equals(
								candidate?.NetId,
								amsNetId,
								StringComparison.OrdinalIgnoreCase
							)
					);

				// A route without an address still names the target, and that name is what the
				// user typed into the route dialog - normally a resolvable host name.
				return route == null ? null : FirstUsable(route.Address, route.Name);
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static string FirstUsable(params string[] candidates)
		{
			return candidates
				.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate))
				?.Trim();
		}
	}
}
