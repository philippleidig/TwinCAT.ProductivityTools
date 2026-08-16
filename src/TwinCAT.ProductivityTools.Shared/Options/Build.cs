using System.ComponentModel;
using System.Runtime.InteropServices;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace TwinCAT.ProductivityTools.Options
{
	internal partial class OptionsProvider
	{
		[ComVisible(true)]
		public class BuildOptions : BaseOptionPage<Build> { }
	}

	public class Build : BaseOptionModel<Build>
	{
		[Category("Build")]
		[DisplayName("Delete build artifacts on clean")]
		[Description(
			"Deletes the boot folder and the compiler output of a TwinCAT project from disk "
				+ "whenever the project is cleaned."
		)]
		[DefaultValue(false)]
		public bool DeleteBuildArtifactsOnClean { get; set; } = false;
	}
}
