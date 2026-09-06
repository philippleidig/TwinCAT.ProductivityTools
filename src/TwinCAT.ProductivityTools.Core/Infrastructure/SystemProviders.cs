using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Remote;

namespace TwinCAT.ProductivityTools.Infrastructure
{
	/// <inheritdoc cref="IRegistryProvider"/>
	public sealed class WindowsRegistryProvider : IRegistryProvider
	{
		public static readonly WindowsRegistryProvider Instance = new WindowsRegistryProvider();

		public object GetValue(
			RegistryHive hive,
			RegistryView view,
			string subKey,
			string valueName
		)
		{
			try
			{
				using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view))
				using (RegistryKey key = baseKey?.OpenSubKey(subKey))
				{
					return key?.GetValue(valueName);
				}
			}
			catch (Exception)
			{
				return null;
			}
		}
	}

	/// <inheritdoc cref="IFileSystemProbe"/>
	public sealed class PhysicalFileSystemProbe : IFileSystemProbe
	{
		public static readonly PhysicalFileSystemProbe Instance = new PhysicalFileSystemProbe();

		public bool FileExists(string path) => !string.IsNullOrEmpty(path) && File.Exists(path);

		public bool DirectoryExists(string path) =>
			!string.IsNullOrEmpty(path) && Directory.Exists(path);

		public IEnumerable<string> GetDirectories(string path)
		{
			if (!DirectoryExists(path))
			{
				return Enumerable.Empty<string>();
			}

			try
			{
				return Directory.GetDirectories(path);
			}
			catch (Exception)
			{
				return Enumerable.Empty<string>();
			}
		}
	}

	/// <inheritdoc cref="IProcessLauncher"/>
	public sealed class SystemProcessLauncher : IProcessLauncher
	{
		public static readonly SystemProcessLauncher Instance = new SystemProcessLauncher();

		public void Start(ProcessLaunch launch)
		{
			if (launch == null)
			{
				throw new ArgumentNullException(nameof(launch));
			}

			ProcessStartInfo start = new ProcessStartInfo
			{
				FileName = launch.FileName,
				Arguments = launch.Arguments,
				UseShellExecute = launch.UseShellExecute,
			};

			// Only the handle of the started process is disposed here. The process itself outlives
			// the extension - that is the point of starting it.
			using (Process.Start(start)) { }
		}
	}
}
