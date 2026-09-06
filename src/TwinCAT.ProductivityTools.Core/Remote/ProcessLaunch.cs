using System;

namespace TwinCAT.ProductivityTools.Remote
{
	/// <summary>
	/// The process that opens a session on a target, described but not started.
	/// </summary>
	/// <remarks>
	/// Keeping the description apart from the start is what makes the command lines testable: a
	/// test asserts on the values, and only <see cref="Abstractions.IProcessLauncher"/> touches the
	/// machine.
	/// </remarks>
	public sealed class ProcessLaunch
	{
		public ProcessLaunch(string fileName, string arguments, bool useShellExecute)
		{
			if (string.IsNullOrWhiteSpace(fileName))
			{
				throw new ArgumentException("A file name is required.", nameof(fileName));
			}

			FileName = fileName;
			Arguments = arguments ?? string.Empty;
			UseShellExecute = useShellExecute;
		}

		public string FileName { get; }

		public string Arguments { get; }

		/// <summary>
		/// Whether the shell starts the process. A console application needs it, because Visual
		/// Studio has no console of its own to lend, and a URL needs it to reach the browser.
		/// </summary>
		public bool UseShellExecute { get; }

		public override string ToString()
		{
			return (FileName + " " + Arguments).Trim();
		}
	}
}
