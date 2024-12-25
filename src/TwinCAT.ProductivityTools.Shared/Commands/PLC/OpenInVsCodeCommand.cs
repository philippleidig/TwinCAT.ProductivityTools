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
			var dte = await VS.GetRequiredServiceAsync<DTE, DTE>();
			var selectedItem = dte?.SelectedItems?.Item(1).ProjectItem;

			string filePath = selectedItem.Properties.Item("FullPath").Value.ToString();

			if (!(selectedItem?.Object is ITcSmTreeItem treeItem))
			{
				return;
			}

			if (treeItem.IsPlcProjectFolder())
			{
				await OpenFolderInVsAsync(filePath);
			}
			else 
			{
				await OpenFileInVsAsync(filePath);
			}
		}

		private async Task OpenFileInVsAsync(string path)
		{
			if (!File.Exists(path)) 
			{
				// await VS.MessageBox.ShowErrorAsync();
			}

			OpenVsCode(path);
		}

		private async Task OpenFolderInVsAsync(string path)
		{
			if (!Directory.Exists(path))
			{
				// await VS.MessageBox.ShowErrorAsync();
			}

			OpenVsCode(path);
		}

		private void OpenVsCode(string path)
		{
			EnsurePathExist();
			bool isDirectory = Directory.Exists(path);

			var args = isDirectory ? "." : path;

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
