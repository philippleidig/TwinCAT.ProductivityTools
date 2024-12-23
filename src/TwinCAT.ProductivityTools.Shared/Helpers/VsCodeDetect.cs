using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace TwinCAT.ProductivityTools.Helpers
{
	internal static class VsCodeDetect
	{
		internal static string InRegistry()
		{
			var key = Registry.CurrentUser;
			var name = "Icon";
			try
			{
				var subKey = key.OpenSubKey(@"SOFTWARE\Classes\*\shell\VSCode\");
				var value = subKey.GetValue(name).ToString();
				if (File.Exists(value))
				{
					return value;
				}

				return null;
			}
			catch
			{
				return null;
			}
		}

		internal static string InLocalAppData()
		{
			var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");

			var codePartDir = @"Programs\Microsoft VS Code";
			var codeDir = Path.Combine(localAppData, codePartDir);
			var drives = DriveInfo.GetDrives();

			foreach (var drive in drives)
			{
				if (drive.DriveType == DriveType.Fixed)
				{
					var path = Path.Combine(drive.Name[0] + codeDir.Substring(1), "code.exe");
					if (File.Exists(path))
					{
						return path;
					}
				}
			}

			return null;
		}

		internal static string InEnvVarPath()
		{
			var envPath = Environment.GetEnvironmentVariable("Path");
			var paths = envPath.Split(';');
			var parentDir = "Microsoft VS Code";
			foreach (var path in paths)
			{
				if (path.ToLower().Contains("code"))
				{
					var temp = Path.Combine(
						path.Substring(
							0,
							path.IndexOf(parentDir, StringComparison.InvariantCultureIgnoreCase)
						),
						parentDir,
						"code.exe"
					);
					if (File.Exists(temp))
					{
						return temp;
					}
				}
			}
			return null;
		}
	}
}
