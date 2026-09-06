using System;
using System.Linq;
using TwinCAT.ProductivityTools.Helpers;

namespace TwinCAT.ProductivityTools.Remote
{
	/// <summary>
	/// Opens a terminal session on a TwinCAT/BSD or TwinCAT/Linux target with the OpenSSH client
	/// that ships with Windows.
	/// </summary>
	public sealed class SecureShellConnection : ITargetConnection
	{
		/// <summary>The account every Beckhoff image ships.</summary>
		public const string DefaultUserName = "Administrator";

		private readonly ISshClientLocator locator;
		private readonly string userName;

		public SecureShellConnection(string userName)
			: this(new SshClientLocator(), userName) { }

		public SecureShellConnection(ISshClientLocator locator, string userName)
		{
			this.locator = locator ?? throw new ArgumentNullException(nameof(locator));
			this.userName = userName;
		}

		public string DisplayName => "SSH";

		/// <remarks>
		/// Two things can stop the session, and they need different answers from the user.
		/// </remarks>
		public string UnavailableMessage =>
			UsableUserName(userName) == null
				? $"\"{userName}\" cannot be used as an SSH user name. Change it under Tools > "
					+ "Options > TwinCAT 3.1 ProductivityTools > General."
				: "The OpenSSH client (ssh.exe) was not found. Install it under Settings > Apps > "
					+ "Optional features > OpenSSH Client, or put ssh.exe on PATH.";

		/// <remarks>
		/// The session is interactive, and Visual Studio has no console to lend to a console
		/// application, so the shell has to start it - that is what opens the console window the
		/// user types into.
		/// </remarks>
		public ProcessLaunch Build(string address)
		{
			string host = TargetAddress.Normalize(address);

			if (host == null)
			{
				return null;
			}

			string user = UsableUserName(userName);

			if (user == null)
			{
				return null;
			}

			string executable = locator.Locate();

			return executable == null
				? null
				: new ProcessLaunch(executable, user + "@" + host, true);
		}

		/// <summary>
		/// Returns the user name to connect with, or <c>null</c> when the configured one cannot be
		/// put on a command line.
		/// </summary>
		/// <remarks>
		/// The name comes from the options page, so it is input. A name that carries a space or a
		/// leading dash would turn into a second argument of the client.
		/// </remarks>
		public static string UsableUserName(string userName)
		{
			if (string.IsNullOrWhiteSpace(userName))
			{
				return DefaultUserName;
			}

			string trimmed = userName.Trim();

			bool usable =
				trimmed[0] != '-'
				&& !trimmed.Any(character => char.IsWhiteSpace(character) || character == '@');

			return usable ? trimmed : null;
		}
	}
}
