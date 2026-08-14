using FluentAssertions;
using TwinCAT.ProductivityTools.Installation;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Installation
{
	/// <summary>
	/// TwinCAT 4026 moved every path this extension depends on. These tests pin both layouts down
	/// with the paths that were taken from real 4024 and 4026 installations.
	/// </summary>
	public class TwinCATInstallationTests
	{
		private const string ProgramData = @"C:\ProgramData";

		private const string LegacyRoot = @"C:\TwinCAT\";
		private const string LegacyRoutes = @"C:\TwinCAT\3.1\Target\StaticRoutes.xml";
		private const string LegacyTemplates =
			@"C:\TwinCAT\3.1\Components\Plc\PlcTemplates\1.0.0.0\Plc Templates\Standard.plcproj";

		private const string CurrentRoot = @"C:\Program Files (x86)\Beckhoff\TwinCAT\";
		private const string CurrentRoutes =
			@"C:\ProgramData\Beckhoff\TwinCAT\3.1\Runtimes\UmRT_Default\3.1\Target\StaticRoutes.xml";
		private const string CurrentTemplates =
			@"C:\ProgramData\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates\1.1.0.1\TemplatesDir\Standard PLC Template.vsdir";

		[Fact]
		public void Reports_that_twincat_is_missing_when_the_registry_is_empty()
		{
			TwinCATInstallation installation = Create(Fake.Registry());

			installation.IsInstalled.Should().BeFalse();
			installation.Build.Should().Be(0);
			installation.StaticRoutesPath.Should().BeNull();
			installation.PlcTemplatesDirectory.Should().BeNull();
		}

		[Fact]
		public void Reads_the_build_from_the_registry()
		{
			Create(Fake.Registry(CurrentRoot, 4026)).Build.Should().Be(4026);
		}

		[Fact]
		public void Finds_the_static_routes_of_a_4024_installation()
		{
			TwinCATInstallation installation = Create(
				Fake.Registry(LegacyRoot, 4024),
				LegacyRoutes
			);

			installation.StaticRoutesPath.Should().Be(LegacyRoutes);
		}

		[Fact]
		public void Finds_the_static_routes_of_a_4026_installation_below_program_data()
		{
			TwinCATInstallation installation = Create(
				Fake.Registry(CurrentRoot, 4026),
				CurrentRoutes
			);

			installation.StaticRoutesPath.Should().Be(CurrentRoutes);
		}

		[Fact]
		public void Prefers_the_default_runtime_when_several_runtimes_exist()
		{
			TwinCATInstallation installation = Create(
				Fake.Registry(CurrentRoot, 4026),
				CurrentRoutes,
				@"C:\ProgramData\Beckhoff\TwinCAT\3.1\Runtimes\Machine\3.1\Target\StaticRoutes.xml"
			);

			installation.StaticRoutesPath.Should().Be(CurrentRoutes);
		}

		[Fact]
		public void Falls_back_to_another_runtime_when_the_default_one_has_no_routes()
		{
			const string other =
				@"C:\ProgramData\Beckhoff\TwinCAT\3.1\Runtimes\Machine\3.1\Target\StaticRoutes.xml";

			TwinCATInstallation installation = Create(Fake.Registry(CurrentRoot, 4026), other);

			installation.StaticRoutesPath.Should().Be(other);
		}

		[Fact]
		public void Does_not_use_the_4024_routing_path_on_4026()
		{
			TwinCATInstallation installation = Create(
				Fake.Registry(CurrentRoot, 4026),
				LegacyRoutes
			);

			installation.StaticRoutesPath.Should().BeNull();
		}

		[Fact]
		public void Returns_null_when_the_routing_file_has_not_been_created_yet()
		{
			Create(Fake.Registry(LegacyRoot, 4024)).StaticRoutesPath.Should().BeNull();
		}

		[Fact]
		public void Finds_the_plc_templates_of_a_4024_installation()
		{
			TwinCATInstallation installation = Create(
				Fake.Registry(LegacyRoot, 4024),
				LegacyTemplates
			);

			installation
				.PlcTemplatesDirectory.Should()
				.Be(@"C:\TwinCAT\3.1\Components\Plc\PlcTemplates\1.0.0.0\Plc Templates");
		}

		[Fact]
		public void Finds_the_plc_templates_of_a_4026_installation()
		{
			TwinCATInstallation installation = Create(
				Fake.Registry(CurrentRoot, 4026),
				CurrentTemplates
			);

			installation
				.PlcTemplatesDirectory.Should()
				.Be(@"C:\ProgramData\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates\1.1.0.1");
		}

		[Fact]
		public void Uses_the_newest_template_package_that_is_installed()
		{
			TwinCATInstallation installation = Create(
				Fake.Registry(CurrentRoot, 4026),
				CurrentTemplates,
				@"C:\ProgramData\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates\1.0.0.0\a.vsdir",
				@"C:\ProgramData\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates\1.1.0.0\a.vsdir"
			);

			installation
				.PlcTemplatesDirectory.Should()
				.Be(@"C:\ProgramData\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates\1.1.0.1");
		}

		[Fact]
		public void Compares_template_versions_as_numbers_not_as_text()
		{
			TwinCATInstallation installation = Create(
				Fake.Registry(CurrentRoot, 4026),
				@"C:\ProgramData\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates\1.9.0.0\a.vsdir",
				@"C:\ProgramData\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates\1.10.0.0\a.vsdir"
			);

			installation
				.PlcTemplatesDirectory.Should()
				.Be(@"C:\ProgramData\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates\1.10.0.0");
		}

		[Fact]
		public void Ignores_directories_that_are_not_versions()
		{
			TwinCATInstallation installation = Create(
				Fake.Registry(CurrentRoot, 4026),
				@"C:\ProgramData\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates\backup\a.vsdir"
			);

			installation.PlcTemplatesDirectory.Should().BeNull();
		}

		[Fact]
		public void Treats_a_build_beyond_4026_like_the_current_layout()
		{
			TwinCATInstallation installation = Create(
				Fake.Registry(CurrentRoot, 4030),
				CurrentRoutes
			);

			installation.StaticRoutesPath.Should().Be(CurrentRoutes);
		}

		private static TwinCATInstallation Create(
			Abstractions.IRegistryProvider registry,
			params string[] existingFiles
		) => new TwinCATInstallation(registry, Fake.FileSystem(existingFiles), ProgramData);
	}
}
