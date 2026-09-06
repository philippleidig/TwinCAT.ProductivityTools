using System;
using FluentAssertions;
using TwinCAT.ProductivityTools.Infrastructure;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Infrastructure
{
	/// <summary>
	/// Only the guard is covered here. Everything that decides what to start is tested on the
	/// connections; starting a real process is not something a unit test should do.
	/// </summary>
	public class SystemProcessLauncherTests
	{
		[Fact]
		public void Refuses_a_missing_launch()
		{
			Action start = () => SystemProcessLauncher.Instance.Start(null);

			start.Should().Throw<ArgumentNullException>();
		}
	}
}
