using System.Collections.Generic;

namespace TwinCAT.ProductivityTools.Abstractions
{
	/// <summary>
	/// The subset of the file system that the core logic needs, so that path resolution and
	/// discovery can be verified without a TwinCAT installation on disk.
	/// </summary>
	public interface IFileSystemProbe
	{
		bool FileExists(string path);

		bool DirectoryExists(string path);

		/// <summary>
		/// Returns the immediate subdirectories of <paramref name="path"/>, or an empty sequence
		/// when the directory does not exist.
		/// </summary>
		IEnumerable<string> GetDirectories(string path);
	}
}
