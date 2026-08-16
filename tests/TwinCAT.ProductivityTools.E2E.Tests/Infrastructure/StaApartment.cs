using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace TwinCAT.ProductivityTools.E2E.Tests.Infrastructure
{
	/// <summary>
	/// A single thread in a single threaded apartment that all automation work runs on.
	/// </summary>
	/// <remarks>
	/// xunit executes tests on thread pool threads, which are multi threaded apartment. That breaks
	/// automation in two ways: <c>CoRegisterMessageFilter</c> is rejected outside an STA, so a busy
	/// IDE answers with <c>RPC_E_CALL_REJECTED</c> instead of being waited for, and every call has
	/// to be marshalled anyway. Owning one STA thread for the whole session solves both, and it
	/// keeps the automation object on the thread that created it, which is what COM expects.
	/// </remarks>
	public sealed class StaApartment : IDisposable
	{
		private readonly BlockingCollection<Action> queue = new BlockingCollection<Action>();

		private readonly Thread thread;

		private bool disposed;

		public StaApartment(string name)
		{
			thread = new Thread(Pump) { Name = name, IsBackground = true, };

			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		/// <summary>Runs the work on the apartment thread and waits for it.</summary>
		public void Run(Action work)
		{
			Run(() =>
			{
				work();
				return true;
			});
		}

		/// <summary>Runs the work on the apartment thread and returns its result.</summary>
		public T Run<T>(Func<T> work)
		{
			if (work == null)
			{
				throw new ArgumentNullException(nameof(work));
			}

			if (Thread.CurrentThread == thread)
			{
				return work();
			}

			var completion = new TaskCompletionSource<T>();

			queue.Add(() =>
			{
				try
				{
					completion.SetResult(work());
				}
				catch (Exception exception)
				{
					completion.SetException(exception);
				}
			});

			try
			{
				return completion.Task.GetAwaiter().GetResult();
			}
			catch (AggregateException aggregate) when (aggregate.InnerExceptions.Count == 1)
			{
				throw aggregate.InnerExceptions[0];
			}
		}

		private void Pump()
		{
			foreach (Action work in queue.GetConsumingEnumerable())
			{
				work();
			}
		}

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			disposed = true;

			queue.CompleteAdding();

			// The thread is a background thread, so a stuck automation call cannot keep the test
			// host alive. Waiting a little still gives an orderly shutdown the chance it deserves.
			thread.Join(TimeSpan.FromSeconds(30));

			queue.Dispose();
		}
	}
}
