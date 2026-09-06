using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Community.VisualStudio.Toolkit;
using FluentAssertions;
using TwinCAT.ProductivityTools.Options;
using Xunit;

namespace TwinCAT.ProductivityTools.Integration.Tests.Services
{
	/// <summary>
	/// Covers the settings the extension exposes in Tools / Options.
	/// </summary>
	/// <remarks>
	/// Every property of an option model becomes a row in the options grid, and the shell builds
	/// that row entirely from the attributes. A property without a display name shows up as its
	/// identifier, and a property without a description leaves the user guessing. The defaults are
	/// what a fresh installation behaves like, so they are pinned as well.
	/// </remarks>
	public class OptionModelTests
	{
		[Fact]
		public void BuildArtifactCleanupIsOffByDefault()
		{
			// Deleting files from disk is destructive enough that it has to be opted into.
			new Options.Build()
				.DeleteBuildArtifactsOnClean.Should()
				.BeFalse();
		}

		[Fact]
		public void TheVsCodePathIsEmptyUntilItIsDetected()
		{
			// A hard coded per user path is wrong on a machine with a system wide installation,
			// and it puts the account name of whoever installed the extension into the options
			// grid. An empty value makes the command detect the installation instead.
			new General()
				.VsCodeInstallPath.Should()
				.BeEmpty();
		}

		[Fact]
		public void TheSshUserNameDefaultsToTheAccountEveryBeckhoffImageShips()
		{
			// A blank default would make the connect command build "ssh @10.0.0.5" on its first
			// use, so the setting starts out with the account the images actually carry.
			new General().SshUserName.Should().Be("Administrator");
		}

		[Theory]
		[InlineData(typeof(General))]
		[InlineData(typeof(Options.Build))]
		public void EverySettingIsDescribedForTheOptionsGrid(Type model)
		{
			foreach (PropertyInfo property in SettingsOf(model))
			{
				property
					.GetCustomAttribute<DisplayNameAttribute>()
					.Should()
					.NotBeNull("{0} needs a caption in the options grid", property.Name);

				property
					.GetCustomAttribute<DescriptionAttribute>()
					.Should()
					.NotBeNull("{0} needs a description in the options grid", property.Name);

				property
					.GetCustomAttribute<CategoryAttribute>()
					.Should()
					.NotBeNull("{0} needs a category in the options grid", property.Name);
			}
		}

		[Theory]
		[InlineData(typeof(General))]
		[InlineData(typeof(Options.Build))]
		public void EverySettingIsReadableAndWritable(Type model)
		{
			// The base option model persists a setting by reflecting over the public properties.
			// A property without a setter is silently never restored.
			SettingsOf(model)
				.Should()
				.OnlyContain(property => property.CanRead && property.CanWrite)
				.And.NotBeEmpty();
		}

		[Theory]
		[InlineData(typeof(General))]
		[InlineData(typeof(Options.Build))]
		public void EverySettingUsesATypeTheSettingsStoreCanRoundTrip(Type model)
		{
			// The settings store keeps primitives and strings. Anything else is serialized in a
			// way that survives neither an upgrade nor a settings export.
			SettingsOf(model)
				.Should()
				.OnlyContain(
					property =>
						property.PropertyType.IsPrimitive
						|| property.PropertyType.IsEnum
						|| property.PropertyType == typeof(string)
				);
		}

		[Theory]
		[InlineData(typeof(General))]
		[InlineData(typeof(Options.Build))]
		public void EveryModelIsPersistedThroughTheToolkitBaseClass(Type model)
		{
			model.BaseType.Should().NotBeNull();
			model.BaseType.IsGenericType.Should().BeTrue();
			model
				.BaseType.GetGenericTypeDefinition()
				.Should()
				.Be(
					typeof(BaseOptionModel<>),
					"{0} has to inherit the loading and saving of the toolkit",
					model.Name
				);
			model.BaseType.GetGenericArguments().Single().Should().Be(model);
		}

		private static PropertyInfo[] SettingsOf(Type model) =>
			model.GetProperties(
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
			);
	}
}
