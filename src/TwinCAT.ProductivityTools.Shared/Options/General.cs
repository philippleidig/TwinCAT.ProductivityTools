using System;
using System.ComponentModel;
using Community.VisualStudio.Toolkit;

namespace TwinCAT.ProductivityTools.Options
{
	public class General : BaseOptionModel<General>
	{
		[Category("General")]
		[DisplayName("Path to code.exe")]
		[Description("Specify the path to code.exe.")]
		public string VsCodeInstallPath { get; set; } =
			Environment.ExpandEnvironmentVariables(
				@"%localappdata%\Programs\Microsoft VS Code\Code.exe"
			);
	}
}
