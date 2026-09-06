using System;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Infrastructure;

namespace TwinCAT.ProductivityTools.Remote
{
	/// <summary>
	/// Opens a Windows remote desktop session with the client that ships with Windows.
	/// </summary>
	public sealed class RemoteDesktopConnection : ITargetConnection
	{
		public const string ExecutablePath = @"%SystemRoot%\System32\mstsc.exe";

		private readonly IFileSystemProbe fileSystem;
		private readonly Func<string, string> expandEnvironment;

		public RemoteDesktopConnection()
			: this(PhysicalFileSystemProbe.Instance, Environment.ExpandEnvironmentVariables) { }

		public RemoteDesktopConnection(
			IFileSystemProbe fileSystem,
			Func<string, string> expandEnvironment
		)
		{
			this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
			this.expandEnvironment =
				expandEnvironment ?? throw new ArgumentNullException(nameof(expandEnvironment));
		}

		public string DisplayName => "Remote Desktop";

		public string UnavailableMessage =>
			"The Windows remote desktop client (mstsc.exe) was not found on this machine.";

		/// <remarks>
		/// <c>/v:</c> is the documented form of the switch. The client is a windowed application,
		/// so it needs no shell and no console.
		/// </remarks>
		public ProcessLaunch Build(string address)
		{
			string host = TargetAddress.Normalize(address);

			if (host == null)
			{
				return null;
			}

			string executable = expandEnvironment(ExecutablePath);

			return fileSystem.FileExists(executable)
				? new ProcessLaunch(executable, "/v:" + host, false)
				: null;
		}
	}
}
