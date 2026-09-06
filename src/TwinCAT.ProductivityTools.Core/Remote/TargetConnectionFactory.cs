using System;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Infrastructure;

namespace TwinCAT.ProductivityTools.Remote
{
	/// <summary>
	/// Picks the session that fits the operating system of a target.
	/// </summary>
	public sealed class TargetConnectionFactory
	{
		private readonly IFileSystemProbe fileSystem;
		private readonly Func<string, string> expandEnvironment;
		private readonly ISshClientLocator sshLocator;

		public TargetConnectionFactory()
			: this(
				PhysicalFileSystemProbe.Instance,
				Environment.ExpandEnvironmentVariables,
				new SshClientLocator()
			) { }

		public TargetConnectionFactory(
			IFileSystemProbe fileSystem,
			Func<string, string> expandEnvironment,
			ISshClientLocator sshLocator
		)
		{
			this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
			this.expandEnvironment =
				expandEnvironment ?? throw new ArgumentNullException(nameof(expandEnvironment));
			this.sshLocator = sshLocator ?? throw new ArgumentNullException(nameof(sshLocator));
		}

		/// <summary>
		/// Returns the session for an operating system, or <c>null</c> when it offers none this
		/// extension can open.
		/// </summary>
		/// <remarks>
		/// A Windows CE image serves neither remote desktop nor SSH; its remote configuration is
		/// the Device Manager. An unknown operating system never reaches here - the caller asks
		/// the user before it does.
		/// </remarks>
		/// <exception cref="ArgumentOutOfRangeException">
		/// The operating system is <see cref="TargetOperatingSystem.Unknown"/>.
		/// </exception>
		public ITargetConnection For(TargetOperatingSystem operatingSystem, string sshUserName)
		{
			switch (operatingSystem)
			{
				case TargetOperatingSystem.Windows:
					return new RemoteDesktopConnection(fileSystem, expandEnvironment);

				case TargetOperatingSystem.Linux:
				case TargetOperatingSystem.Bsd:
					return new SecureShellConnection(sshLocator, sshUserName);

				case TargetOperatingSystem.WindowsCe:
					return null;

				default:
					throw new ArgumentOutOfRangeException(
						nameof(operatingSystem),
						"The operating system of the target has to be known before a session can "
							+ "be opened."
					);
			}
		}
	}
}
