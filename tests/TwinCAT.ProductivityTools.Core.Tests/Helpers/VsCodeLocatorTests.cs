using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Win32;
using NSubstitute;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Helpers;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Helpers
{
	public class VsCodeLocatorTests
	{
		private const string UserInstallation =
			@"C:\Users\tester\AppData\Local\Programs\Microsoft VS Code\Code.exe";

		private const string MachineInstallation =
			@"C:\Program Files\Microsoft VS Code\Code.exe";

		[Fact]
		public void Finds_the_editor_through_the_shell_integration_key()
		{
			VsCodeLocator locator = Create(
				registryValue: UserInstallation,
				existingFiles: new[] { UserInstallation }
			);

			locator.Locate().Should().Be(UserInstallation);
		}

		[Fact]
		public void Ignores_a_registry_entry_that_points_at_a_removed_installation()
		{
			VsCodeLocator locator = Create(registryValue: UserInstallation);

			locator.Locate().Should().BeNull();
		}

		[Fact]
		public void Finds_the_editor_next_to_its_bin_directory_on_the_path()
		{
			VsCodeLocator locator = Create(
				path: @"C:\Windows;C:\Program Files\Microsoft VS Code\bin",
				existingFiles: new[] { MachineInstallation }
			);

			locator.Locate().Should().Be(MachineInstallation);
		}

		[Fact]
		public void Finds_the_editor_directly_on_the_path()
		{
			VsCodeLocator locator = Create(
				path: @"C:\Program Files\Microsoft VS Code",
				existingFiles: new[] { MachineInstallation }
			);

			locator.Locate().Should().Be(MachineInstallation);
		}

		[Fact]
		public void Ignores_quoted_and_empty_path_entries()
		{
			VsCodeLocator locator = Create(
				path: @";;""C:\Program Files\Microsoft VS Code"";",
				existingFiles: new[] { MachineInstallation }
			);

			locator.Locate().Should().Be(MachineInstallation);
		}

		[Fact]
		public void Finds_a_per_user_installation_through_local_app_data()
		{
			VsCodeLocator locator = Create(
				localAppData: @"C:\Users\tester\AppData\Local",
				existingFiles: new[] { UserInstallation }
			);

			locator.Locate().Should().Be(UserInstallation);
		}

		[Fact]
		public void Returns_null_when_visual_studio_code_is_not_installed()
		{
			Create().Locate().Should().BeNull();
		}

		[Fact]
		public void Survives_an_environment_without_the_variables_it_reads()
		{
			VsCodeLocator locator = new VsCodeLocator(
				Substitute.For<IRegistryProvider>(),
				Fake.FileSystem(),
				name => null
			);

			locator.Locate().Should().BeNull();
		}

		[Fact]
		public void Survives_a_path_entry_that_is_not_a_valid_directory()
		{
			VsCodeLocator locator = Create(path: "C:\\va|id?;");

			locator.Invoking(l => l.Locate()).Should().NotThrow();
		}

		private static VsCodeLocator Create(
			string registryValue = null,
			string path = null,
			string localAppData = null,
			string[] existingFiles = null
		)
		{
			var registry = Substitute.For<IRegistryProvider>();

			registry
				.GetValue(
					RegistryHive.CurrentUser,
					RegistryView.Default,
					VsCodeLocator.ShellRegistryKey,
					VsCodeLocator.ShellRegistryValue
				)
				.Returns(registryValue);

			var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["Path"] = path,
				["LOCALAPPDATA"] = localAppData,
			};

			return new VsCodeLocator(
				registry,
				Fake.FileSystem(existingFiles ?? new string[0]),
				name => environment.TryGetValue(name, out string value) ? value : null
			);
		}
	}
}
