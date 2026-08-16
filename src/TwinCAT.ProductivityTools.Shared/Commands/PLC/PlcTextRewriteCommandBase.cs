using System;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Plc;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	/// <summary>
	/// Shared behaviour of the commands that rewrite the structured text of PLC objects.
	/// </summary>
	/// <remarks>
	/// The selection may be a single object, a folder or a whole PLC project. Everything below the
	/// selected item is rewritten, which also covers the methods, actions, properties and
	/// transitions of a POU - they are child tree items and were silently skipped before.
	/// </remarks>
	internal abstract class PlcTextRewriteCommandBase<TCommand> : BaseCommand<TCommand>
		where TCommand : PlcTextRewriteCommandBase<TCommand>, new()
	{
		/// <summary>Name of the undo step and of the progress messages.</summary>
		protected abstract string OperationName { get; }

		protected abstract string Rewrite(string text);

		protected override void BeforeQueryStatus(EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			bool isSupported = false;

			try
			{
				DTE dte = VS.GetRequiredService<DTE, DTE>();
				isSupported = dte.GetSelectedObject<ITcSmTreeItem>().CarriesPlcText();
			}
			catch (Exception)
			{
				// BeforeQueryStatus runs while the context menu is being built. An exception here
				// would tear down the whole menu, so an unreadable selection simply hides the
				// command.
			}

			Command.Visible = isSupported;
			Command.Enabled = isSupported;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			DTE dte = await VS.GetRequiredServiceAsync<DTE, DTE>();
			ProjectItem selectedItem = dte.GetSelectedProjectItem();

			if (!(selectedItem?.Object is ITcSmTreeItem treeItem) || !treeItem.CarriesPlcText())
			{
				return;
			}

			try
			{
				PlcRewriteResult result;

				using (UndoContextScope.Open(dte, OperationName))
				{
					result = new PlcTextRewriter(Rewrite).Apply(treeItem);
				}

				selectedItem.Save();

				await Report.ShowStatusAsync(
					$"{OperationName}: {result.Changed} of {result.Visited} code sections changed."
				);
			}
			catch (Exception ex)
			{
				await Report.FailureAsync(
					$"{OperationName} failed in {SafeName(selectedItem)}.",
					ex
				);
			}
		}

		private static string SafeName(ProjectItem projectItem)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				return projectItem?.Name ?? "the selected item";
			}
			catch (Exception)
			{
				return "the selected item";
			}
		}
	}
}
