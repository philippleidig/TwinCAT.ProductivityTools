using System;
using FluentAssertions;
using NSubstitute;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Remote;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Remote
{
	public class TargetConnectionFactoryTests
	{
		private const string Mstsc = @"C:\Windows\System32\mstsc.exe";
		private const string Ssh = @"C:\Windows\System32\OpenSSH\ssh.exe";

		[Fact]
		public void Offers_remote_desktop_to_a_windows_target()
		{
			Create()
				.For(TargetOperatingSystem.Windows, "Administrator")
				.Should()
				.BeOfType<RemoteDesktopConnection>();
		}

		[Theory]
		[InlineData(TargetOperatingSystem.Linux)]
		[InlineData(TargetOperatingSystem.Bsd)]
		public void Offers_ssh_to_a_target_without_a_remote_desktop_server(
			TargetOperatingSystem operatingSystem
		)
		{
			Create()
				.For(operatingSystem, "Administrator")
				.Should()
				.BeOfType<SecureShellConnection>();
		}

		[Fact]
		public void Offers_nothing_to_a_windows_ce_target()
		{
			// Windows CE serves neither remote desktop nor SSH; its remote configuration is the
			// Device Manager.
			Create().For(TargetOperatingSystem.WindowsCe, "Administrator").Should().BeNull();
		}

		[Fact]
		public void Refuses_to_choose_for_an_unknown_operating_system()
		{
			Action choose = () => Create().For(TargetOperatingSystem.Unknown, "Administrator");

			choose.Should().Throw<ArgumentOutOfRangeException>();
		}

		[Fact]
		public void Passes_the_configured_user_name_to_the_ssh_session()
		{
			ITargetConnection connection = Create().For(TargetOperatingSystem.Bsd, "root");

			connection.Build("10.0.0.5").Arguments.Should().Be("root@10.0.0.5");
		}

		private static TargetConnectionFactory Create()
		{
			ISshClientLocator locator = Substitute.For<ISshClientLocator>();
			locator.Locate().Returns(Ssh);

			return new TargetConnectionFactory(
				Fake.FileSystem(Mstsc),
				value => value.Replace("%SystemRoot%", @"C:\Windows"),
				locator
			);
		}
	}
}
