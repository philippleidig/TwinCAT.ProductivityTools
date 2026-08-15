using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Installation;
using Xunit;

namespace TwinCAT.ProductivityTools.E2E.Tests.Infrastructure
{
	/// <summary>
	/// Starts one IDE for the whole test class and creates a TwinCAT solution in it.
	/// </summary>
	/// <remarks>
	/// Starting a shell and creating a TwinCAT project costs tens of seconds, so both are shared.
	/// Every test adds its own PLC project to stay independent without paying for a second start.
	/// </remarks>
	public class TwinCatSolutionFixture : IDisposable
	{
		private const string SolutionName = "E2E";

		public TwinCatSolutionFixture()
		{
			if (E2EPrerequisites.MissingReason() != null)
			{
				// Every test in the class is skipped anyway. Building the fixture would only
				// produce a misleading error.
				return;
			}

			Session = IdeSession.Start(E2EPrerequisites.Ide);

			try
			{
				SolutionPath = Path.Combine(Session.WorkingFolder, SolutionName + ".sln");

				Session.Run(() =>
				{
					Session.Dte.Solution.Create(Session.WorkingFolder, SolutionName + ".sln");

					Project project = Session.Dte.Solution.AddFromTemplate(
						XaeTemplatePath(),
						Path.Combine(Session.WorkingFolder, SolutionName),
						SolutionName
					);

					SystemManager = (ITcSysManager2)project.Object;

					Session.Dte.Solution.SaveAs(SolutionPath);
				});
			}
			catch
			{
				Session.Dispose();
				Session = null;
				throw;
			}
		}

		public IdeSession Session { get; private set; }

		public ITcSysManager2 SystemManager { get; private set; }

		public string SolutionPath { get; private set; }

		/// <summary>Runs automation work on the thread that owns the IDE.</summary>
		public void Run(Action work) => Session.Run(work);

		/// <summary>Runs automation work on the thread that owns the IDE.</summary>
		public T Run<T>(Func<T> work) => Session.Run(work);

		/// <summary>
		/// Adds a PLC project from the standard template and returns its project node. Must be
		/// called from inside <see cref="Run(Action)"/>.
		/// </summary>
		/// <remarks>
		/// How the template is addressed changed between TwinCAT versions: 4024 takes the full
		/// path of the <c>.plcproj</c>, while a 4026 engineering also accepts the bare file name
		/// and falls back to its own default when nothing is given. Trying the variants in turn
		/// keeps the tests running on either, and the aggregated message names all of them when
		/// none works.
		/// </remarks>
		public ITcSmTreeItem AddPlcProject(string name)
		{
			ITcSmTreeItem plc = SystemManager.LookupTreeItem("TIPC");

			string template = PlcTemplatePath();

			var attempts = new List<string>
			{
				template,
				Path.GetFileName(template),
				Path.GetFileNameWithoutExtension(template),
				string.Empty,
			};

			var failures = new List<string>();

			for (int attempt = 0; attempt < attempts.Count; attempt++)
			{
				// Every attempt needs its own name. A failing attempt already writes the .plcproj
				// to disk, so reusing the name would make the remaining attempts fail on the
				// leftover file instead of on the template.
				string candidateName = attempt == 0 ? name : $"{name}_{attempt}";

				string described = attempts[attempt].Length == 0 ? "<default>" : attempts[attempt];

				ITcSmTreeItem created;

				try
				{
					created = plc.CreateChild(candidateName, 0, string.Empty, attempts[attempt]);
				}
				catch (Exception exception)
				{
					failures.Add($"'{described}': {exception.Message}");

					continue;
				}

				// A rejected template does not always raise: some TwinCAT versions answer with a
				// bare PLC node that carries the instance but no sources. Such a node would make
				// the tests fail much later with a confusing message, so it is discarded here.
				if (FindDescendant(created, "POUs", 4) != null)
				{
					return created;
				}

				failures.Add($"'{described}': the new node contains no sources.");

				TryDeleteChild(plc, candidateName);
			}

			throw new InvalidOperationException(
				"No PLC project could be created from '"
					+ template
					+ "'. Attempts: "
					+ string.Join(" | ", failures)
			);
		}

		private static void TryDeleteChild(ITcSmTreeItem parent, string name)
		{
			try
			{
				parent.DeleteChild(name);
			}
			catch (Exception)
			{
				// Leaving the node behind only costs the next attempt a different name.
			}
		}

		/// <summary>
		/// Returns the child with the given name. Must be called from inside
		/// <see cref="Run(Action)"/>.
		/// </summary>
		public static ITcSmTreeItem Child(ITcSmTreeItem parent, string name)
		{
			for (int index = 1; index <= parent.ChildCount; index++)
			{
				ITcSmTreeItem child = parent.Child[index];

				if (string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
				{
					return child;
				}
			}

			throw new InvalidOperationException($"'{parent.Name}' has no child called '{name}'.");
		}

		/// <summary>
		/// Searches the subtree for the first node with the given name.
		/// </summary>
		/// <remarks>
		/// A PLC node carries both the project and its instance, and which one comes first is not
		/// contractual. Searching by name instead of by index keeps the tests independent of that
		/// ordering and of the extra levels a future TwinCAT version might introduce.
		/// </remarks>
		public static ITcSmTreeItem Descendant(
			ITcSmTreeItem root,
			string name,
			int maximumDepth = 4
		)
		{
			ITcSmTreeItem found = FindDescendant(root, name, maximumDepth);

			if (found == null)
			{
				var visited = new List<string>();

				Describe(root, maximumDepth, string.Empty, visited);

				throw new InvalidOperationException(
					$"'{root.Name}' has no descendant called '{name}' within {maximumDepth} "
						+ "levels. The subtree is: "
						+ string.Join(", ", visited)
				);
			}

			return found;
		}

		private static void Describe(
			ITcSmTreeItem parent,
			int remainingDepth,
			string prefix,
			List<string> into
		)
		{
			if (remainingDepth <= 0)
			{
				return;
			}

			for (int index = 1; index <= parent.ChildCount; index++)
			{
				ITcSmTreeItem child = parent.Child[index];

				string path = prefix + "/" + child.Name;

				into.Add(path);

				Describe(child, remainingDepth - 1, path, into);
			}
		}

		private static ITcSmTreeItem FindDescendant(
			ITcSmTreeItem parent,
			string name,
			int remainingDepth
		)
		{
			if (remainingDepth <= 0)
			{
				return null;
			}

			for (int index = 1; index <= parent.ChildCount; index++)
			{
				ITcSmTreeItem child = parent.Child[index];

				if (string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
				{
					return child;
				}

				ITcSmTreeItem deeper = FindDescendant(child, name, remainingDepth - 1);

				if (deeper != null)
				{
					return deeper;
				}
			}

			return null;
		}

		/// <summary>
		/// Locates the XAE project template through the registry, because its root moved between
		/// TwinCAT 4024 and 4026.
		/// </summary>
		private static string XaeTemplatePath()
		{
			var installation = new TwinCATInstallation();

			var candidates = new List<string>();

			if (!string.IsNullOrEmpty(installation.InstallationDirectory))
			{
				candidates.Add(
					Path.Combine(
						installation.InstallationDirectory,
						@"Components\Base\PrjTemplate\TwinCAT Project.tsproj"
					)
				);
			}

			candidates.Add(
				@"C:\Program Files (x86)\Beckhoff\TwinCAT\3.1\Components\Base\PrjTemplate\TwinCAT Project.tsproj"
			);
			candidates.Add(@"C:\TwinCAT\3.1\Components\Base\PrjTemplate\TwinCAT Project.tsproj");

			string found = candidates.FirstOrDefault(File.Exists);

			if (found == null)
			{
				throw new FileNotFoundException(
					"The TwinCAT XAE project template was not found. Looked in: "
						+ string.Join(", ", candidates)
				);
			}

			return found;
		}

		/// <summary>
		/// Locates the standard PLC project template of the installed TwinCAT version.
		/// </summary>
		private static string PlcTemplatePath()
		{
			var installation = new TwinCATInstallation();

			var roots = new List<string>();

			if (!string.IsNullOrEmpty(installation.PlcTemplatesDirectory))
			{
				roots.Add(installation.PlcTemplatesDirectory);
			}

			roots.Add(@"C:\ProgramData\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates");
			roots.Add(@"C:\TwinCAT\3.1\Components\Plc\PlcTemplates");

			foreach (string root in roots.Where(Directory.Exists))
			{
				// The templates are versioned by folder and several versions coexist after an
				// upgrade. The oldest one no longer matches the layout the installed engineering
				// expects, so the newest has to win.
				string template = Directory
					.EnumerateFiles(root, "Standard*.plcproj", SearchOption.AllDirectories)
					.OrderByDescending(VersionOf)
					.ThenByDescending(path => path)
					.FirstOrDefault();

				if (template != null)
				{
					return template;
				}
			}

			throw new FileNotFoundException(
				"No standard PLC project template was found. Looked in: " + string.Join(", ", roots)
			);
		}

		/// <summary>
		/// Reads the version out of the folder a template lives in, so that 1.1.0.1 sorts above
		/// 1.0.0.0 rather than below it as a string would.
		/// </summary>
		private static Version VersionOf(string templatePath)
		{
			for (
				DirectoryInfo directory = Directory.GetParent(templatePath);
				directory != null;
				directory = directory.Parent
			)
			{
				Version version;

				if (Version.TryParse(directory.Name, out version))
				{
					return version;
				}
			}

			return new Version(0, 0);
		}

		public void Dispose()
		{
			Session?.Dispose();
			Session = null;
		}
	}

	/// <summary>
	/// Groups every test that shares one IDE instance.
	/// </summary>
	[CollectionDefinition(Name)]
	public class TwinCatSolutionCollection : ICollectionFixture<TwinCatSolutionFixture>
	{
		public const string Name = "TwinCAT solution";
	}
}
