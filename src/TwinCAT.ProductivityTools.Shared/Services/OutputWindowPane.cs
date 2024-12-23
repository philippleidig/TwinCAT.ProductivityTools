using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using TwinCAT.ProductivityTools.Abstractions;

namespace TwinCAT.ProductivityTools.Services
{
	public class OutputWindow : IOutputWindowPane
	{
		private OutputWindowPane _outputWindowPane;

		public OutputWindow()
		{
			Init();
		}

		private async void Init()
		{
			_outputWindowPane = await VS.Windows.CreateOutputWindowPaneAsync(
				"TwinCAT ProductivityTools"
			)
				.ConfigureAwait(false);
		}

		public Task WriteLineAsync()
		{
			return _outputWindowPane.WriteLineAsync();
		}

		public async Task WriteLineAsync(string value)
		{
			await _outputWindowPane.WriteAsync(value);
		}
	}
}
