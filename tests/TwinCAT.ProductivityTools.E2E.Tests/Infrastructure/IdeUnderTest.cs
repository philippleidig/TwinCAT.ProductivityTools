using System;
using System.IO;
using Microsoft.Win32;

namespace TwinCAT.ProductivityTools.E2E.Tests.Infrastructure
{
	/// <summary>
	/// The IDE an end to end run drives.
	/// </summary>
	public enum IdeKind
	{
		TcXaeShell64,
		TcXaeShell,
		VS2022,
		VS2026,
	}

	/// <summary>
	/// Locates the IDE the tests should drive and describes why it cannot be driven.
	/// </summary>
	/// <remarks>
	/// The resolution mirrors <c>tcpkg/_shared/IdeIntegration.psm1</c> so that the tests find
	/// exactly the installation the package would have deployed into. Visual Studio is found
	/// through <c>vswhere</c>, the shells through the registry keys Beckhoff writes.
	/// </remarks>
	public sealed class IdeUnderTest
	{
		/// <summary>Name of the environment variable the workflow sets.</summary>
		public const string SelectionVariable = "TCPT_E2E_IDE";

		private IdeUnderTest(
			IdeKind kind,
			string progId,
			string installationPath,
			string executable
		)
		{
			Kind = kind;
			ProgId = progId;
			InstallationPath = installationPath;
			Executable = executable;
		}

		public IdeKind Kind { get; }

		/// <summary>COM identifier the automation object is created from.</summary>
		public string ProgId { get; }

		public string InstallationPath { get; }

		public string Executable { get; }

		/// <summary>
		/// Reads <c>TCPT_E2E_IDE</c> and falls back to the 64 bit shell, which is the installation
		/// a TwinCAT 4026 engineering machine always has.
		/// </summary>
		public static IdeKind Selected()
		{
			string requested = Environment.GetEnvironmentVariable(SelectionVariable);

			if (string.IsNullOrWhiteSpace(requested))
			{
				return IdeKind.TcXaeShell64;
			}

			IdeKind kind;

			if (!Enum.TryParse(requested.Trim(), true, out kind))
			{
				throw new InvalidOperationException(
					$"'{requested}' is not a known IDE. Set {SelectionVariable} to one of "
						+ string.Join(", ", Enum.GetNames(typeof(IdeKind)))
						+ "."
				);
			}

			return kind;
		}

		/// <summary>
		/// Returns the installation, or <c>null</c> together with the reason when it is missing.
		/// </summary>
		public static IdeUnderTest TryLocate(IdeKind kind, out string unavailableReason)
		{
			unavailableReason = null;

			string progId = ProgIdOf(kind);

			if (!IsRegistered(progId))
			{
				unavailableReason = $"{kind} is not installed ({progId} is not registered).";
				return null;
			}

			string installation = InstallationOf(kind);

			if (string.IsNullOrEmpty(installation) || !Directory.Exists(installation))
			{
				unavailableReason = $"{kind} is registered but its installation folder is gone.";
				return null;
			}

			string executable = Path.Combine(
				installation,
				"Common7",
				"IDE",
				IsShell(kind) ? "TcXaeShell.exe" : "devenv.exe"
			);

			if (!File.Exists(executable))
			{
				unavailableReason = $"{kind} is registered but {executable} does not exist.";
				return null;
			}

			return new IdeUnderTest(kind, progId, installation, executable);
		}

		private static bool IsShell(IdeKind kind) =>
			kind == IdeKind.TcXaeShell || kind == IdeKind.TcXaeShell64;

		private static string ProgIdOf(IdeKind kind)
		{
			switch (kind)
			{
				case IdeKind.TcXaeShell:
					return "TcXaeShell.DTE.15.0";
				case IdeKind.TcXaeShell64:
					return "TcXaeShell.DTE.17.0";
				case IdeKind.VS2022:
					return "VisualStudio.DTE.17.0";
				case IdeKind.VS2026:
					return "VisualStudio.DTE.18.0";
				default:
					throw new ArgumentOutOfRangeException(nameof(kind));
			}
		}

		private static bool IsRegistered(string progId)
		{
			using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(progId))
			{
				return key != null;
			}
		}

		private static string InstallationOf(IdeKind kind)
		{
			switch (kind)
			{
				case IdeKind.TcXaeShell:
					return ShellInstallation(RegistryView.Registry32, "15.0");
				case IdeKind.TcXaeShell64:
					return ShellInstallation(RegistryView.Registry64, "17.0");
				case IdeKind.VS2022:
					return VisualStudioInstallation("[17.0,18.0)");
				case IdeKind.VS2026:
					return VisualStudioInstallation("[18.0,19.0)");
				default:
					throw new ArgumentOutOfRangeException(nameof(kind));
			}
		}

		private static string ShellInstallation(RegistryView view, string version)
		{
			using (RegistryKey root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
			using (RegistryKey key = root.OpenSubKey($@"SOFTWARE\Beckhoff\TcXaeShell\{version}"))
			{
				return key?.GetValue("InstallDir") as string;
			}
		}

		private static string VisualStudioInstallation(string versionRange)
		{
			string vswhere = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
				"Microsoft Visual Studio",
				"Installer",
				"vswhere.exe"
			);

			if (!File.Exists(vswhere))
			{
				return null;
			}

			using (var process = new System.Diagnostics.Process())
			{
				process.StartInfo.FileName = vswhere;
				process.StartInfo.Arguments =
					$"-latest -prerelease -products * -version \"{versionRange}\" -property installationPath";
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = true;

				process.Start();

				string output = process.StandardOutput.ReadToEnd();

				process.WaitForExit(30000);

				return output.Trim().Split('\n')[0].Trim();
			}
		}
	}
}
