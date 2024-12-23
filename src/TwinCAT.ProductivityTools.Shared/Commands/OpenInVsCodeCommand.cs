using System;
using System.IO;
using System.Windows.Forms;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using TwinCAT.ProductivityTools.Helpers;
using TwinCAT.ProductivityTools.Options;
using static System.Windows.Forms.Design.AxImporter;

namespace TwinCAT.ProductivityTools.Commands
{
	internal sealed class OpenVsCodeCommand
	{
		private void OpenCurrentFileInVs(object sender, EventArgs e)
		{
			try
			{
				var dte = VS.GetRequiredService<DTE, DTE>() as DTE2;
				Assumes.Present(dte);

				var activeDocument = dte.ActiveDocument;

				if (activeDocument != null)
				{
					var path = activeDocument.FullName;

					if (!string.IsNullOrEmpty(path))
					{
						OpenVsCode(path);
					}
					else
					{
						VS.MessageBox.Show("Couldn't resolve the folder");
					}
				}
				else
				{
					VS.MessageBox.Show("Couldn't find active document");
				}
			}
			catch (Exception ex)
			{
				VS.MessageBox.ShowError(ex.Message);
			}
		}

		private void OpenFolderInVs(object sender, EventArgs e)
		{
			try
			{
				var dte = VS.GetRequiredService<DTE, DTE>() as DTE2;
				Assumes.Present(dte);

				//string path = ProjectHelpers.GetSelectedPath(dte, General.Instance.OpenSolutionProjectAsRegularFile);
				string path = "";

				if (!string.IsNullOrEmpty(path))
				{
					OpenVsCode(path);
				}
				else
				{
					VS.MessageBox.Show("Couldn't resolve the folder");
				}
			}
			catch (Exception ex)
			{
				VS.MessageBox.ShowError(ex.Message);
			}
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
