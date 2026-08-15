using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace TwinCAT.ProductivityTools.E2E.Tests.Infrastructure
{
	/// <summary>
	/// A fact that runs only where a real IDE with a real TwinCAT installation exists.
	/// </summary>
	/// <remarks>
	/// Marking the tests with the <c>Category=E2E</c> trait keeps them out of CI. Skipping instead
	/// of failing on a machine that misses a prerequisite keeps a plain <c>dotnet test</c> over the
	/// whole repository green for a contributor who only has the SDK installed, while still telling
	/// them exactly what is missing.
	/// </remarks>
	[TraitDiscoverer(E2ETraitDiscoverer.TypeName, E2ETraitDiscoverer.AssemblyName)]
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class E2EFactAttribute : FactAttribute, ITraitAttribute
	{
		/// <summary>
		/// Marks a test that has to create a PLC project through the automation interface.
		/// </summary>
		/// <remarks>
		/// Inserting a PLC project makes the XAE shell hand over to the PLC engineering, which is a
		/// separate product with its own version and licence state. Where that hand over does not
		/// work the failure looks like a defect of this extension although nothing of it ran, so
		/// these tests are opt in rather than a permanent red.
		/// </remarks>
		public bool RequiresPlcEngineering { get; set; }

		public override string Skip
		{
			get => base.Skip ?? MissingReason();
			set => base.Skip = value;
		}

		private string MissingReason()
		{
			string missing = E2EPrerequisites.MissingReason();

			if (missing != null)
			{
				return missing;
			}

			if (RequiresPlcEngineering && !E2EPrerequisites.PlcEngineeringEnabled)
			{
				return "Creating PLC projects through the automation interface is opt in. Set "
					+ "TCPT_E2E_PLC=1 once the PLC engineering of this machine is known to accept "
					+ "it.";
			}

			return null;
		}
	}

	/// <summary>
	/// Answers whether an end to end run is possible at all, and why not.
	/// </summary>
	public static class E2EPrerequisites
	{
		private static readonly Lazy<string> Reason = new Lazy<string>(Evaluate);

		/// <summary>
		/// Returns <c>null</c> when everything is in place, otherwise a sentence naming what is
		/// missing.
		/// </summary>
		public static string MissingReason() => Reason.Value;

		/// <summary>
		/// The IDE the tests drive, or <c>null</c> when the prerequisites are not met.
		/// </summary>
		public static IdeUnderTest Ide { get; private set; }

		/// <summary>
		/// Whether the machine is declared to support creating PLC projects through automation.
		/// </summary>
		public static bool PlcEngineeringEnabled =>
			IsTruthy(Environment.GetEnvironmentVariable("TCPT_E2E_PLC"));

		private static bool IsTruthy(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}

			value = value.Trim();

			return value.Equals("1", StringComparison.OrdinalIgnoreCase)
				|| value.Equals("true", StringComparison.OrdinalIgnoreCase)
				|| value.Equals("yes", StringComparison.OrdinalIgnoreCase);
		}

		private static string Evaluate()
		{
			if (!IsTwinCatInstalled())
			{
				return "TwinCAT XAE is not installed on this machine.";
			}

			string reason;

			IdeUnderTest ide = IdeUnderTest.TryLocate(IdeUnderTest.Selected(), out reason);

			if (ide == null)
			{
				return reason;
			}

			if (!IsExtensionInstalled(ide))
			{
				return $"TwinCAT.ProductivityTools is not deployed in {ide.Kind}. Build the "
					+ "solution and install the VSIX, or install the TcPkg workload, then run "
					+ "these tests again.";
			}

			Ide = ide;

			return null;
		}

		/// <summary>
		/// Looks for the extension assembly in the places a VSIX for this particular IDE can end
		/// up in.
		/// </summary>
		/// <remarks>
		/// An installation for all users, which is what the installer and the TcPkg package do,
		/// lands under the IDE itself. A developer deploying from Visual Studio gets a per user
		/// installation instead. Both have to count, otherwise the tests would refuse to run in
		/// exactly the situation they are written for. The per user folder has to be narrowed to
		/// the instance under test, because every IDE on the machine keeps its own and finding the
		/// extension in a different one would be a false positive.
		/// </remarks>
		private static bool IsExtensionInstalled(IdeUnderTest ide)
		{
			var roots = new List<string>
			{
				Path.Combine(ide.InstallationPath, @"Common7\IDE\Extensions"),
			};

			roots.AddRange(PerUserExtensionRoots(ide));

			return roots.Where(Directory.Exists).Any(ContainsExtension);
		}

		private static IEnumerable<string> PerUserExtensionRoots(IdeUnderTest ide)
		{
			string local = Environment.GetFolderPath(
				Environment.SpecialFolder.LocalApplicationData
			);

			foreach (string vendor in new[] { @"Microsoft\VisualStudio", @"Beckhoff\TcXaeShell" })
			{
				string root = Path.Combine(local, vendor);

				if (!Directory.Exists(root))
				{
					continue;
				}

				foreach (string instance in Directory.EnumerateDirectories(root))
				{
					if (
						Path.GetFileName(instance)
							.StartsWith(ShellVersionOf(ide.Kind), StringComparison.Ordinal)
					)
					{
						yield return Path.Combine(instance, "Extensions");
					}
				}
			}
		}

		/// <summary>
		/// Version prefix the per user folder of an IDE starts with. The 64 bit shell is built on
		/// the Visual Studio 2022 shell and therefore also reports 17.0.
		/// </summary>
		private static string ShellVersionOf(IdeKind kind)
		{
			switch (kind)
			{
				case IdeKind.TcXaeShell:
					return "15.0_";
				case IdeKind.TcXaeShell64:
				case IdeKind.VS2022:
					return "17.0_";
				case IdeKind.VS2026:
					return "18.0_";
				default:
					throw new ArgumentOutOfRangeException(nameof(kind));
			}
		}

		private static bool ContainsExtension(string root)
		{
			try
			{
				return Directory
					.EnumerateFiles(
						root,
						"TwinCAT.ProductivityTools.*.dll",
						SearchOption.AllDirectories
					)
					.Any();
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}
			catch (IOException)
			{
				return false;
			}
		}

		private static bool IsTwinCatInstalled()
		{
			// The automation interface is what the tests really need. Its type library is
			// registered by the XAE installation, so asking COM is both cheap and accurate.
			Type systemManager = Type.GetTypeFromProgID("TcXaeShell.DTE.17.0", throwOnError: false);

			if (systemManager == null)
			{
				systemManager = Type.GetTypeFromProgID("TcXaeShell.DTE.15.0", throwOnError: false);
			}

			return systemManager != null
				|| Directory.Exists(@"C:\TwinCAT\3.1")
				|| Directory.Exists(@"C:\ProgramData\Beckhoff\TwinCAT");
		}
	}
}
