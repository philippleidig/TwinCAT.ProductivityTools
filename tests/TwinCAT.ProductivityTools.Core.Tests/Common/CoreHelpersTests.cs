using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using TwinCAT.ProductivityTools.DataTypes;
using TwinCAT.ProductivityTools.Extensions;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Common
{
	public class ArrayHelpersTests
	{
		[Fact]
		public void Reads_an_ads_string_up_to_its_terminator()
		{
			byte[] value = Encoding.ASCII.GetBytes("CX-123456\0\0\0\0\0\0");

			ArrayHelpers.ByteArrayToString(value).Should().Be("CX-123456");
		}

		[Fact]
		public void Replaces_characters_outside_ascii()
		{
			byte[] value = { (byte)'a', 0xFF, (byte)'b', 0 };

			ArrayHelpers.ByteArrayToString(value).Should().Be("a?b");
		}

		[Fact]
		public void Returns_an_empty_string_for_no_value()
		{
			ArrayHelpers.ByteArrayToString(null).Should().BeEmpty();
			ArrayHelpers.ByteArrayToString(new byte[0]).Should().BeEmpty();
		}

		[Fact]
		public void Returns_an_empty_string_when_the_buffer_starts_with_a_terminator()
		{
			ArrayHelpers.ByteArrayToString(new byte[] { 0, (byte)'a' }).Should().BeEmpty();
		}
	}

	public class EnumTests
	{
		[Fact]
		public void Knows_the_defined_names()
		{
			Enum<EventSeverity>.IsDefined("WARN").Should().BeTrue();
			Enum<EventSeverity>.IsDefined("NOPE").Should().BeFalse();
		}

		[Fact]
		public void Knows_the_defined_values()
		{
			Enum<EventSeverity>.IsDefined(EventSeverity.ERROR).Should().BeTrue();
			Enum<EventSeverity>.IsDefined((EventSeverity)0x03).Should().BeFalse();
		}

		[Fact]
		public void Lists_every_value()
		{
			Enum<EventSeverity>.GetValues().Should().Contain(EventSeverity.HINT).And.HaveCount(7);
		}
	}

	public class ArrayExtensionTests
	{
		[Fact]
		public void Copies_a_slice()
		{
			new byte[] { 1, 2, 3, 4, 5 }.CopySlice(1, 3).Should().Equal(2, 3, 4);
		}

		[Fact]
		public void Shortens_a_slice_that_reaches_beyond_the_source()
		{
			new byte[] { 1, 2, 3 }.CopySlice(2, 3).Should().Equal(3);
		}

		[Fact]
		public void Pads_a_slice_that_reaches_beyond_the_source_when_asked_to()
		{
			new byte[] { 1, 2, 3 }.CopySlice(2, 3, padToLength: true).Should().Equal(3, 0, 0);
		}

		[Fact]
		public void Splits_a_buffer_into_slices()
		{
			IEnumerable<byte[]> slices = new byte[] { 1, 2, 3, 4, 5 }.Slices(2);

			slices.Should().HaveCount(3);
			slices.Last().Should().Equal(5);
		}
	}

	public class ListExtensionTests
	{
		[Fact]
		public void Adds_a_value_only_once()
		{
			var list = new List<string> { "a" };

			list.AddIfNotExists("a");
			list.AddIfNotExists("b");

			list.Should().Equal("a", "b");
		}

		[Fact]
		public void Replaces_a_value_in_place()
		{
			var list = new List<string> { "a", "b" };

			list.UpdateValue("b", "c");

			list.Should().Equal("a", "c");
		}

		[Fact]
		public void Removes_a_value_that_is_present()
		{
			var list = new List<string> { "a", "b" };

			list.DeleteIfExists("b");
			list.DeleteIfExists("z");

			list.Should().Equal("a");
		}

		[Fact]
		public void Detects_a_list_of_only_empty_values()
		{
			new List<string> { null, null }.AreValuesEmpty().Should().BeTrue();
			new List<string> { null, "a" }.AreValuesEmpty().Should().BeFalse();
		}

		[Fact]
		public void Rejects_a_missing_list()
		{
			List<string> list = null;

			Action add = () => list.AddIfNotExists("a");

			add.Should().Throw<ArgumentNullException>();
		}
	}

	public class StringBuilderExtensionsTests
	{
		[Fact]
		public void Puts_text_in_front_of_the_builder()
		{
			new StringBuilder("World").Preappend("Hello ").ToString().Should().Be("Hello World");
		}

		[Fact]
		public void Puts_a_repeated_character_in_front_of_the_builder()
		{
			new StringBuilder("1").Preappend('0', 3).ToString().Should().Be("0001");
		}

		[Fact]
		public void Keeps_the_builder_chainable()
		{
			new StringBuilder("c").Preappend("b").Preappend("a").ToString().Should().Be("abc");
		}
	}
}
