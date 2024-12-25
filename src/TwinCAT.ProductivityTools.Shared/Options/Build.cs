using System.ComponentModel;
using Community.VisualStudio.Toolkit;

namespace TwinCAT.ProductivityTools.Options
{
	public class Build : BaseOptionModel<Build>
	{
		[Category("Build")]
		[DisplayName("Delete build artifacts on clean")]
		[Description("Specifies build artifacts should be deleted when running the Clean command.")]
		[DefaultValue(true)]
		public bool DeleteBuildArtifactsOnClean { get; set; } = false;

		[Category("Build")]
		[DisplayName("AutoSaveLibraryAfterBuild")]
		[Description(
			"Determines if the PLC project is automatically saved as library after a successful build."
		)]
		[DefaultValue(true)]
		public bool AutoSaveLibraryAfterBuild { get; set; } = false;
	}
}
