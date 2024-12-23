using System.Collections;
using System.Collections.Generic;

namespace TwinCAT.ProductivityTools.Helpers
{
	internal class FileFilterProvider : IEnumerable<string>
	{
		private IEnumerable<string> _files = new List<string>()
		{
			// git folder
			//".git/",

			// Visual Studio
			".vs/",
			"*.suo",
			"*.user",
			"*.userosscache",
			// TwinCAT PLC files
			"*.plcproj.bak",
			"*.plcproj.orig",
			"*.tpy",
			"*.tclrs",
			"#*.compiled-library",
			"#*.library",
			"*.compileinfo",
			"*.core",
			"LineIDs.dbg",
			"LineIDs.dbg.bak",
			//"*.tmc",
			"*.tmcRefac",
			// TwinCAT XAE files
			"*.tsproj.bak",
			"*.tsproj.orig",
			"*.xti.bak",
			"*.xti.orig",
			"*.project.~u",
			// TwinCAT folders
			"_Boot/",
			"_CompileInfo/",
			"_Libraries/",
			"_ModuleInstall/",
			"_Deployment/",
			"_Repository/"
		};

		public IEnumerator<string> GetEnumerator()
		{
			return _files.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _files.GetEnumerator();
		}
	}
}
