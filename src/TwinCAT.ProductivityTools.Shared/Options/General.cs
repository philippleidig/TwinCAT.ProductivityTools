using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace TwinCAT.ProductivityTools.Options
{
	internal partial class OptionsProvider
	{
		[ComVisible(true)]
		public class GeneralOptions : BaseOptionPage<General> { }
	}

	public class General : BaseOptionModel<General>
	{
		[Category("External tools")]
		[DisplayName("Path to code.exe")]
		[Description(
			"Full path of the Visual Studio Code executable that is used by \"Open in VS Code\". "
				+ "Leave empty to detect the installation automatically."
		)]
		public string VsCodeInstallPath { get; set; } = string.Empty;

		[Category("Remote access")]
		[DisplayName("SSH user name")]
		[Description(
			"User name used to open an SSH session on a TwinCAT/BSD or TwinCAT/Linux target. "
				+ "The extension connects as <user>@<address>. Administrator is the account every "
				+ "Beckhoff image ships."
		)]
		public string SshUserName { get; set; } = "Administrator";
	}
}
