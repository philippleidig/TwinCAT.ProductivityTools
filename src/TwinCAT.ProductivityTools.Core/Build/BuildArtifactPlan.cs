using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TwinCAT.ProductivityTools.Build
{
	/// <summary>
	/// The files and directories that have to disappear so that only the sources of a TwinCAT
	/// project are left behind.
	/// </summary>
	public sealed class BuildArtifactPlan
	{
		public static readonly BuildArtifactPlan Empty = new BuildArtifactPlan(
			new string[0],
			new string[0]
		);

		private BuildArtifactPlan(IReadOnlyList<string> files, IReadOnlyList<string> directories)
		{
			Files = files;
			Directories = directories;
		}

		public IReadOnlyList<string> Files { get; }

		public IReadOnlyList<string> Directories { get; }

		public bool IsEmpty => Files.Count == 0 && Directories.Count == 0;

		/// <summary>
		/// Turns the entries a <see cref="Helpers.FileFilter"/> denied into absolute paths.
		/// An entry that ends with a slash is a directory, everything else is a file. The
		/// directories are ordered from the deepest to the shallowest one so that a nested
		/// directory is still there when it is deleted.
		/// </summary>
		public static BuildArtifactPlan Create(
			string rootDirectory,
			IEnumerable<string> deniedEntries
		)
		{
			if (string.IsNullOrWhiteSpace(rootDirectory))
			{
				throw new ArgumentException(
					"The root directory is required.",
					nameof(rootDirectory)
				);
			}

			if (deniedEntries == null)
			{
				return Empty;
			}

			List<string> files = new List<string>();
			List<string> directories = new List<string>();

			foreach (string entry in deniedEntries)
			{
				if (string.IsNullOrWhiteSpace(entry))
				{
					continue;
				}

				bool isDirectory = entry.EndsWith("/") || entry.EndsWith("\\");
				string relative = entry
					.Replace('/', Path.DirectorySeparatorChar)
					.Trim(Path.DirectorySeparatorChar);

				if (relative.Length == 0)
				{
					// The filter reports the root itself as an entry. Cleaning it would delete
					// the whole project.
					continue;
				}

				string absolute = Path.Combine(rootDirectory, relative);

				if (isDirectory)
				{
					directories.Add(absolute);
				}
				else
				{
					files.Add(absolute);
				}
			}

			return new BuildArtifactPlan(
				files.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(f => f).ToList(),
				directories
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.OrderByDescending(d => d.Count(c => c == Path.DirectorySeparatorChar))
					.ThenBy(d => d)
					.ToList()
			);
		}
	}
}
