using System;
using System.IO;

namespace TwinCAT.ProductivityTools.E2E.Tests.Infrastructure
{
	/// <summary>
	/// Finds files of the checked out repository from inside a test run.
	/// </summary>
	/// <remarks>
	/// The tests compare what a running IDE registered against what the sources declare, so they
	/// need the source tree rather than the build output. Walking up to the folder that contains
	/// <c>.git</c> works for a normal clone as well as for a worktree, where <c>.git</c> is a file.
	/// </remarks>
	public static class RepositoryPaths
	{
		private static readonly Lazy<string> RootFolder = new Lazy<string>(FindRoot);

		public static string Root => RootFolder.Value;

		public static string CommandTable =>
			Path.Combine(Root, "src", "SharedFiles", "Commands.vsct");

		/// <summary>Build output of the 64 bit VSIX project.</summary>
		public static string VsixOutput(string configuration) =>
			Path.Combine(Root, "src", "TwinCAT.ProductivityTools.17", "bin", configuration);

		private static string FindRoot()
		{
			var directory = new DirectoryInfo(
				Path.GetDirectoryName(new Uri(typeof(RepositoryPaths).Assembly.CodeBase).LocalPath)
			);

			while (directory != null)
			{
				string marker = Path.Combine(directory.FullName, ".git");

				if (Directory.Exists(marker) || File.Exists(marker))
				{
					return directory.FullName;
				}

				directory = directory.Parent;
			}

			throw new InvalidOperationException(
				"The repository root was not found above the test assembly."
			);
		}
	}
}
