using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using TwinCAT.ProductivityTools.Abstractions;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Services
{
	/// <summary>
	/// Writes to the "TwinCAT ProductivityTools" pane of the output window.
	/// </summary>
	/// <remarks>
	/// The pane is created lazily. Creating it eagerly in the constructor would either block
	/// package initialisation or - when done in an <c>async void</c> method - leave the field null
	/// for the first callers. Those callers are the <c>catch</c> blocks of the commands, so a
	/// missing pane used to turn a reported error into a <see cref="NullReferenceException"/>.
	/// </remarks>
	internal sealed class OutputWindow : IOutputWindowPane
	{
		public const string PaneName = "TwinCAT ProductivityTools";

		private readonly AsyncLazy<OutputWindowPane> pane;

		public OutputWindow()
		{
			pane = new AsyncLazy<OutputWindowPane>(
				CreatePaneAsync,
				ThreadHelper.JoinableTaskFactory
			);
		}

		public Task WriteLineAsync() => WriteLineAsync(string.Empty);

		public async Task WriteLineAsync(string value)
		{
			OutputWindowPane outputWindowPane = await GetPaneAsync();

			if (outputWindowPane == null)
			{
				return;
			}

			try
			{
				await outputWindowPane.WriteLineAsync(value ?? string.Empty);
			}
			catch (Exception)
			{
				// Reporting is best effort. It must never mask the problem it is reporting.
			}
		}

		private async Task<OutputWindowPane> GetPaneAsync()
		{
			try
			{
				return await pane.GetValueAsync();
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static async Task<OutputWindowPane> CreatePaneAsync()
		{
			return await VS.Windows.CreateOutputWindowPaneAsync(PaneName);
		}
	}
}
