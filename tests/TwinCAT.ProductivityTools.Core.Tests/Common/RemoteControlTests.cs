using System;
using System.Text;
using FluentAssertions;
using Xunit;

namespace TwinCAT.ProductivityTools.Core.Tests.Common
{
	public class RemoteControlTests
	{
		[Fact]
		public void BuildStartProcessRequest_writes_the_three_lengths_first()
		{
			byte[] request = RemoteControl.BuildStartProcessRequest(
				@"C:\TwinCAT\3.1\System\TcRteInstall.exe",
				@"C:\TwinCAT\3.1\System",
				"-r installnic \"LAN\""
			);

			BitConverter.ToInt32(request, 0).Should().Be(38);
			BitConverter.ToInt32(request, 4).Should().Be(21);
			BitConverter.ToInt32(request, 8).Should().Be(19);
		}

		[Fact]
		public void BuildStartProcessRequest_separates_the_strings_with_a_terminator()
		{
			byte[] request = RemoteControl.BuildStartProcessRequest("a", "bb", "ccc");

			Encoding.ASCII.GetString(request, 12, 1).Should().Be("a");
			request[13].Should().Be(0);

			Encoding.ASCII.GetString(request, 14, 2).Should().Be("bb");
			request[16].Should().Be(0);

			Encoding.ASCII.GetString(request, 17, 3).Should().Be("ccc");
			request[20].Should().Be(0);
		}

		[Fact]
		public void BuildStartProcessRequest_always_returns_the_fixed_request_size()
		{
			RemoteControl
				.BuildStartProcessRequest("a", "b", "c")
				.Should()
				.HaveCount(RemoteControl.StartProcessRequestSize);
		}

		[Fact]
		public void BuildStartProcessRequest_accepts_a_missing_directory_and_arguments()
		{
			Action build = () => RemoteControl.BuildStartProcessRequest("a", null, null);

			build.Should().NotThrow();
		}

		[Fact]
		public void BuildStartProcessRequest_rejects_arguments_that_do_not_fit()
		{
			// The request has a fixed size. Writing past its end used to throw an
			// IndexOutOfRangeException from inside Array.CopyTo, which told the user nothing.
			Action build = () =>
				RemoteControl.BuildStartProcessRequest(
					new string('a', 400),
					new string('b', 400),
					"args"
				);

			build.Should().Throw<ArgumentException>().WithMessage("*too long*");
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void BuildStartProcessRequest_rejects_a_missing_path(string path)
		{
			Action build = () => RemoteControl.BuildStartProcessRequest(path, "dir", "args");

			build.Should().Throw<ArgumentException>();
		}

		[Fact]
		public void IsTargetReachable_reports_false_for_a_missing_target()
		{
			RemoteControl.IsTargetReachable(null).Should().BeFalse();
		}
	}
}
