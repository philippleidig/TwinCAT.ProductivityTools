using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using TwinCAT.ProductivityTools.Plc;
using Xunit;

namespace TwinCAT.ProductivityTools.Tests.Architecture
{
	/// <summary>
	/// The core library only stays unit testable as long as it does not reach into the Visual Studio
	/// shell or into COM. These tests fail as soon as someone adds such a dependency.
	/// </summary>
	public class CoreDependencyTests
	{
		private static readonly Assembly Core = typeof(CommentRemover).Assembly;

		private static readonly string[] ForbiddenAssemblies =
		{
			"Microsoft.VisualStudio",
			"Community.VisualStudio.Toolkit",
			"EnvDTE",
			"TCatSysManagerLib",
			"PresentationFramework",
			"System.Windows.Forms",
		};

		/// <summary>
		/// Code coverage instrumentation adds a reference to its own shim to the assembly it
		/// instruments. That reference only exists while the suite runs under a collector, it is
		/// not part of the shipped library, and its name would otherwise trip the
		/// Microsoft.VisualStudio prefix below.
		/// </summary>
		private static readonly string[] InstrumentationAssemblies =
		{
			"Microsoft.VisualStudio.CodeCoverage.Shim",
		};

		[Fact]
		public void The_core_library_does_not_reference_the_visual_studio_shell()
		{
			IEnumerable<string> referenced = Core.GetReferencedAssemblies()
				.Select(name => name.Name)
				.Where(
					name =>
						!InstrumentationAssemblies.Contains(name, StringComparer.OrdinalIgnoreCase)
				);

			referenced
				.Should()
				.NotContain(
					name =>
						ForbiddenAssemblies.Any(
							forbidden =>
								name.StartsWith(forbidden, StringComparison.OrdinalIgnoreCase)
						)
				);
		}

		[Fact]
		public void The_core_library_does_not_expose_com_types()
		{
			IEnumerable<Type> comTypes = Core.GetTypes().Where(type => type.IsImport);

			comTypes.Should().BeEmpty();
		}

		[Fact]
		public void The_core_library_targets_the_framework_the_extension_runs_on()
		{
			Core.ImageRuntimeVersion.Should().StartWith("v4");
		}

		[Fact]
		public void The_core_library_is_strong_named_so_that_the_signed_vsix_can_use_it()
		{
			Core.GetName().GetPublicKeyToken().Should().NotBeEmpty();
		}
	}
}
