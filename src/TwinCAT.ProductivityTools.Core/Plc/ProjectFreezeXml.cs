using System;
using System.Linq;
using System.Xml.Linq;

namespace TwinCAT.ProductivityTools.Plc
{
	/// <summary>
	/// The document transformations that freeze a TwinCAT project on its current toolchain.
	/// </summary>
	/// <remarks>
	/// Freezing pins the TwinCAT version of the solution project and the compiler version of every
	/// PLC project, so that opening the solution on a machine with a newer TwinCAT installation does
	/// not silently migrate it. All operations are idempotent: freezing an already frozen project
	/// changes nothing.
	/// </remarks>
	public static class ProjectFreezeXml
	{
		public const string TcVersionFixedAttribute = "TcVersionFixed";
		public const string SolutionProjectRoot = "TcSmProject";

		private static readonly string[] AutoUpdateElements =
		{
			"SecureOnlineMode",
			"AutoUpdateVisuProfile",
			"AutoUpdateUmlProfile",
		};

		/// <summary>
		/// Marks the <c>.tsproj</c> document as version fixed.
		/// </summary>
		/// <returns><c>true</c> when the document was modified.</returns>
		public static bool FreezeSolutionProject(XDocument tsProject)
		{
			if (tsProject == null)
			{
				throw new ArgumentNullException(nameof(tsProject));
			}

			XElement root = tsProject.Element(SolutionProjectRoot);

			if (root == null)
			{
				return false;
			}

			XAttribute attribute = root.Attribute(TcVersionFixedAttribute);

			if (attribute != null)
			{
				if (string.Equals(attribute.Value, "true", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}

				attribute.Value = "true";

				return true;
			}

			root.Add(new XAttribute(TcVersionFixedAttribute, "true"));

			return true;
		}

		/// <summary>
		/// Pins the compiler version of a <c>.plcproj</c> document and removes the settings that
		/// would let TwinCAT update the project on load.
		/// </summary>
		/// <returns><c>true</c> when the document was modified.</returns>
		public static bool FreezePlcProject(XDocument plcProject, string compilerVersion)
		{
			if (plcProject == null)
			{
				throw new ArgumentNullException(nameof(plcProject));
			}

			XElement root = plcProject.Root;

			if (root == null)
			{
				return false;
			}

			XNamespace ns = root.GetDefaultNamespace();

			// A .plcproj can carry several property groups; the unconditioned one holds the
			// project wide settings.
			XElement propertyGroup = root.Elements(ns + "PropertyGroup")
				.FirstOrDefault(group => group.Attribute("Condition") == null);

			if (propertyGroup == null)
			{
				propertyGroup = new XElement(ns + "PropertyGroup");
				root.AddFirst(propertyGroup);
			}

			bool changed = false;

			foreach (string name in AutoUpdateElements)
			{
				foreach (XElement element in propertyGroup.Elements(ns + name).ToList())
				{
					element.Remove();
					changed = true;
				}
			}

			if (string.IsNullOrEmpty(compilerVersion))
			{
				return changed;
			}

			XElement compiler = propertyGroup.Element(ns + "CompilerVersion");

			if (compiler == null)
			{
				propertyGroup.Add(new XElement(ns + "CompilerVersion", compilerVersion));

				return true;
			}

			if (compiler.Value == compilerVersion)
			{
				return changed;
			}

			compiler.Value = compilerVersion;

			return true;
		}

		/// <summary>
		/// Reads the compiler version the PLC project is currently configured with, from the XML the
		/// System Manager produces for a PLC project node.
		/// </summary>
		public static string ReadActiveCompilerVersion(XDocument plcProjectTreeItem) =>
			plcProjectTreeItem
				?.Element("TreeItem")
				?.Element("IECProjectDef")
				?.Element("CompilerSettings")
				?.Element("ActiveCompiler")
				?.Value;

		/// <summary>
		/// Reads the path of the <c>.plcproj</c> file from the XML the System Manager produces for a
		/// PLC project root node.
		/// </summary>
		public static string ReadProjectPath(XDocument plcProjectRootTreeItem) =>
			plcProjectRootTreeItem
				?.Element("TreeItem")
				?.Element("PlcProjectDef")
				?.Element("ProjectPath")
				?.Value;
	}
}
