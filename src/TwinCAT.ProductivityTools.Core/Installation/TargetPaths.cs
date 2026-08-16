using System;
using System.IO;

namespace TwinCAT.ProductivityTools.Installation
{
	/// <summary>
	/// Well known paths on a TwinCAT target system.
	/// </summary>
	/// <remarks>
	/// The paths cannot be probed, because they live on the remote machine. They are derived from
	/// the TwinCAT version the target reports over ADS instead. TwinCAT 4026 moved the system
	/// directory out of <c>C:\TwinCAT</c> into the program files directory, so a hard coded
	/// <c>C:\TwinCAT\3.1\System</c> no longer exists on a current target.
	/// </remarks>
	public static class TargetPaths
	{
		public const string LegacySystemDirectory = @"C:\TwinCAT\3.1\System";

		public const string ReorganizedSystemDirectory =
			@"C:\Program Files (x86)\Beckhoff\TwinCAT\3.1\System";

		public const string SetTickScriptName = "win8settick.bat";

		/// <summary>
		/// System directory of the target. A missing or unparsable version falls back to the 4024
		/// layout, which is what the majority of the deployed targets still use.
		/// </summary>
		public static string SystemDirectory(Version twinCatVersion)
		{
			return IsReorganized(twinCatVersion)
				? ReorganizedSystemDirectory
				: LegacySystemDirectory;
		}

		public static string SetTickScript(Version twinCatVersion)
		{
			return Path.Combine(SystemDirectory(twinCatVersion), SetTickScriptName);
		}

		public static bool IsReorganized(Version twinCatVersion)
		{
			return twinCatVersion != null
				&& twinCatVersion.Build >= TwinCATInstallation.FirstReorganizedBuild;
		}
	}
}
