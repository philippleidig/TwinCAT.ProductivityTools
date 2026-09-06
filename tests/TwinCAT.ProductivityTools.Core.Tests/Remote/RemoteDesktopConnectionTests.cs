using FluentAssertions;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Remote;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Remote
{
	public class RemoteDesktopConnectionTests
	{
		private const string Mstsc = @"C:\Windows\System32\mstsc.exe";

		[Fact]
		public void Starts_the_client_that_ships_with_windows()
		{
			Create().Build("10.0.0.5").FileName.Should().Be(Mstsc);
		}

		[Fact]
		public void Passes_the_address_with_the_documented_switch()
		{
			Create().Build("10.0.0.5").Arguments.Should().Be("/v:10.0.0.5");
		}

		[Fact]
		public void Connects_to_a_host_name_as_well()
		{
			Create().Build(" CX-2A0D25 ").Arguments.Should().Be("/v:CX-2A0D25");
		}

		[Fact]
		public void Does_not_need_the_shell_for_a_windowed_client()
		{
			Create().Build("10.0.0.5").UseShellExecute.Should().BeFalse();
		}

		[Fact]
		public void Reports_nothing_when_the_client_is_missing()
		{
			RemoteDesktopConnection connection = new RemoteDesktopConnection(
				Fake.FileSystem(),
				Expand
			);

			connection.Build("10.0.0.5").Should().BeNull();
			connection.UnavailableMessage.Should().Contain("mstsc.exe");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		[InlineData("10.0.0.5 /v:evil")]
		public void Reports_nothing_for_an_unusable_address(string address)
		{
			Create().Build(address).Should().BeNull();
		}

		[Fact]
		public void Is_named_after_the_session_the_user_sees()
		{
			Create().DisplayName.Should().Be("Remote Desktop");
		}

		private static RemoteDesktopConnection Create()
		{
			IFileSystemProbe fileSystem = Fake.FileSystem(Mstsc);

			return new RemoteDesktopConnection(fileSystem, Expand);
		}

		private static string Expand(string value)
		{
			return value.Replace("%SystemRoot%", @"C:\Windows");
		}
	}
}
