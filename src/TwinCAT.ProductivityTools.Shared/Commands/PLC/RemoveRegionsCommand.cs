using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Abstractions;
using TwinCAT.ProductivityTools.Extensions;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	[Command(PackageIds.RemoveRegionsCommandId)]
	internal class RemoveRegionsCommand : BaseCommand<RemoveRegionsCommand>
	{
		protected override void BeforeQueryStatus(EventArgs e)
		{
			Command.Visible = VS.Solutions.IsTwinCATProjectLoaded();
			Command.Enabled = true;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var dte = await VS.GetRequiredServiceAsync<DTE, DTE>();
			var selectedItem = dte?.SelectedItems?.Item(1).ProjectItem;

			if (!(selectedItem?.Object is ITcPlcPou plcPou))
				return;

			try
			{
				dte.UndoContext.Open("Remove Regions");

				RemoveRegions(
					plcPou as ITcPlcImplementation,
					text => ((ITcPlcImplementation)plcPou).ImplementationText = text
				);
				RemoveRegions(
					plcPou as ITcPlcDeclaration,
					text => ((ITcPlcDeclaration)plcPou).DeclarationText = text
				);

				selectedItem.Save();
			}
			catch (Exception ex)
			{
				await VS.StatusBar.ShowMessageAsync(
					$"Failed to remove regions in {selectedItem.Name}. See output window for more information."
				);

				IOutputWindowPane outputWindowPane = await VS.GetRequiredServiceAsync<
					IOutputWindowPane,
					IOutputWindowPane
				>();
				await outputWindowPane.WriteLineAsync(ex.Message);
			}
			finally
			{
				dte.UndoContext.Close();
			}
		}

		private void RemoveRegions<T>(T plcPart, Action<string> updateTextAction)
			where T : class
		{
			if (plcPart == null)
				return;

			string text =
				typeof(T) == typeof(ITcPlcImplementation)
					? (plcPart as ITcPlcImplementation)?.ImplementationText
					: (plcPart as ITcPlcDeclaration)?.DeclarationText;

			if (string.IsNullOrEmpty(text))
				return;

			string lineEnding = text.Contains("\r\n") ? "\r\n" : "\n";

			var newLines = new List<string>();
			var lines = text.Split(new[] { lineEnding }, StringSplitOptions.None);

			foreach (var line in lines)
			{
				if (
					!Regex.IsMatch(line, "^\\{region\\s+\"[^\"]*\"\\}$")
					&& !Regex.IsMatch(line, "^\\{endregion\\}$")
				)
				{
					newLines.Add(line);
				}
			}

			updateTextAction(string.Join(lineEnding, newLines));
		}
	}
}
