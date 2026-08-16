using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using TwinCAT.ProductivityTools.Plc;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Plc
{
	public class ProjectFreezeXmlTests
	{
		private const string PlcProjectNamespace =
			"http://schemas.microsoft.com/developer/msbuild/2003";

		[Fact]
		public void Marks_the_solution_project_as_version_fixed()
		{
			XDocument document = XDocument.Parse("<TcSmProject><Project /></TcSmProject>");

			ProjectFreezeXml.FreezeSolutionProject(document).Should().BeTrue();
			document.Root.Attribute("TcVersionFixed").Value.Should().Be("true");
		}

		[Fact]
		public void Reports_no_change_when_the_solution_project_is_already_frozen()
		{
			XDocument document = XDocument.Parse("<TcSmProject TcVersionFixed=\"true\" />");

			ProjectFreezeXml.FreezeSolutionProject(document).Should().BeFalse();
		}

		[Fact]
		public void Repairs_a_solution_project_that_was_explicitly_unfrozen()
		{
			XDocument document = XDocument.Parse("<TcSmProject TcVersionFixed=\"false\" />");

			ProjectFreezeXml.FreezeSolutionProject(document).Should().BeTrue();
			document.Root.Attribute("TcVersionFixed").Value.Should().Be("true");
		}

		[Fact]
		public void Ignores_a_document_that_is_not_a_solution_project()
		{
			XDocument document = XDocument.Parse("<Something />");

			ProjectFreezeXml.FreezeSolutionProject(document).Should().BeFalse();
		}

		[Fact]
		public void Adds_the_compiler_version_to_a_plc_project()
		{
			XDocument document = PlcProject("<PropertyGroup><Name>Untitled</Name></PropertyGroup>");

			ProjectFreezeXml.FreezePlcProject(document, "3.5.19.20").Should().BeTrue();

			CompilerVersion(document).Should().Be("3.5.19.20");
		}

		[Fact]
		public void Writes_the_compiler_version_into_the_namespace_of_the_document()
		{
			XDocument document = PlcProject("<PropertyGroup />");

			ProjectFreezeXml.FreezePlcProject(document, "3.5.19.20");

			document
				.Descendants(XName.Get("CompilerVersion", PlcProjectNamespace))
				.Should()
				.HaveCount(1);
		}

		[Fact]
		public void Updates_an_existing_compiler_version_instead_of_adding_a_second_one()
		{
			XDocument document = PlcProject(
				"<PropertyGroup><CompilerVersion>3.5.17.0</CompilerVersion></PropertyGroup>"
			);

			ProjectFreezeXml.FreezePlcProject(document, "3.5.19.20").Should().BeTrue();

			document
				.Descendants()
				.Should()
				.ContainSingle(e => e.Name.LocalName == "CompilerVersion");
			CompilerVersion(document).Should().Be("3.5.19.20");
		}

		[Fact]
		public void Freezing_a_plc_project_twice_changes_nothing_the_second_time()
		{
			XDocument document = PlcProject(
				"<PropertyGroup><SecureOnlineMode>true</SecureOnlineMode></PropertyGroup>"
			);

			ProjectFreezeXml.FreezePlcProject(document, "3.5.19.20").Should().BeTrue();
			ProjectFreezeXml.FreezePlcProject(document, "3.5.19.20").Should().BeFalse();
		}

		[Theory]
		[InlineData("SecureOnlineMode")]
		[InlineData("AutoUpdateVisuProfile")]
		[InlineData("AutoUpdateUmlProfile")]
		public void Removes_the_settings_that_would_update_the_project_on_load(string element)
		{
			XDocument document = PlcProject(
				$"<PropertyGroup><{element}>true</{element}></PropertyGroup>"
			);

			ProjectFreezeXml.FreezePlcProject(document, null).Should().BeTrue();

			document.Descendants().Should().NotContain(e => e.Name.LocalName == element);
		}

		[Fact]
		public void Keeps_conditioned_property_groups_untouched()
		{
			XDocument document = PlcProject(
				"<PropertyGroup Condition=\" '$(Configuration)' == 'Release' \">"
					+ "<SecureOnlineMode>true</SecureOnlineMode></PropertyGroup>"
			);

			ProjectFreezeXml.FreezePlcProject(document, null);

			document.Descendants().Should().Contain(e => e.Name.LocalName == "SecureOnlineMode");
		}

		[Fact]
		public void Creates_a_property_group_when_the_project_has_none()
		{
			XDocument document = PlcProject(string.Empty);

			ProjectFreezeXml.FreezePlcProject(document, "3.5.19.20").Should().BeTrue();

			CompilerVersion(document).Should().Be("3.5.19.20");
		}

		[Fact]
		public void Reads_the_active_compiler_version_of_a_plc_project_node()
		{
			XDocument document = XDocument.Parse(
				"<TreeItem><IECProjectDef><CompilerSettings>"
					+ "<ActiveCompiler>3.5.19.20</ActiveCompiler>"
					+ "</CompilerSettings></IECProjectDef></TreeItem>"
			);

			ProjectFreezeXml.ReadActiveCompilerVersion(document).Should().Be("3.5.19.20");
		}

		[Fact]
		public void Returns_null_when_the_node_does_not_carry_a_compiler_version()
		{
			XDocument document = XDocument.Parse("<TreeItem />");

			ProjectFreezeXml.ReadActiveCompilerVersion(document).Should().BeNull();
		}

		[Fact]
		public void Reads_the_project_path_of_a_plc_project_root_node()
		{
			XDocument document = XDocument.Parse(
				"<TreeItem><PlcProjectDef><ProjectPath>C:\\p\\Untitled1.plcproj</ProjectPath>"
					+ "</PlcProjectDef></TreeItem>"
			);

			ProjectFreezeXml.ReadProjectPath(document).Should().Be("C:\\p\\Untitled1.plcproj");
		}

		private static XDocument PlcProject(string body) =>
			XDocument.Parse($"<Project xmlns=\"{PlcProjectNamespace}\">{body}</Project>");

		private static string CompilerVersion(XDocument document) =>
			document.Descendants(XName.Get("CompilerVersion", PlcProjectNamespace)).Single().Value;
	}
}
