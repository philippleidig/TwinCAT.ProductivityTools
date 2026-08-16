using System;
using FluentAssertions;
using TwinCAT.ProductivityTools.Installation;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Installation
{
	public class TargetPathsTests
	{
		[Theory]
		[InlineData(3, 1, 4024, 60)]
		[InlineData(3, 1, 4022, 0)]
		public void Uses_the_legacy_layout_up_to_4024(int major, int minor, int build, int revision)
		{
			Version version = new Version(major, minor, build, revision);

			TargetPaths.IsReorganized(version).Should().BeFalse();
			TargetPaths
				.SetTickScript(version)
				.Should()
				.Be(@"C:\TwinCAT\3.1\System\win8settick.bat");
		}

		[Theory]
		[InlineData(3, 1, 4026, 0)]
		[InlineData(3, 1, 4026, 21)]
		[InlineData(3, 1, 4028, 0)]
		public void Uses_the_program_files_layout_from_4026(
			int major,
			int minor,
			int build,
			int revision
		)
		{
			Version version = new Version(major, minor, build, revision);

			TargetPaths.IsReorganized(version).Should().BeTrue();
			TargetPaths
				.SetTickScript(version)
				.Should()
				.Be(@"C:\Program Files (x86)\Beckhoff\TwinCAT\3.1\System\win8settick.bat");
		}

		[Fact]
		public void Falls_back_to_the_legacy_layout_for_an_unknown_version()
		{
			// A target that does not answer the version request is far more likely to be an old
			// one, so the fallback points at the directory such a target actually has.
			TargetPaths.IsReorganized(null).Should().BeFalse();
			TargetPaths.SystemDirectory(null).Should().Be(TargetPaths.LegacySystemDirectory);
		}
	}
}
