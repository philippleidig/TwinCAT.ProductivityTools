using FluentAssertions;
using TwinCAT.ProductivityTools.Remote;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Remote
{
	public class DeviceManagerUrlTests
	{
		[Fact]
		public void Builds_the_configuration_page_of_a_target()
		{
			DeviceManagerUrl.For("10.0.0.5").Should().Be("https://10.0.0.5/config");
		}

		[Fact]
		public void Works_with_a_host_name()
		{
			DeviceManagerUrl.For("CX-2A0D25").Should().Be("https://CX-2A0D25/config");
		}

		[Fact]
		public void Brackets_an_ipv6_literal()
		{
			DeviceManagerUrl.For("fe80::1").Should().Be("https://[fe80::1]/config");
		}

		[Fact]
		public void Always_uses_https()
		{
			// The Device Manager only listens on HTTPS, with a self signed certificate.
			DeviceManagerUrl.For("10.0.0.5").Should().StartWith("https://");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		[InlineData("10.0.0.5/../evil")]
		[InlineData("http://10.0.0.5")]
		public void Reports_nothing_for_an_unusable_address(string address)
		{
			DeviceManagerUrl.For(address).Should().BeNull();
		}
	}
}
