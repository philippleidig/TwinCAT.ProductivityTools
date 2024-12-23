using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Abstractions;

namespace TwinCAT.ProductivityTools.Services
{
	internal class ProjectFreezerService : IProjectFreezerService
	{
		public async Task FreezeProjectsAsync(IEnumerable<EnvDTE.Project> projects)
		{
			//ITcRemoteManager remoteManager = dte?.GetObject("TcRemoteManager") as ITcRemoteManager;

			foreach (EnvDTE.Project project in projects)
			{
				try
				{
					await FreezeProjectAsync(project);
				}
				catch (Exception ex) { }
			}
		}

		public async Task FreezeProjectAsync(EnvDTE.Project project)
		{
			if (!(project is TcSysManager))
			{
				return;
			}

			var tsProjectFile = XDocument.Load(project.FullName);
			tsProjectFile.Element("TcSmProject").Add(new XAttribute("TcVersionFixed", "true"));
			tsProjectFile.Save(project.FullName);

			ITcSysManager systemManager = project as TcSysManager;
			ITcSmTreeItem plcProjectItems = systemManager.LookupTreeItem("TIPC") as ITcSmTreeItem;

			foreach (ITcSmTreeItem plcProjectItem in plcProjectItems)
			{
				await FreezePlcProjectAsync(plcProjectItem);
			}
		}

		public async Task FreezePlcProjectAsync(ITcSmTreeItem project)
		{
			if (!(project is ITcProjectRoot))
			{
				return;
			}

			ITcProjectRoot projectRoot = (ITcProjectRoot)project;
			ITcSmTreeItem _plcProjectItem = (ITcSmTreeItem)projectRoot.NestedProject;
			ITcPlcIECProject _iecProjectItem = (ITcPlcIECProject)_plcProjectItem;

			ITcPlcLibraryManager references =
				_plcProjectItem.LookupChild("References") as ITcPlcLibraryManager;
			references.FreezePlaceholder();

			var plcProjectXml = XDocument.Parse(_plcProjectItem.ProduceXml());
			var plcRootXml = XDocument.Parse(project.ProduceXml());

			var projectFilePath = plcRootXml
				.Element("TreeItem")
				.Element("PlcProjectDef")
				.Element("ProjectPath")
				.Value;

			var projectInfo = plcProjectXml
				.Element("TreeItem")
				.Element("IECProjectDef")
				.Element("ProjectInfo");

			var compilerSettings = plcProjectXml
				.Element("TreeItem")
				.Element("IECProjectDef")
				.Element("CompilerSettings");
			var activeCompilerVersion = compilerSettings.Element("ActiveCompiler").Value;

			var plcProjectFile = XDocument.Load(projectFilePath);
			XNamespace ns = plcProjectFile.Root.GetDefaultNamespace();

			var propertyGroup = plcProjectFile
				.Element(ns + "Project")
				.Element(ns + "PropertyGroup");
			var projectExtensions = plcProjectFile
				.Element(ns + "Project")
				.Element(ns + "ProjectExtensions");

			propertyGroup.Element(ns + "SecureOnlineMode")?.Remove();
			propertyGroup.Element(ns + "AutoUpdateVisuProfile")?.Remove();
			propertyGroup.Element(ns + "AutoUpdateUmlProfile")?.Remove();

			var compilerVersionElement = propertyGroup.Element("CompilerVersion");

			if (compilerVersionElement != null)
			{
				compilerVersionElement.Value = activeCompilerVersion;
			}
			else
			{
				propertyGroup.Add(new XElement(ns + "CompilerVersion", activeCompilerVersion));
			}

			plcProjectFile.Save(projectFilePath);
		}
	}
}
