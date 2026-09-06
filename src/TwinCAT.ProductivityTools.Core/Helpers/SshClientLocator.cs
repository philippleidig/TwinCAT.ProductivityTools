using System;
using System.IO;
using System.Linq;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Infrastructure;

namespace TwinCAT.ProductivityTools.Helpers
{
	/// <summary>
	/// Finds the OpenSSH client on the local machine.
	/// </summary>
	public interface ISshClientLocator
	{
		/// <summary>
		/// Returns the full path of <c>ssh.exe</c>, or <c>null</c> when the OpenSSH client is not
		/// installed.
		/// </summary>
		string Locate();
	}

	/// <summary>
	/// Looks for the client that ships with Windows first and only then on <c>PATH</c>.
	/// </summary>
	/// <remarks>
	/// The in box client is preferred because a client from Git for Windows or from Cygwin behaves
	/// differently around the console, and <c>PATH</c> is the way to pick one of those on purpose.
	/// </remarks>
	public sealed class SshClientLocator : ISshClientLocator
	{
		public const string ExecutableName = "ssh.exe";

		/// <summary>Where Windows keeps the OpenSSH client.</summary>
		public const string SystemDirectory = @"%SystemRoot%\System32\OpenSSH";

		/// <summary>
		/// The same directory, reached from a 32 bit process.
		/// </summary>
		/// <remarks>
		/// Visual Studio 2017 and 2019 are 32 bit processes, for which Windows redirects
		/// <c>System32</c> to <c>SysWOW64</c> - and there is no OpenSSH below that. The
		/// <c>Sysnative</c> alias reaches the real <c>System32</c>. It does not exist in a 64 bit
		/// process, where the first probe already succeeds, so both are simply tried in order.
		/// </remarks>
		public const string NativeSystemDirectory = @"%SystemRoot%\Sysnative\OpenSSH";

		private readonly IFileSystemProbe fileSystem;
		private readonly Func<string, string> expandEnvironment;
		private readonly Func<string, string> environmentVariable;

		public SshClientLocator()
			: this(
				PhysicalFileSystemProbe.Instance,
				Environment.ExpandEnvironmentVariables,
				Environment.GetEnvironmentVariable
			) { }

		public SshClientLocator(
			IFileSystemProbe fileSystem,
			Func<string, string> expandEnvironment,
			Func<string, string> environmentVariable
		)
		{
			this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
			this.expandEnvironment =
				expandEnvironment ?? throw new ArgumentNullException(nameof(expandEnvironment));
			this.environmentVariable =
				environmentVariable ?? throw new ArgumentNullException(nameof(environmentVariable));
		}

		public string Locate() =>
			InSystemDirectory() ?? InNativeSystemDirectory() ?? InEnvironmentPath();

		public string InSystemDirectory() => InDirectory(SystemDirectory);

		public string InNativeSystemDirectory() => InDirectory(NativeSystemDirectory);

		public string InEnvironmentPath()
		{
			string variable = environmentVariable("Path");

			if (string.IsNullOrEmpty(variable))
			{
				return null;
			}

			foreach (
				string entry in variable.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
			)
			{
				string directory = entry.Trim().Trim('"');

				if (directory.Length == 0)
				{
					continue;
				}

				string candidate = Existing(Combine(directory, ExecutableName));

				if (candidate != null)
				{
					return candidate;
				}
			}

			return null;
		}

		private string InDirectory(string directory)
		{
			string expanded = expandEnvironment(directory);

			return Existing(Combine(expanded, ExecutableName));
		}

		private string Existing(string path) =>
			!string.IsNullOrEmpty(path) && fileSystem.FileExists(path) ? path : null;

		private static string Combine(params string[] parts)
		{
			if (parts.Any(string.IsNullOrEmpty))
			{
				return null;
			}

			try
			{
				return Path.Combine(parts);
			}
			catch (ArgumentException)
			{
				return null;
			}
		}
	}
}
