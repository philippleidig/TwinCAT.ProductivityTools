using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace TwinCAT.ProductivityTools.Helpers
{
	/// <summary>
	/// Opens a Visual Studio undo context and closes exactly the one it opened.
	/// </summary>
	/// <remarks>
	/// <c>UndoContext.Open</c> throws when a context is already open and <c>UndoContext.Close</c>
	/// throws when none is open. Closing a context in a <c>finally</c> block that was never opened
	/// therefore replaces the original error with a second one - or turns a successful command into
	/// a failing one. This scope only closes what it opened and never throws.
	/// </remarks>
	internal sealed class UndoContextScope : IDisposable
	{
		private readonly DTE dte;

		private UndoContextScope(DTE dte)
		{
			this.dte = dte;
		}

		public static UndoContextScope Open(DTE dte, string name)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				UndoContext undoContext = dte?.UndoContext;

				if (undoContext == null || undoContext.IsOpen)
				{
					// Another operation already owns the undo stack. Its context will collect our
					// changes as well, which is still correct - it just groups a little more.
					return new UndoContextScope(null);
				}

				undoContext.Open(name, false);

				return new UndoContextScope(dte);
			}
			catch (Exception)
			{
				// Undo support is a convenience. A command must still run without it.
				return new UndoContextScope(null);
			}
		}

		public void Dispose()
		{
			if (dte == null)
			{
				return;
			}

			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				if (dte.UndoContext?.IsOpen == true)
				{
					dte.UndoContext.Close();
				}
			}
			catch (Exception)
			{
				// Nothing can be done about a failing close, and throwing here would hide the
				// exception the command itself may be reporting.
			}
		}
	}
}
