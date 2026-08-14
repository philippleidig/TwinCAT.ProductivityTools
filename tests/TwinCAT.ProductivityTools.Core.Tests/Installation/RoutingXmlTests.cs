using FluentAssertions;
using TwinCAT.ProductivityTools.Installation;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Installation
{
	public class RoutingXmlTests
	{
		[Fact]
		public void Detects_that_relative_net_ids_are_enabled()
		{
			const string xml =
				"<TreeItem><RoutePrj><UseRelativeNetIds>true</UseRelativeNetIds></RoutePrj></TreeItem>";

			RoutingXml.IsUseRelativeNetIdsEnabled(xml).Should().BeTrue();
		}

		[Theory]
		[InlineData("TRUE")]
		[InlineData("True")]
		[InlineData(" true ")]
		[InlineData("1")]
		public void Accepts_the_spellings_the_system_manager_produces(string value)
		{
			RoutingXml
				.IsUseRelativeNetIdsEnabled($"<TreeItem><UseRelativeNetIds>{value}</UseRelativeNetIds></TreeItem>")
				.Should()
				.BeTrue();
		}

		[Fact]
		public void Detects_that_relative_net_ids_are_disabled()
		{
			const string xml =
				"<TreeItem><RoutePrj><UseRelativeNetIds>false</UseRelativeNetIds></RoutePrj></TreeItem>";

			RoutingXml.IsUseRelativeNetIdsEnabled(xml).Should().BeFalse();
		}

		[Fact]
		public void Treats_a_missing_setting_as_disabled()
		{
			RoutingXml.IsUseRelativeNetIdsEnabled("<TreeItem><RoutePrj /></TreeItem>").Should().BeFalse();
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		[InlineData("<TreeItem>")]
		public void Treats_unusable_input_as_disabled(string xml)
		{
			RoutingXml.IsUseRelativeNetIdsEnabled(xml).Should().BeFalse();
		}

		[Fact]
		public void Produces_a_document_the_system_manager_accepts()
		{
			string xml = RoutingXml.EnableUseRelativeNetIds();

			RoutingXml.IsUseRelativeNetIdsEnabled(xml).Should().BeTrue();
		}
	}
}
