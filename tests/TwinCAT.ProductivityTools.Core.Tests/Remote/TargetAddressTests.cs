using FluentAssertions;
using TwinCAT.ProductivityTools.Remote;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Remote
{
	/// <summary>
	/// The address is read from a route file the extension does not own, so it is input and is
	/// checked before it reaches a command line or a URL.
	/// </summary>
	public class TargetAddressTests
	{
		[Theory]
		[InlineData("10.0.0.5")]
		[InlineData("CX-2A0D25")]
		[InlineData("testbench.local")]
		[InlineData("fe80::1")]
		public void Accepts_an_address_that_names_a_host(string address)
		{
			TargetAddress.IsUsable(address).Should().BeTrue();
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		[InlineData("10.0.0.5 -oProxyCommand=calc")]
		[InlineData("10.0.0.5/../config")]
		[InlineData("http://10.0.0.5")]
		[InlineData("a b")]
		public void Rejects_anything_that_would_become_a_second_argument(string address)
		{
			TargetAddress.IsUsable(address).Should().BeFalse();
			TargetAddress.Normalize(address).Should().BeNull();
			TargetAddress.ForUrl(address).Should().BeNull();
		}

		[Fact]
		public void Trims_the_address()
		{
			TargetAddress.Normalize("  10.0.0.5\t").Should().Be("10.0.0.5");
		}

		[Fact]
		public void Brackets_an_ipv6_literal_for_a_url()
		{
			// Without the brackets the colons of the address read as the port separator.
			TargetAddress.ForUrl("fe80::1").Should().Be("[fe80::1]");
		}

		[Fact]
		public void Leaves_every_other_address_alone_in_a_url()
		{
			TargetAddress.ForUrl("10.0.0.5").Should().Be("10.0.0.5");
			TargetAddress.ForUrl("CX-2A0D25").Should().Be("CX-2A0D25");
		}
	}
}
