using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using NSubstitute;
using TwinCAT.ProductivityTools.Installation;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Routing
{
	public class StaticRoutesReaderTests
	{
		private const string Routes =
			@"<?xml version='1.0' encoding='UTF-8'?>
<TcConfig>
  <RemoteConnections>
    <Route>
      <Name>CX-123456</Name>
      <Address>192.168.1.10</Address>
      <NetId>192.168.1.10.1.1</NetId>
      <Type>TCP_IP</Type>
    </Route>
    <Route>
      <Name>Testbench</Name>
      <Address>testbench.local</Address>
      <NetId>5.23.42.1.1.1</NetId>
      <Type>TCP_IP</Type>
    </Route>
  </RemoteConnections>
</TcConfig>";

		[Fact]
		public void Reads_every_configured_route()
		{
			Read(Routes).Should().HaveCount(2);
		}

		[Fact]
		public void Reads_the_fields_of_a_route()
		{
			TcConfigRoute route = Read(Routes).First();

			route.Name.Should().Be("CX-123456");
			route.Address.Should().Be("192.168.1.10");
			route.NetId.Should().Be("192.168.1.10.1.1");
			route.Type.Should().Be("TCP_IP");
		}

		[Fact]
		public void Returns_nothing_for_a_routing_file_without_routes()
		{
			Read("<TcConfig><RemoteConnections /></TcConfig>").Should().BeEmpty();
		}

		[Fact]
		public void Returns_nothing_for_a_routing_file_that_is_not_valid_xml()
		{
			Read("<TcConfig>").Should().BeEmpty();
		}

		[Fact]
		public void Returns_nothing_for_a_missing_stream()
		{
			StaticRoutesReader.Read(null).Should().BeEmpty();
		}

		[Fact]
		public void Returns_nothing_when_twincat_has_no_routing_file()
		{
			var installation = Substitute.For<ITwinCATInstallation>();
			installation.StaticRoutesPath.Returns((string)null);

			new StaticRoutesReader(installation).ListRoutes().Should().BeEmpty();
		}

		[Fact]
		public void Returns_nothing_when_the_routing_file_disappeared()
		{
			var installation = Substitute.For<ITwinCATInstallation>();
			installation.StaticRoutesPath.Returns(@"C:\does\not\exist\StaticRoutes.xml");

			new StaticRoutesReader(installation).ListRoutes().Should().BeEmpty();
		}

		[Fact]
		public void Reads_the_routing_file_of_the_local_installation()
		{
			string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".xml");

			File.WriteAllText(path, Routes, Encoding.UTF8);

			try
			{
				var installation = Substitute.For<ITwinCATInstallation>();
				installation.StaticRoutesPath.Returns(path);

				new StaticRoutesReader(installation).ListRoutes().Should().HaveCount(2);
			}
			finally
			{
				File.Delete(path);
			}
		}

		private static System.Collections.Generic.IEnumerable<TcConfigRoute> Read(string xml) =>
			StaticRoutesReader.Read(new MemoryStream(Encoding.UTF8.GetBytes(xml)));
	}
}
