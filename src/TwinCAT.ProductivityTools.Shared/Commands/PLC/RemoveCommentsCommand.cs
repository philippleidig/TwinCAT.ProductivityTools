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
	[Command(PackageIds.RemoveCommentsCommandId)]
	internal class RemoveCommentsCommand : BaseCommand<RemoveCommentsCommand>
	{
		protected override void BeforeQueryStatus(EventArgs e)
		{
			Command.Visible = VS.Solutions.IsTwinCATProjectLoaded();
			Command.Enabled = true;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			EnvDTE.DTE dte = await VS.GetRequiredServiceAsync<DTE, DTE>();
			EnvDTE.ProjectItem selectedItem = dte?.SelectedItems?.Item(1).ProjectItem;

			if (selectedItem == null || !(selectedItem.Object is ITcPlcPou plcPou))
				return;

			try
			{
				dte.UndoContext.Open("Remove Comments");

				RemoveComments(
					plcPou as ITcPlcImplementation,
					text => (plcPou as ITcPlcImplementation).ImplementationText = text
				);
				RemoveComments(
					plcPou as ITcPlcDeclaration,
					text => (plcPou as ITcPlcDeclaration).DeclarationText = text
				);

				selectedItem.Save();
			}
			catch (Exception ex)
			{
				await VS.StatusBar.ShowMessageAsync(
					$"Failed to remove comments in {selectedItem.Name}. See output window for more information."
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

		private void RemoveComments<T>(T plcPart, Action<string> updateTextAction)
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

			var isMultiLineCommentActive = false;

			foreach (var line in lines)
			{
				var re =
					@"(@(?:""[^""]*"")+|""(?:[^""\n\\]+|\\.)*""|'(?:[^'\n\\]+|\\.)*')|//.*|\(\*(?s:.*?)\*\)";
				string cleanedLine = Regex.Replace(line, re, "$1");

				if (Regex.IsMatch(cleanedLine, "(\\(\\*.*)"))
				{
					isMultiLineCommentActive = true;
				}

				if (Regex.IsMatch(cleanedLine, "(.*\\*\\))"))
				{
					isMultiLineCommentActive = false;
				}

				if (isMultiLineCommentActive)
				{
					continue;
				}

				newLines.Add(cleanedLine);
			}

			updateTextAction(string.Join(lineEnding, newLines));
		}
	}
}
