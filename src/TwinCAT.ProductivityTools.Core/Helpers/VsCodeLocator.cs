using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Infrastructure;

namespace TwinCAT.ProductivityTools.Helpers
{
	/// <summary>
	/// Finds the Visual Studio Code executable on the local machine.
	/// </summary>
	public interface IVsCodeLocator
	{
		/// <summary>
		/// Returns the full path of <c>Code.exe</c>, or <c>null</c> when Visual Studio Code is not
		/// installed.
		/// </summary>
		string Locate();
	}

	/// <summary>
	/// Looks for Visual Studio Code in the shell integration registry key, on <c>PATH</c> and in the
	/// per user installation directory, in that order.
	/// </summary>
	public sealed class VsCodeLocator : IVsCodeLocator
	{
		public const string ShellRegistryKey = @"SOFTWARE\Classes\*\shell\VSCode";
		public const string ShellRegistryValue = "Icon";
		public const string UserInstallationSuffix = @"Programs\Microsoft VS Code";
		public const string ExecutableName = "Code.exe";

		private readonly IRegistryProvider registry;
		private readonly IFileSystemProbe fileSystem;
		private readonly Func<string, string> environmentVariable;

		public VsCodeLocator()
			: this(
				WindowsRegistryProvider.Instance,
				PhysicalFileSystemProbe.Instance,
				Environment.GetEnvironmentVariable
			) { }

		public VsCodeLocator(
			IRegistryProvider registry,
			IFileSystemProbe fileSystem,
			Func<string, string> environmentVariable
		)
		{
			this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
			this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
			this.environmentVariable =
				environmentVariable ?? throw new ArgumentNullException(nameof(environmentVariable));
		}

		public string Locate() => InRegistry() ?? InEnvironmentPath() ?? InLocalAppData();

		/// <summary>
		/// The "Open with Code" shell extension stores the executable it points at, which is the
		/// most reliable hint for a user installation.
		/// </summary>
		public string InRegistry()
		{
			string path =
				registry.GetValue(
					RegistryHive.CurrentUser,
					RegistryView.Default,
					ShellRegistryKey,
					ShellRegistryValue
				) as string;

			return Existing(path);
		}

		public string InEnvironmentPath()
		{
			string variable = environmentVariable("Path");

			if (string.IsNullOrEmpty(variable))
			{
				return null;
			}

			foreach (
				string entry in variable.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
			)
			{
				string directory = entry.Trim().Trim('"');

				if (directory.Length == 0)
				{
					continue;
				}

				// Visual Studio Code puts its "bin" directory on PATH, while Code.exe lives in the
				// parent directory next to it.
				string candidate =
					Existing(Combine(directory, ExecutableName))
					?? Existing(Combine(Parent(directory), ExecutableName));

				if (candidate != null)
				{
					return candidate;
				}
			}

			return null;
		}

		public string InLocalAppData()
		{
			string localAppData = environmentVariable("LOCALAPPDATA");

			return string.IsNullOrEmpty(localAppData)
				? null
				: Existing(Combine(localAppData, UserInstallationSuffix, ExecutableName));
		}

		private string Existing(string path) =>
			!string.IsNullOrEmpty(path) && fileSystem.FileExists(path) ? path : null;

		private static string Parent(string directory)
		{
			try
			{
				return Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
			}
			catch (ArgumentException)
			{
				return null;
			}
		}

		private static string Combine(params string[] parts)
		{
			if (parts.Any(string.IsNullOrEmpty))
			{
				return null;
			}

			try
			{
				return Path.Combine(parts);
			}
			catch (ArgumentException)
			{
				return null;
			}
		}
	}
}
