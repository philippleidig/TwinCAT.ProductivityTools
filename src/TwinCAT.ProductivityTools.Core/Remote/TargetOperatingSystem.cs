namespace TwinCAT.ProductivityTools.Remote
{
	/// <summary>
	/// Operating system family of a target system, as far as the target reports it.
	/// </summary>
	/// <remarks>
	/// The value decides how a session on the target is opened, which is why Windows CE is kept
	/// apart from Windows: a CE image serves neither remote desktop nor SSH, so treating it as
	/// Windows can only open a session the target refuses. <see cref="Unknown"/> is the default
	/// value so that a device information that could not be read never silently claims to be
	/// Windows.
	/// </remarks>
	public enum TargetOperatingSystem
	{
		Unknown = 0,
		Windows,
		WindowsCe,
		Linux,
		Bsd,
	}
}
