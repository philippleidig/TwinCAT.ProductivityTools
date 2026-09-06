using TwinCAT.ProductivityTools.Remote;

namespace TwinCAT.ProductivityTools.Abstractions
{
	/// <summary>
	/// Starts a local process, so that everything which only decides <em>what</em> to start stays
	/// testable without a machine that can run it.
	/// </summary>
	public interface IProcessLauncher
	{
		void Start(ProcessLaunch launch);
	}
}
