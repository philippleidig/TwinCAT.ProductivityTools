using FluentAssertions;
using TwinCAT.Ads;
using TwinCAT.ProductivityTools.Routing;
using Xunit;

namespace TwinCAT.ProductivityTools.Core.Tests.Routing
{
	/// <summary>
	/// Pins the behaviour that <c>AmsNetId.TryParse</c> only pretends to have.
	/// </summary>
	/// <remarks>
	/// The ADS implementation throws an <c>ArgumentException</c> for null and for an empty string,
	/// which used to take down the RTE install dialog whenever the active project had no target
	/// yet. Every caller in the extension reads its input from a project property, a route file or
	/// a text box, so empty input is normal and has to come back as a plain "no".
	/// </remarks>
	public class AmsNetIdParserTests
	{
		[Theory]
		[InlineData("1.2.3.4.5.6")]
		[InlineData("192.168.0.1.1.1")]
		[InlineData("255.255.255.255.255.255")]
		public void AcceptsASixPartAddress(string value)
		{
			AmsNetId netId;

			AmsNetIdParser.TryParse(value, out netId).Should().BeTrue();
			netId.Should().NotBeNull();
			netId.ToString().Should().Be(value);
		}

		[Fact]
		public void TrimsSurroundingWhitespace()
		{
			AmsNetId netId;

			AmsNetIdParser.TryParse("  1.2.3.4.5.6\t", out netId).Should().BeTrue();
			netId.ToString().Should().Be("1.2.3.4.5.6");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		[InlineData("\t")]
		public void ReportsMissingInputInsteadOfThrowing(string value)
		{
			AmsNetId netId;

			AmsNetIdParser.TryParse(value, out netId).Should().BeFalse();
			netId.Should().BeNull();
		}

		[Theory]
		[InlineData("not an address")]
		[InlineData("1.2.3.4")]
		[InlineData("1.2.3.4.5")]
		[InlineData("1.2.3.4.5.6.7")]
		[InlineData("300.1.1.1.1.1")]
		[InlineData("256.1.1.1.1.1")]
		[InlineData("1.2.3.4.5.-6")]
		[InlineData("1.2.3.4.5.+6")]
		[InlineData("1.2.3.4.5. 6")]
		[InlineData("a.b.c.d.e.f")]
		[InlineData("......")]
		[InlineData("-1.2.3.4.5.6")]
		public void ReportsMalformedInputInsteadOfThrowing(string value)
		{
			AmsNetId netId;

			AmsNetIdParser.TryParse(value, out netId).Should().BeFalse();
			netId.Should().BeNull();
		}

		[Fact]
		public void IsValidMirrorsTryParse()
		{
			AmsNetIdParser.IsValid("1.2.3.4.5.6").Should().BeTrue();
			AmsNetIdParser.IsValid(null).Should().BeFalse();
			AmsNetIdParser.IsValid(string.Empty).Should().BeFalse();
			AmsNetIdParser.IsValid("nonsense").Should().BeFalse();
		}
	}
}
