using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using TwinCAT.ProductivityTools.Io;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Io
{
	public class IoMappingCsvTests
	{
		[Fact]
		public void Always_starts_with_a_header()
		{
			IoMappingCsv.Build(null).Trim().Should().Be(IoMappingCsv.Header);
		}

		[Fact]
		public void Writes_one_record_per_variable()
		{
			string csv = IoMappingCsv.Build(
				new Dictionary<string, List<Variable>>
				{
					["Term 1^Channel 1"] = new List<Variable>
					{
						new Variable
						{
							Name = "Input",
							Path = "MAIN.bIn",
							Size = 1,
							Offset = 8
						}
					}
				}
			);

			csv.Should().Contain("Term 1 / Channel 1;Input;MAIN.bIn;1;8");
		}

		[Fact]
		public void Quotes_a_value_that_carries_a_separator()
		{
			string csv = IoMappingCsv.Build(
				new Dictionary<string, List<Variable>>
				{
					["Owner"] = new List<Variable>
					{
						new Variable { Name = "a;b", Path = "p" }
					}
				}
			);

			csv.Should().Contain("\"a;b\"");
		}

		[Fact]
		public void Doubles_a_quote_inside_a_quoted_value()
		{
			string csv = IoMappingCsv.Build(
				new Dictionary<string, List<Variable>>
				{
					["Owner"] = new List<Variable>
					{
						new Variable { Name = "a\"b", Path = "p" }
					}
				}
			);

			csv.Should().Contain("\"a\"\"b\"");
		}

		[Fact]
		public void Sorts_the_owners_so_that_two_exports_can_be_compared()
		{
			string csv = IoMappingCsv.Build(
				new Dictionary<string, List<Variable>>
				{
					["z"] = new List<Variable> { new Variable { Name = "n" } },
					["a"] = new List<Variable> { new Variable { Name = "n" } }
				}
			);

			string[] lines = csv.Split('\n').Where(l => l.Trim().Length > 0).ToArray();

			lines[1].Should().StartWith("a;");
			lines[2].Should().StartWith("z;");
		}

		[Fact]
		public void Skips_an_empty_mapping_without_failing()
		{
			string csv = IoMappingCsv.Build(
				new Dictionary<string, List<Variable>> { ["Owner"] = null }
			);

			csv.Trim().Should().Be(IoMappingCsv.Header);
		}
	}
}
