using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using NSubstitute;
using TwinCAT.ProductivityTools.Abstractions;

namespace TwinCAT.ProductivityTools.Tests
{
	/// <summary>
	/// In memory stand ins for the machine state the core library reads.
	/// </summary>
	internal static class Fake
	{
		public static IFileSystemProbe FileSystem(params string[] existingPaths)
		{
			var files = new HashSet<string>(existingPaths, System.StringComparer.OrdinalIgnoreCase);
			var probe = Substitute.For<IFileSystemProbe>();

			probe.FileExists(Arg.Any<string>()).Returns(call => files.Contains(call.Arg<string>()));

			probe
				.DirectoryExists(Arg.Any<string>())
				.Returns(call => Directories(files).Contains(call.Arg<string>()));

			probe
				.GetDirectories(Arg.Any<string>())
				.Returns(call => Children(Directories(files), call.Arg<string>()));

			return probe;
		}

		public static IRegistryProvider Registry(string twinCatDirectory = null, int? build = null)
		{
			var registry = Substitute.For<IRegistryProvider>();

			registry
				.GetValue(
					RegistryHive.LocalMachine,
					RegistryView.Registry32,
					"SOFTWARE\\Beckhoff\\TwinCAT3",
					"TwinCATDir"
				)
				.Returns(twinCatDirectory);

			registry
				.GetValue(
					RegistryHive.LocalMachine,
					RegistryView.Registry32,
					"SOFTWARE\\Beckhoff\\TwinCAT3\\System",
					"Build"
				)
				.Returns(build);

			return registry;
		}

		/// <summary>
		/// Derives every directory that has to exist for the given files, so that a test only needs
		/// to name the files it cares about.
		/// </summary>
		private static HashSet<string> Directories(IEnumerable<string> paths)
		{
			var directories = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

			foreach (string path in paths)
			{
				string current = path;

				while (true)
				{
					int separator = current.LastIndexOf('\\');

					if (separator <= 2)
					{
						break;
					}

					current = current.Substring(0, separator);
					directories.Add(current);
				}
			}

			return directories;
		}

		private static IEnumerable<string> Children(IEnumerable<string> directories, string parent)
		{
			string prefix = parent.TrimEnd('\\') + "\\";

			return directories
				.Where(
					directory =>
						directory.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)
						&& !directory.Substring(prefix.Length).Contains("\\")
				)
				.ToArray();
		}
	}
}
