using System.Collections.Generic;
using System.IO;

namespace TwinCAT.ProductivityTools.Helpers
{
	public class FileSystem
	{
		public virtual bool Exists(string path)
		{
			return File.Exists(path);
		}

		public void WriteAllText(string path, string contents)
		{
			File.WriteAllText(path, contents);
		}

		public string ReadAllText(string path)
		{
			return File.ReadAllText(path);
		}

		public virtual Stream OpenRead(string path)
		{
			return File.OpenRead(path);
		}

		public void Copy(string sourceFileName, string destFileName, bool overwrite)
		{
			File.Copy(sourceFileName, destFileName, overwrite);
		}

		public void DeleteFile(string path)
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}

		public void DeleteDirectory(string path, bool recursive = false)
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive);
			}
		}

		public (IEnumerable<string> Accepted, IEnumerable<string> Denied) GetFilesFiltered(
			string path,
			IEnumerable<string> filter
		)
		{
			if (!Directory.Exists(path))
			{
				throw new DirectoryNotFoundException();
			}

			return FileFilter.Parse(filter, path);
		}
	}
}
