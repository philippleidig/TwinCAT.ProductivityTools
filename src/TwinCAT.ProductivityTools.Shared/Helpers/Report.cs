using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using TwinCAT.ProductivityTools.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Helpers
{
	/// <summary>
	/// Single place that turns an exception into user visible feedback.
	/// </summary>
	/// <remarks>
	/// Every step is guarded on its own. Reporting happens inside <c>catch</c> blocks, where a
	/// second exception would leave the user with nothing but a Visual Studio crash dialog.
	/// </remarks>
	internal static class Report
	{
		public static async Task FailureAsync(
			string summary,
			Exception exception,
			bool silent = false
		)
		{
			if (!silent)
			{
				await ShowStatusAsync(
					$"{summary} See the {OutputWindowName} output window for details."
				);
			}

			await WriteAsync($"{summary}{Environment.NewLine}{exception}");

			try
			{
				ActivityLog.TryLogError(Vsix.Name, $"{summary}{Environment.NewLine}{exception}");
			}
			catch (Exception)
			{
				// The activity log is only available when Visual Studio was started with /log.
			}
		}

		public static async Task WriteAsync(string message)
		{
			try
			{
				IOutputWindowPane outputWindowPane = await VS.GetRequiredServiceAsync<
					IOutputWindowPane,
					IOutputWindowPane
				>();

				await outputWindowPane.WriteLineAsync(message);
			}
			catch (Exception)
			{
				// The pane cannot be created before the package is sited, and the service is gone
				// once the package is disposed.
			}
		}

		public static async Task ShowStatusAsync(string message)
		{
			try
			{
				await VS.StatusBar.ShowMessageAsync(message);
			}
			catch (Exception)
			{
				// The status bar is not available while the shell is shutting down.
			}
		}

		private const string OutputWindowName = "TwinCAT ProductivityTools";
	}
}
