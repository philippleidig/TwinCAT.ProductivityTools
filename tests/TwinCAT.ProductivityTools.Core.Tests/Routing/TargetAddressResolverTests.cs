using System.Collections.Generic;
using System.Linq;
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
		public void Derives_the_address_from_the_net_id_when_no_route_matches()
		{
			// TwinCAT builds the AmsNetID of a system from its IPv4 address plus ".1.1", so the
			// first four octets are a usable address for a target that was never added as a
			// static route.
			new TargetAddressResolver(Reader())
				.Resolve("192.168.10.20.1.1")
				.Should()
				.Be("192.168.10.20");
		}

		[Fact]
		public void Returns_nothing_for_a_net_id_that_is_not_an_address()
		{
			new TargetAddressResolver(Reader()).Resolve("5.24.300.37.1.1").Should().BeNull();
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
		public void Falls_back_when_the_route_file_cannot_be_read()
		{
			IRouteReader reader = Substitute.For<IRouteReader>();
			reader.ListRoutes().Returns(_ => throw new System.IO.IOException());

			new TargetAddressResolver(reader).Resolve("192.168.1.1.1.1").Should().Be("192.168.1.1");
		}

		[Fact]
		public void Ignores_a_route_without_an_address()
		{
			IRouteReader reader = Reader(
				new TcConfigRoute { NetId = "192.168.1.1.1.1", Address = "" }
			);

			new TargetAddressResolver(reader).Resolve("192.168.1.1.1.1").Should().Be("192.168.1.1");
		}
	}
}
