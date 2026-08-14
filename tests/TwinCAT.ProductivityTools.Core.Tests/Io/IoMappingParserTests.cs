using System.Collections.Generic;
using FluentAssertions;
using TwinCAT.ProductivityTools.Io;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Io
{
	public class IoMappingParserTests
	{
		private readonly IoMappingParser parser = new IoMappingParser();

		private const string Mapping =
			@"<MappingInfo>
  <OwnerA Name='TIPC^Untitled1^Untitled1 Instance'>
    <OwnerB Name='TIID^Device 1 (EtherCAT)^Term 1 (EK1100)^Term 2 (EL1008)'>
      <Link VarA='PlcTask Inputs^MAIN.bInput' VarB='Channel 1^Input' Size='1' OffsA='0' />
      <Link VarA='PlcTask Inputs^MAIN.bSecond' VarB='Channel 2^Input' Size='1' OffsB='8' />
    </OwnerB>
  </OwnerA>
</MappingInfo>";

		[Fact]
		public void Groups_the_links_by_the_variable_of_the_first_owner()
		{
			IDictionary<string, List<Variable>> result = parser.Parse(Mapping);

			result.Should().ContainKey("TIPC^Untitled1^Untitled1 Instance^PlcTask Inputs^MAIN.bInput");
			result.Should().HaveCount(2);
		}

		[Fact]
		public void Builds_the_name_of_a_link_from_its_second_owner()
		{
			Variable variable = parser.Parse(Mapping)[
				"TIPC^Untitled1^Untitled1 Instance^PlcTask Inputs^MAIN.bInput"
			][0];

			variable
				.Name.Should()
				.Be(
					"TIID^Device 1 (EtherCAT)^Term 1 (EK1100)^Term 2 (EL1008)^Channel 1^Input"
				);
			variable.Path.Should().Be("Channel 1^Input");
			variable.Size.Should().Be(1);
		}

		[Fact]
		public void Prefers_the_offset_of_the_first_owner()
		{
			parser.Parse(Mapping)["TIPC^Untitled1^Untitled1 Instance^PlcTask Inputs^MAIN.bInput"][0]
				.Offset.Should()
				.Be(0);
		}

		[Fact]
		public void Falls_back_to_the_offset_of_the_second_owner()
		{
			parser.Parse(Mapping)["TIPC^Untitled1^Untitled1 Instance^PlcTask Inputs^MAIN.bSecond"][0]
				.Offset.Should()
				.Be(8);
		}

		[Fact]
		public void Collects_several_links_of_the_same_variable()
		{
			const string xml =
				@"<MappingInfo><OwnerA Name='A'>
                    <OwnerB Name='B'><Link VarA='v' VarB='x' /></OwnerB>
                    <OwnerB Name='C'><Link VarA='v' VarB='y' /></OwnerB>
                  </OwnerA></MappingInfo>";

			parser.Parse(xml)["A^v"].Should().HaveCount(2);
		}

		[Fact]
		public void Defaults_missing_size_and_offset_to_zero()
		{
			const string xml =
				"<MappingInfo><OwnerA Name='A'><OwnerB Name='B'><Link VarA='v' VarB='x' />"
				+ "</OwnerB></OwnerA></MappingInfo>";

			Variable variable = parser.Parse(xml)["A^v"][0];

			variable.Size.Should().Be(0);
			variable.Offset.Should().Be(0);
		}

		[Fact]
		public void Survives_attributes_that_are_not_numbers()
		{
			const string xml =
				"<MappingInfo><OwnerA Name='A'><OwnerB Name='B'>"
				+ "<Link VarA='v' VarB='x' Size='0.1' OffsA='n/a' /></OwnerB></OwnerA></MappingInfo>";

			parser.Parse(xml)["A^v"][0].Size.Should().Be(0);
		}

		[Fact]
		public void Survives_links_without_names()
		{
			const string xml =
				"<MappingInfo><OwnerA><OwnerB><Link /></OwnerB></OwnerA></MappingInfo>";

			parser.Parse(xml).Should().ContainKey("^");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		[InlineData("not xml at all <")]
		public void Returns_an_empty_result_for_unusable_input(string xml)
		{
			parser.Parse(xml).Should().BeEmpty();
		}

		[Fact]
		public void Returns_an_empty_result_for_a_configuration_without_mappings()
		{
			parser.Parse("<MappingInfo />").Should().BeEmpty();
		}
	}
}
