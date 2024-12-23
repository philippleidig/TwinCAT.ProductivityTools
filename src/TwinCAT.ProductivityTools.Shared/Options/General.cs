using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using Community.VisualStudio.Toolkit;
using static Microsoft.VisualStudio.Shell.DialogPage;

namespace TwinCAT.ProductivityTools.Options
{
	//internal partial class OptionsProvider
	//{
	//    [ComVisible(true)]
	//    public class GeneralOptions : BaseOptionPage<General> { }
	//}

	public class General : BaseOptionModel<General>
	{
		[Category("General")]
		[DisplayName("AutoSaveLibraryAfterBuild")]
		[Description(
			"Determines if the PLC project is automatically saved as library after a successful build."
		)]
		[DefaultValue(true)]
		public bool AutoSaveLibraryAfterBuild { get; set; } = true;

		[Category("General")]
		[DisplayName("Path to code.exe")]
		[Description("Specify the path to code.exe.")]
		public string VsCodeInstallPath { get; set; } =
			Environment.ExpandEnvironmentVariables(
				@"%localappdata%\Programs\Microsoft VS Code\Code.exe"
			);
	}
}
