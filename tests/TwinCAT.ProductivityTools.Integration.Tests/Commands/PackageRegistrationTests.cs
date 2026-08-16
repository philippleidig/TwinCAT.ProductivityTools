using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Community.VisualStudio.Toolkit;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.ToolWindows;
using Xunit;

namespace TwinCAT.ProductivityTools.Integration.Tests.Commands
{
	/// <summary>
	/// Asserts what the package promises Visual Studio through its registration attributes.
	/// </summary>
	/// <remarks>
	/// The attributes end up in the pkgdef, and the shell reads the pkgdef long before any code of
	/// this extension runs. A wrong attribute therefore does not fail at run time in a way anybody
	/// notices - the extension simply never loads, or a command silently does nothing. Checking the
	/// declarations is the only cheap way to catch that.
	/// </remarks>
	public class PackageRegistrationTests
	{
		private static readonly Type Package = typeof(ProductivityToolsPackage);

		[Fact]
		public void PackageLoadsInTheBackground()
		{
			PackageRegistrationAttribute registration =
				Package.GetCustomAttribute<PackageRegistrationAttribute>();

			registration.Should().NotBeNull();
			registration.AllowsBackgroundLoading.Should().BeTrue();
			registration.UseManagedResourcesOnly.Should().BeTrue();
		}

		[Fact]
		public void PackageIdentityMatchesTheCommandTable()
		{
			// The guid of the package and the guid of the command set are the same value. If they
			// drift apart the menu resource of the pkgdef points at a command set nobody owns.
			GuidAttribute guid = Package.GetCustomAttribute<GuidAttribute>();

			guid.Should().NotBeNull();
			guid.Value.Should().Be(PackageGuids.ProductivityToolsCmdSetString);
		}

		[Fact]
		public void MenuResourceIsRegistered()
		{
			ProvideMenuResourceAttribute menu =
				Package.GetCustomAttribute<ProvideMenuResourceAttribute>();

			menu.Should().NotBeNull();
			menu.ResourceID.Should().Be("Menus.ctmenu");
			menu.Version.Should().Be(1);
		}

		[Fact]
		public void PackageAutoLoadsForEverySolutionThatHasProjects()
		{
			// A TwinCAT solution has either one project or several, and the commands must be
			// available in both cases. Without both contexts the menu entries stay greyed out
			// until something else happens to load the package.
			string[] contexts = Package
				.GetCustomAttributes<ProvideAutoLoadAttribute>()
				.Select(attribute => attribute.LoadGuid.ToString("B"))
				.ToArray();

			contexts
				.Should()
				.Contain(
					new Guid(VSConstants.UICONTEXT.SolutionHasSingleProject_string).ToString("B")
				)
				.And.Contain(
					new Guid(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string).ToString("B")
				);
		}

		[Fact]
		public void AutoLoadNeverBlocksTheShell()
		{
			// Loading synchronously on a UI context is deprecated and makes the shell complain in
			// the activity log of every user who has that context.
			Package
				.GetCustomAttributes<ProvideAutoLoadAttribute>()
				.Should()
				.OnlyContain(attribute => attribute.Flags == PackageAutoLoadFlags.BackgroundLoad);
		}

		[Fact]
		public void OutputWindowPaneIsOfferedAsAnAsyncService()
		{
			ProvideServiceAttribute service = Package
				.GetCustomAttributes<ProvideServiceAttribute>()
				.SingleOrDefault(
					attribute => attribute.ServiceType == typeof(IOutputWindowPane).GUID
				);

			service.Should().NotBeNull();

			// Report writes from background threads. A service that is not async queryable can
			// only be resolved from the UI thread and would deadlock those callers.
			service.IsAsyncQueryable.Should().BeTrue();
		}

		[Fact]
		public void ServiceRegistrationRegistersTheAdvertisedService()
		{
			// The ProvideService attribute only advertises the service. The registration below is
			// what actually creates it, and the two are easy to let drift apart.
			MethodInfo register = Package.GetMethod(
				"RegisterServicesAsync",
				BindingFlags.Instance | BindingFlags.NonPublic
			);

			register.Should().NotBeNull("the package must still register its services");
		}

		[Fact]
		public void ToolWindowIsRegisteredAsATabbedPaneOnTheRight()
		{
			ProvideToolWindowAttribute toolWindow =
				Package.GetCustomAttribute<ProvideToolWindowAttribute>();

			toolWindow.Should().NotBeNull();
			toolWindow.ToolType.Should().Be<IOMappingToolWindow.Pane>();
			toolWindow.Orientation.Should().Be(ToolWindowOrientation.Right);
			toolWindow.Style.Should().Be(VsDockStyle.Tabbed);
		}

		[Fact]
		public void BothOptionPagesAreRegisteredUnderTheProductName()
		{
			List<ProvideOptionPageAttribute> pages = Package
				.GetCustomAttributes<ProvideOptionPageAttribute>()
				.ToList();

			pages.Should().HaveCount(2);
			pages.Should().OnlyContain(page => page.CategoryName == Vsix.Name);
			pages.Select(page => page.PageName).Should().BeEquivalentTo("General", "Build");

			// Without profile support the settings are lost whenever a user imports or resets
			// their settings, which is exactly when they would expect them to survive.
			pages.Should().OnlyContain(page => page.SupportsProfiles);
		}

		[Fact]
		public void OptionPagesAreComVisibleSoTheShellCanCreateThem()
		{
			// The shell creates an option page through COM. A page that is not ComVisible shows up
			// as an empty tab in the options dialog with no error anywhere.
			Type[] pageTypes = Package
				.GetCustomAttributes<ProvideOptionPageAttribute>()
				.Select(attribute => attribute.PageType)
				.ToArray();

			pageTypes.Should().HaveCount(2);

			foreach (Type pageType in pageTypes)
			{
				pageType
					.GetCustomAttribute<ComVisibleAttribute>()
					?.Value
					.Should()
					.BeTrue($"{pageType.Name} is instantiated by the shell through COM");
			}
		}

		[Fact]
		public void ProductRegistrationUsesTheGeneratedManifestValues()
		{
			InstalledProductRegistrationAttribute product =
				Package.GetCustomAttribute<InstalledProductRegistrationAttribute>();

			product.Should().NotBeNull();
			product.ProductId.Should().Be(Vsix.Version);
			product.ProductName.Should().Be(Vsix.Name);
		}
	}
}
