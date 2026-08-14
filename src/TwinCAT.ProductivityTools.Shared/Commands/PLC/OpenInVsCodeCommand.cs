using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Windows.Forms;
using TCatSysManagerLib;
using TwinCAT.ProductivityTools.Extensions;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Options;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Commands
{
	[Command(PackageIds.OpenInVsCodeCommandId)]
	internal sealed class OpenVsCodeCommand : BaseCommand<OpenVsCodeCommand>
	{
		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			var dte = await VS.GetRequiredServiceAsync<DTE, DTE>();
			ProjectItem selectedItem = dte.GetSelectedProjectItem();

			if (!(selectedItem?.Object is ITcSmTreeItem treeItem))
			{
				return;
			}

			string filePath = selectedItem.Properties?.Item("FullPath")?.Value?.ToString();

			if (string.IsNullOrEmpty(filePath))
			{
				await VS.MessageBox.ShowErrorAsync(
					Vsix.Name,
					"The selected item does not have a file system path."
				);
				return;
			}

			if (treeItem.IsPlcProjectFolder())
			{
				await OpenFolderInVsCodeAsync(filePath);
			}
			else
			{
				await OpenFileInVsCodeAsync(filePath);
			}
		}

		private async Task OpenFileInVsCodeAsync(string path)
		{
			if (!File.Exists(path))
			{
				await VS.MessageBox.ShowErrorAsync(Vsix.Name, "File not found: " + path);
				return;
			}

			OpenVsCode(path);
		}

		private async Task OpenFolderInVsCodeAsync(string path)
		{
			if (!Directory.Exists(path))
			{
				await VS.MessageBox.ShowErrorAsync(Vsix.Name, "Folder not found: " + path);
				return;
			}

			OpenVsCode(path);
		}

		private void OpenVsCode(string path)
		{
			EnsurePathExist();
			bool isDirectory = Directory.Exists(path);

			var args = isDirectory ? "." : $"\"{path}\"";

			var start = new System.Diagnostics.ProcessStartInfo()
			{
				FileName = $"\"{General.Instance.VsCodeInstallPath}\"",
				Arguments = args,
				CreateNoWindow = true,
				UseShellExecute = false,
				WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
			};

			if (isDirectory)
			{
				start.WorkingDirectory = path;
			}

			using (System.Diagnostics.Process.Start(start)) { }
		}

		private void EnsurePathExist()
		{
			if (File.Exists(General.Instance.VsCodeInstallPath))
				return;

			if (!string.IsNullOrEmpty(VsCodeDetect.InRegistry()))
			{
				SaveVsCodeInstallPath(VsCodeDetect.InRegistry());
			}
			else if (!string.IsNullOrEmpty(VsCodeDetect.InEnvVarPath()))
			{
				SaveVsCodeInstallPath(VsCodeDetect.InEnvVarPath());
			}
			else if (!string.IsNullOrEmpty(VsCodeDetect.InLocalAppData()))
			{
				SaveVsCodeInstallPath(VsCodeDetect.InLocalAppData());
			}
			else
			{
				var isConfirmed = VS.MessageBox.ShowConfirm(
					Vsix.Name,
					"I can't find Visual Studio Code (Code.exe). Would you like to help me find it?"
				);

				if (!isConfirmed)
					return;

				var dialog = new OpenFileDialog
				{
					DefaultExt = ".exe",
					FileName = "Code.exe",
					InitialDirectory = Environment.GetFolderPath(
						Environment.SpecialFolder.ProgramFiles
					),
					CheckFileExists = true
				};

				var result = dialog.ShowDialog();

				if (result == DialogResult.OK)
				{
					SaveVsCodeInstallPath(dialog.FileName);
				}
			}
		}

		private void SaveVsCodeInstallPath(string path)
		{
			General.Instance.VsCodeInstallPath = path;
			General.Instance.Save();
		}
	}
}
