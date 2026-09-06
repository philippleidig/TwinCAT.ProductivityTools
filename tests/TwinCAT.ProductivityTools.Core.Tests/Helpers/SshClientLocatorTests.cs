using System.Collections.Generic;
using FluentAssertions;
using TwinCAT.ProductivityTools.Helpers;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Helpers
{
	public class SshClientLocatorTests
	{
		private const string InBox = @"C:\Windows\System32\OpenSSH\ssh.exe";

		/// <summary>
		/// The same file, seen from a 32 bit Visual Studio, for which Windows redirects
		/// <c>System32</c> to <c>SysWOW64</c>.
		/// </summary>
		private const string Native = @"C:\Windows\Sysnative\OpenSSH\ssh.exe";

		private const string GitForWindows = @"C:\Program Files\Git\usr\bin\ssh.exe";

		[Fact]
		public void Finds_the_client_that_ships_with_windows()
		{
			Create(existingFiles: new[] { InBox }).Locate().Should().Be(InBox);
		}

		[Fact]
		public void Finds_the_client_of_a_32_bit_process_through_sysnative()
		{
			// A 32 bit host sees no OpenSSH below System32, because that path is redirected to
			// SysWOW64. Reporting "not installed" there would be wrong on a machine that has it.
			Create(existingFiles: new[] { Native }).Locate().Should().Be(Native);
		}

		[Fact]
		public void Prefers_the_client_that_ships_with_windows_over_one_on_the_path()
		{
			SshClientLocator locator = Create(
				path: @"C:\Program Files\Git\usr\bin",
				existingFiles: new[] { InBox, GitForWindows }
			);

			locator.Locate().Should().Be(InBox);
		}

		[Fact]
		public void Finds_a_client_on_the_path()
		{
			SshClientLocator locator = Create(
				path: @"C:\Program Files\Git\usr\bin",
				existingFiles: new[] { GitForWindows }
			);

			locator.Locate().Should().Be(GitForWindows);
		}

		[Fact]
		public void Ignores_quoted_and_empty_path_entries()
		{
			SshClientLocator locator = Create(
				path: @";;""C:\Program Files\Git\usr\bin"";",
				existingFiles: new[] { GitForWindows }
			);

			locator.Locate().Should().Be(GitForWindows);
		}

		[Fact]
		public void Reports_nothing_when_openssh_is_not_installed()
		{
			Create().Locate().Should().BeNull();
		}

		[Fact]
		public void Survives_a_path_entry_that_is_not_a_directory()
		{
			SshClientLocator locator = Create(path: "C:\\va|id?");

			locator.Invoking(candidate => candidate.Locate()).Should().NotThrow();
		}

		private static SshClientLocator Create(
			string path = null,
			string[] existingFiles = null
		)
		{
			Dictionary<string, string> environment = new Dictionary<string, string>
			{
				{ "Path", path },
			};

			return new SshClientLocator(
				Fake.FileSystem(existingFiles ?? new string[0]),
				value => value.Replace("%SystemRoot%", @"C:\Windows"),
				name => environment.TryGetValue(name, out string value) ? value : null
			);
		}
	}
}
