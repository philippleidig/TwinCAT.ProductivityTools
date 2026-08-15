using System;
using System.IO;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Process = System.Diagnostics.Process;
using Stopwatch = System.Diagnostics.Stopwatch;
using Thread = System.Threading.Thread;

namespace TwinCAT.ProductivityTools.E2E.Tests.Infrastructure
{
	/// <summary>
	/// A running IDE that the tests drive, together with the temporary folder its solutions live
	/// in.
	/// </summary>
	/// <remarks>
	/// The instance is created invisibly through COM and is not the one the developer happens to
	/// have open, so a test run never touches real work. Disposing closes it again and removes the
	/// temporary folder.
	/// </remarks>
	public sealed class IdeSession : IDisposable
	{
		private readonly StaApartment apartment;

		private readonly IDisposable retryScope;

		private readonly int processId;

		private bool disposed;

		private IdeSession(
			IdeUnderTest ide,
			DTE2 dte,
			StaApartment apartment,
			IDisposable retryScope,
			string workingFolder,
			int processId
		)
		{
			Ide = ide;
			Dte = dte;
			this.apartment = apartment;
			this.retryScope = retryScope;
			WorkingFolder = workingFolder;
			this.processId = processId;
		}

		public IdeUnderTest Ide { get; }

		/// <summary>
		/// The automation object. Every call on it has to go through <see cref="Run{T}"/>, because
		/// it belongs to the apartment thread.
		/// </summary>
		public DTE2 Dte { get; }

		/// <summary>Temporary folder every solution of this session is created in.</summary>
		public string WorkingFolder { get; }

		/// <summary>Runs automation work on the thread that owns the automation object.</summary>
		public T Run<T>(Func<T> work) => apartment.Run(work);

		/// <summary>Runs automation work on the thread that owns the automation object.</summary>
		public void Run(Action work) => apartment.Run(work);

		public static IdeSession Start(IdeUnderTest ide)
		{
			if (ide == null)
			{
				throw new ArgumentNullException(nameof(ide));
			}

			var apartment = new StaApartment("TcPT E2E automation");

			try
			{
				string workingFolder = Path.Combine(
					WorkingFolderRoot(),
					"TcPT.E2E",
					DateTime.Now.ToString("yyyyMMdd-HHmmss")
						+ "-"
						+ Guid.NewGuid().ToString("N").Substring(0, 8)
				);

				Directory.CreateDirectory(workingFolder);

				bool visible = IsTruthy(Environment.GetEnvironmentVariable("TCPT_E2E_VISIBLE"));

				return apartment.Run(() =>
				{
					IDisposable retryScope = ComRetryScope.Register();

					try
					{
						Type automation = Type.GetTypeFromProgID(ide.ProgId, throwOnError: true);

						var dte = (DTE2)Activator.CreateInstance(automation, nonPublic: true);

						// An automation instance stays invisible and unattended unless it is told
						// otherwise. Both have to be set before anything else runs, because a modal
						// dialog in an invisible window would deadlock the run.
						dte.UserControl = false;
						dte.SuppressUI = !visible;
						dte.MainWindow.Visible = visible;

						return new IdeSession(
							ide,
							dte,
							apartment,
							retryScope,
							workingFolder,
							ProcessIdOf(dte)
						);
					}
					catch
					{
						retryScope.Dispose();
						throw;
					}
				});
			}
			catch
			{
				apartment.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Makes the IDE visible. Useful while a failing test is being investigated and required
		/// when a screenshot is taken.
		/// </summary>
		public void Show()
		{
			Run(() =>
			{
				Dte.MainWindow.Visible = true;
				Dte.SuppressUI = false;
			});
		}

		/// <summary>
		/// Returns the folder the throw-away solutions are created in. Overridable because some
		/// TwinCAT components refuse to work below the temporary folder of a roaming profile.
		/// </summary>
		private static string WorkingFolderRoot()
		{
			string configured = Environment.GetEnvironmentVariable("TCPT_E2E_WORKDIR");

			return string.IsNullOrWhiteSpace(configured) ? Path.GetTempPath() : configured.Trim();
		}

		private static bool IsTruthy(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}

			value = value.Trim();

			return value.Equals("1", StringComparison.OrdinalIgnoreCase)
				|| value.Equals("true", StringComparison.OrdinalIgnoreCase)
				|| value.Equals("yes", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Waits until the IDE reports that it is no longer building, debugging or otherwise busy.
		/// </summary>
		public void WaitUntilIdle(TimeSpan timeout)
		{
			Stopwatch elapsed = Stopwatch.StartNew();

			while (elapsed.Elapsed < timeout)
			{
				bool idle = Run(() =>
				{
					try
					{
						return Dte.Debugger.CurrentMode == dbgDebugMode.dbgDesignMode;
					}
					catch (COMException)
					{
						// The shell is still starting up. Asking again in a moment is the only
						// option.
						return false;
					}
				});

				if (idle)
				{
					return;
				}

				Thread.Sleep(200);
			}

			throw new TimeoutException($"The IDE was still busy after {timeout}.");
		}

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			disposed = true;

			try
			{
				apartment.Run(() =>
				{
					try
					{
						Dte.Solution?.Close(SaveFirst: false);
					}
					catch (Exception)
					{
						// A half opened solution cannot always be closed cleanly. The process is
						// killed below either way.
					}

					try
					{
						Dte.Quit();
					}
					catch (Exception)
					{
						// Quit throws when the shell already went away on its own.
					}

					retryScope.Dispose();
				});
			}
			catch (Exception)
			{
				// The apartment thread may already be gone; the process is killed below.
			}

			KillIfStillRunning();

			apartment.Dispose();

			TryRemove(WorkingFolder);
		}

		private void KillIfStillRunning()
		{
			if (processId <= 0)
			{
				return;
			}

			try
			{
				using (Process process = Process.GetProcessById(processId))
				{
					if (process.WaitForExit(20000))
					{
						return;
					}

					// An IDE that ignores Quit is usually stuck on a modal dialog it opened while
					// SuppressUI was on. Leaving it behind would block the next run.
					process.Kill();
					process.WaitForExit(10000);
				}
			}
			catch (ArgumentException)
			{
				// Already gone.
			}
			catch (InvalidOperationException)
			{
				// Already gone.
			}
		}

		private static void TryRemove(string folder)
		{
			for (int attempt = 0; attempt < 5; attempt++)
			{
				try
				{
					if (Directory.Exists(folder))
					{
						Directory.Delete(folder, recursive: true);
					}

					return;
				}
				catch (IOException)
				{
					// The IDE may still hold a handle for a moment after it exited.
					Thread.Sleep(500);
				}
				catch (UnauthorizedAccessException)
				{
					Thread.Sleep(500);
				}
			}
		}

		private static int ProcessIdOf(DTE2 dte)
		{
			try
			{
				// EnvDTE 17 types HWnd as IntPtr while older versions used int, so it is converted
				// through a long to compile against either.
				IntPtr handle = new IntPtr(Convert.ToInt64(dte.MainWindow.HWnd));

				int id;

				GetWindowThreadProcessId(handle, out id);
				return id;
			}
			catch (Exception)
			{
				return 0;
			}
		}

		[DllImport("user32.dll", SetLastError = true)]
		private static extern int GetWindowThreadProcessId(IntPtr window, out int processId);
	}
}
