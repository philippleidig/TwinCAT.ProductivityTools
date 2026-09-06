using FluentAssertions;
using NSubstitute;
using TwinCAT.ProductivityTools.Routing;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Routing
{
	public class TargetAddressResolverTests
	{
		private static IRouteReader Reader(params TcConfigRoute[] routes)
		{
			IRouteReader reader = Substitute.For<IRouteReader>();
			reader.ListRoutes().Returns(routes);

			return reader;
		}

		[Fact]
		public void Prefers_the_address_of_a_configured_route()
		{
			IRouteReader reader = Reader(
				new TcConfigRoute { NetId = "5.24.13.37.1.1", Address = "CX-2A0D25" }
			);

			new TargetAddressResolver(reader).Resolve("5.24.13.37.1.1").Should().Be("CX-2A0D25");
		}

		[Fact]
		public void Matches_a_route_regardless_of_casing()
		{
			IRouteReader reader = Reader(
				new TcConfigRoute { NetId = "5.24.13.37.1.1", Address = "10.0.0.5" }
			);

			new TargetAddressResolver(reader).Resolve("5.24.13.37.1.1").Should().Be("10.0.0.5");
		}

		[Fact]
		public void Trims_the_address_of_a_route()
		{
			IRouteReader reader = Reader(
				new TcConfigRoute { NetId = "5.24.13.37.1.1", Address = "  10.0.0.5  " }
			);

			new TargetAddressResolver(reader).Resolve("5.24.13.37.1.1").Should().Be("10.0.0.5");
		}

		[Fact]
		public void Uses_the_name_of_a_route_that_carries_no_address()
		{
			// The name is what the user typed into the route dialog, which is normally the host
			// name of the target and resolves just as well.
			IRouteReader reader = Reader(
				new TcConfigRoute
				{
					NetId = "5.24.13.37.1.1",
					Address = "",
					Name = "CX-2A0D25",
				}
			);

			new TargetAddressResolver(reader).Resolve("5.24.13.37.1.1").Should().Be("CX-2A0D25");
		}

		[Fact]
		public void Never_derives_an_address_from_the_net_id()
		{
			// TwinCAT generates the AmsNetID of a system from the MAC address of an adapter, so
			// 5.24.13.37.1.1 does not describe 5.24.13.37 - an address in public, routable space
			// that belongs to somebody else. Guessing here used to send remote desktop there.
			new TargetAddressResolver(Reader()).Resolve("5.24.13.37.1.1").Should().BeNull();
		}

		[Fact]
		public void Reports_nothing_when_no_route_matches()
		{
			IRouteReader reader = Reader(
				new TcConfigRoute { NetId = "10.0.0.5.1.1", Address = "10.0.0.5" }
			);

			new TargetAddressResolver(reader).Resolve("192.168.10.20.1.1").Should().BeNull();
		}

		[Fact]
		public void Reports_nothing_for_a_route_that_names_nothing_at_all()
		{
			IRouteReader reader = Reader(
				new TcConfigRoute
				{
					NetId = "192.168.1.1.1.1",
					Address = "",
					Name = "",
				}
			);

			new TargetAddressResolver(reader).Resolve("192.168.1.1.1.1").Should().BeNull();
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		[InlineData("1.2.3")]
		public void Returns_nothing_for_an_unusable_input(string netId)
		{
			new TargetAddressResolver(Reader()).Resolve(netId).Should().BeNull();
		}

		[Fact]
		public void Reports_nothing_when_the_route_file_cannot_be_read()
		{
			IRouteReader reader = Substitute.For<IRouteReader>();
			reader.ListRoutes().Returns(_ => throw new System.IO.IOException());

			new TargetAddressResolver(reader).Resolve("192.168.1.1.1.1").Should().BeNull();
		}
	}
}
