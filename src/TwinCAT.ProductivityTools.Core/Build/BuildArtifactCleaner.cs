using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TwinCAT.ProductivityTools.Build
{
	/// <summary>
	/// Removes the build output of a TwinCAT project from disk.
	/// </summary>
	/// <remarks>
	/// A TwinCAT clean only drops the compiled artefacts the project system knows about. Boot
	/// folders, the generated <c>_Boot</c> and <c>_CompileInfo</c> directories and the PLC compile
	/// output stay behind and end up in a commit or in an archive of the project.
	/// </remarks>
	public sealed class BuildArtifactCleaner
	{
		private readonly Helpers.FileSystem fileSystem;

		public BuildArtifactCleaner()
			: this(new Helpers.FileSystem()) { }

		public BuildArtifactCleaner(Helpers.FileSystem fileSystem)
		{
			this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
		}

		/// <summary>
		/// Deletes everything below <paramref name="projectDirectory"/> that the artefact filter
		/// denies. A single entry that cannot be deleted - because it is open in another process,
		/// for instance - is counted as a failure and does not stop the remaining ones.
		/// </summary>
		public BuildArtifactCleanResult Clean(
			string projectDirectory,
			IEnumerable<string> filter = null
		)
		{
			if (string.IsNullOrWhiteSpace(projectDirectory))
			{
				return BuildArtifactCleanResult.Nothing;
			}

			if (!Directory.Exists(projectDirectory))
			{
				return BuildArtifactCleanResult.Nothing;
			}

			var denied = fileSystem
				.GetFilesFiltered(projectDirectory, filter ?? new Helpers.FileFilterProvider())
				.Denied;

			BuildArtifactPlan plan = BuildArtifactPlan.Create(projectDirectory, denied);

			int files = 0;
			int directories = 0;
			List<string> failed = new List<string>();

			foreach (string file in plan.Files)
			{
				if (!File.Exists(file))
				{
					continue;
				}

				try
				{
					fileSystem.DeleteFile(file);
					files++;
				}
				catch (Exception)
				{
					failed.Add(file);
				}
			}

			foreach (string directory in plan.Directories)
			{
				if (!Directory.Exists(directory))
				{
					continue;
				}

				try
				{
					fileSystem.DeleteDirectory(directory, true);
					directories++;
				}
				catch (Exception)
				{
					failed.Add(directory);
				}
			}

			return new BuildArtifactCleanResult(files, directories, failed);
		}
	}

	public sealed class BuildArtifactCleanResult
	{
		public static readonly BuildArtifactCleanResult Nothing = new BuildArtifactCleanResult(
			0,
			0,
			new string[0]
		);

		public BuildArtifactCleanResult(
			int deletedFiles,
			int deletedDirectories,
			IReadOnlyList<string> failedEntries
		)
		{
			DeletedFiles = deletedFiles;
			DeletedDirectories = deletedDirectories;
			FailedEntries = failedEntries ?? new string[0];
		}

		public int DeletedFiles { get; }

		public int DeletedDirectories { get; }

		public IReadOnlyList<string> FailedEntries { get; }

		public bool DeletedAnything => DeletedFiles > 0 || DeletedDirectories > 0;

		public override string ToString()
		{
			string summary =
				$"{DeletedFiles} file(s) and {DeletedDirectories} directory(ies) deleted";

			return FailedEntries.Any()
				? $"{summary}, {FailedEntries.Count} entry(ies) could not be deleted"
				: summary;
		}
	}
}
