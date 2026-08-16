using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Infrastructure;

namespace TwinCAT.ProductivityTools.Installation
{
	/// <summary>
	/// Describes where the local TwinCAT installation keeps the files this extension works with.
	/// </summary>
	public interface ITwinCATInstallation
	{
		/// <summary>Major build of the installation, for example 4024 or 4026. 0 when TwinCAT is absent.</summary>
		int Build { get; }

		bool IsInstalled { get; }

		/// <summary>Installation root, as reported by the registry (includes a trailing separator on 4026).</summary>
		string InstallationDirectory { get; }

		/// <summary>
		/// Full path of <c>StaticRoutes.xml</c>, or <c>null</c> when it cannot be located.
		/// </summary>
		string StaticRoutesPath { get; }

		/// <summary>
		/// Directory that holds the PLC project templates, or <c>null</c> when it cannot be located.
		/// </summary>
		string PlcTemplatesDirectory { get; }
	}

	/// <summary>
	/// Resolves TwinCAT paths from the registry and the file system.
	/// </summary>
	/// <remarks>
	/// TwinCAT 4026 reorganized the installation. <c>C:\TwinCAT\3.1</c> no longer exists: binaries
	/// moved below <c>%ProgramFiles(x86)%\Beckhoff\TwinCAT</c>, the routing configuration became
	/// per runtime below <c>%ProgramData%\Beckhoff\TwinCAT\3.1\Runtimes\&lt;runtime&gt;</c>, and the
	/// PLC templates moved to <c>%ProgramData%\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates</c>.
	/// Both layouts stay supported because 4024 is still widely deployed.
	/// </remarks>
	public sealed class TwinCATInstallation : ITwinCATInstallation
	{
		public const string RegistrySubKey = @"SOFTWARE\Beckhoff\TwinCAT3";
		public const string SystemRegistrySubKey = @"SOFTWARE\Beckhoff\TwinCAT3\System";

		/// <summary>First build that uses the reorganized 4026 directory layout.</summary>
		public const int FirstReorganizedBuild = 4026;

		private readonly IRegistryProvider registry;
		private readonly IFileSystemProbe fileSystem;
		private readonly string programData;

		public TwinCATInstallation()
			: this(
				WindowsRegistryProvider.Instance,
				PhysicalFileSystemProbe.Instance,
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
			) { }

		public TwinCATInstallation(
			IRegistryProvider registry,
			IFileSystemProbe fileSystem,
			string programDataDirectory
		)
		{
			this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
			this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
			this.programData = programDataDirectory ?? string.Empty;
		}

		public int Build
		{
			get
			{
				object value = registry.GetValue(
					RegistryHive.LocalMachine,
					RegistryView.Registry32,
					SystemRegistrySubKey,
					"Build"
				);

				return value == null ? 0 : Convert.ToInt32(value);
			}
		}

		public bool IsInstalled => Build > 0;

		public string InstallationDirectory =>
			registry.GetValue(
				RegistryHive.LocalMachine,
				RegistryView.Registry32,
				RegistrySubKey,
				"TwinCATDir"
			) as string;

		public string StaticRoutesPath
		{
			get
			{
				if (Build >= FirstReorganizedBuild)
				{
					return ResolveRuntimeStaticRoutes();
				}

				string root = InstallationDirectory;

				if (string.IsNullOrEmpty(root))
				{
					return null;
				}

				string path = Path.Combine(root, @"3.1\Target\StaticRoutes.xml");

				return fileSystem.FileExists(path) ? path : null;
			}
		}

		public string PlcTemplatesDirectory
		{
			get
			{
				if (Build >= FirstReorganizedBuild)
				{
					return HighestVersionDirectory(
						Path.Combine(programData, @"Beckhoff\TwinCAT\PlcEngineering\PlcTemplates")
					);
				}

				string root = InstallationDirectory;

				if (string.IsNullOrEmpty(root))
				{
					return null;
				}

				string versioned = HighestVersionDirectory(
					Path.Combine(root, @"3.1\Components\Plc\PlcTemplates")
				);

				if (versioned == null)
				{
					return null;
				}

				string legacy = Path.Combine(versioned, "Plc Templates");

				return fileSystem.DirectoryExists(legacy) ? legacy : versioned;
			}
		}

		/// <summary>
		/// 4026 keeps one routing configuration per runtime. The default runtime is preferred and
		/// any other runtime that actually carries a <c>StaticRoutes.xml</c> is used as a fallback.
		/// </summary>
		private string ResolveRuntimeStaticRoutes()
		{
			string runtimes = Path.Combine(programData, @"Beckhoff\TwinCAT\3.1\Runtimes");

			IEnumerable<string> candidates = fileSystem
				.GetDirectories(runtimes)
				.OrderByDescending(
					directory =>
						string.Equals(
							Path.GetFileName(directory),
							"UmRT_Default",
							StringComparison.OrdinalIgnoreCase
						)
				)
				.ThenBy(directory => directory, StringComparer.OrdinalIgnoreCase);

			foreach (string runtime in candidates)
			{
				string path = Path.Combine(runtime, @"3.1\Target\StaticRoutes.xml");

				if (fileSystem.FileExists(path))
				{
					return path;
				}
			}

			return null;
		}

		/// <summary>
		/// Returns the subdirectory with the highest version-like name, so that a newly installed
		/// template package is picked up without changing this code.
		/// </summary>
		private string HighestVersionDirectory(string root)
		{
			string best = null;
			Version bestVersion = null;

			foreach (string directory in fileSystem.GetDirectories(root))
			{
				if (!Version.TryParse(Path.GetFileName(directory), out Version version))
				{
					continue;
				}

				if (bestVersion == null || version > bestVersion)
				{
					bestVersion = version;
					best = directory;
				}
			}

			return best;
		}
	}
}
