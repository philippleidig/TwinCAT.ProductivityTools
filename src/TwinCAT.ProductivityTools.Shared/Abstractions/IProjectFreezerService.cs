using System.Collections.Generic;
using System.Threading.Tasks;
using TCatSysManagerLib;

namespace TwinCAT.ProductivityTools.Abstractions
{
	interface IProjectFreezerService
	{
		Task FreezeProjectsAsync(IEnumerable<EnvDTE.Project> projects);
		Task FreezeProjectAsync(EnvDTE.Project project);
		Task FreezePlcProjectAsync(ITcSmTreeItem project);
	}
}
