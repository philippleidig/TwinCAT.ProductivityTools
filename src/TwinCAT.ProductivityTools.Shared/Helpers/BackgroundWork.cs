using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Helpers
{
	/// <summary>
	/// Starts asynchronous work from a synchronous context, typically an event handler of the IDE
	/// or of a WPF control.
	/// </summary>
	/// <remarks>
	/// Such a handler cannot be made <c>async</c> without turning it into <c>async void</c>, where
	/// an exception is rethrown on the thread pool and takes the whole IDE down. Routing the work
	/// through the joinable task factory instead keeps it tracked, so a shutdown can wait for it,
	/// and any failure ends up in the output window rather than in a crash.
	/// </remarks>
	internal static class BackgroundWork
	{
		/// <summary>
		/// Runs <paramref name="work"/> without blocking the caller and reports a failure.
		/// </summary>
		/// <param name="work">The work to run.</param>
		/// <param name="failureMessage">Message shown when the work throws.</param>
		internal static void Run(Func<Task> work, string failureMessage)
		{
			if (work == null)
			{
				throw new ArgumentNullException(nameof(work));
			}

			// VSSDK007 wants the joinable task awaited or joined. Neither is possible here, because
			// the caller is a synchronous handler. The task is tracked by the factory and its
			// failure is reported below, which is what the rule is trying to protect against.
#pragma warning disable VSSDK007
			JoinableTask joinableTask = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				try
				{
					await work().ConfigureAwait(false);
				}
				catch (Exception exception)
				{
					await Report.FailureAsync(failureMessage, exception);
				}
			});
#pragma warning restore VSSDK007

			joinableTask.Task.Forget();
		}
	}
}
