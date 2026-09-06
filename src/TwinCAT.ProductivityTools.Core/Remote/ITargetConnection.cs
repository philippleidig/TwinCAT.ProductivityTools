namespace TwinCAT.ProductivityTools.Remote
{
	/// <summary>
	/// One way of opening an interactive session on a target system.
	/// </summary>
	public interface ITargetConnection
	{
		/// <summary>Name of the session as the user knows it, for example "Remote Desktop".</summary>
		string DisplayName { get; }

		/// <summary>
		/// The process that opens the session, or <c>null</c> when the address is unusable or the
		/// client is not installed on this machine.
		/// </summary>
		ProcessLaunch Build(string address);

		/// <summary>
		/// What to tell the user when <see cref="Build"/> returned <c>null</c>.
		/// </summary>
		/// <remarks>
		/// A missing client is a one line instruction, not a defect, so it is reported as a
		/// message instead of an exception with a stack trace in the output window.
		/// </remarks>
		string UnavailableMessage { get; }
	}
}
