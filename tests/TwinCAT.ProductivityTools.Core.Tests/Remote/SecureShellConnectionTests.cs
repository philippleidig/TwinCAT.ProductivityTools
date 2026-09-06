using FluentAssertions;
using NSubstitute;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Remote;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Remote
{
	public class SecureShellConnectionTests
	{
		private const string Ssh = @"C:\Windows\System32\OpenSSH\ssh.exe";

		[Fact]
		public void Connects_as_the_configured_user()
		{
			Create("root").Build("10.0.0.5").Arguments.Should().Be("root@10.0.0.5");
		}

		[Fact]
		public void Uses_the_client_the_locator_found()
		{
			Create("root").Build("10.0.0.5").FileName.Should().Be(Ssh);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void Falls_back_to_the_account_every_beckhoff_image_ships(string userName)
		{
			Create(userName).Build("10.0.0.5").Arguments.Should().Be("Administrator@10.0.0.5");
		}

		[Fact]
		public void Opens_a_console_window()
		{
			// The session is interactive and Visual Studio has no console to lend to a console
			// application, so the shell has to start it. Turning this off makes the session
			// invisible and unusable.
			Create("root").Build("10.0.0.5").UseShellExecute.Should().BeTrue();
		}

		[Theory]
		[InlineData("-oProxyCommand=calc")]
		[InlineData("root me")]
		[InlineData("root@evil")]
		public void Refuses_a_user_name_that_would_add_an_argument(string userName)
		{
			// The user name comes from the options page, so it is input like any other.
			SecureShellConnection connection = Create(userName);

			connection.Build("10.0.0.5").Should().BeNull();
			connection.UnavailableMessage.Should().Contain("SSH user name");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("10.0.0.5 -oProxyCommand=calc")]
		public void Reports_nothing_for_an_unusable_address(string address)
		{
			Create("root").Build(address).Should().BeNull();
		}

		[Fact]
		public void Explains_how_to_install_the_openssh_client()
		{
			ISshClientLocator locator = Substitute.For<ISshClientLocator>();
			locator.Locate().Returns((string)null);

			SecureShellConnection connection = new SecureShellConnection(locator, "root");

			connection.Build("10.0.0.5").Should().BeNull();
			connection.UnavailableMessage.Should().Contain("Optional features");
		}

		[Fact]
		public void Is_named_after_the_session_the_user_sees()
		{
			Create("root").DisplayName.Should().Be("SSH");
		}

		private static SecureShellConnection Create(string userName)
		{
			ISshClientLocator locator = Substitute.For<ISshClientLocator>();
			locator.Locate().Returns(Ssh);

			return new SecureShellConnection(locator, userName);
		}
	}
}
