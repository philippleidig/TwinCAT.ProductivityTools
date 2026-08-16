using System;
using System.Runtime.InteropServices;

namespace TwinCAT.ProductivityTools.E2E.Tests.Infrastructure
{
	/// <summary>
	/// Makes the calling thread wait for a busy IDE instead of giving up on it.
	/// </summary>
	/// <remarks>
	/// An IDE serves automation on its main thread. While it compiles, opens a project or shows a
	/// dialog, that thread is busy and COM answers a cross apartment call with
	/// <c>RPC_E_CALL_REJECTED</c> after a two second default timeout. Without a message filter,
	/// automation code is therefore flaky by construction. Registering this filter turns the
	/// rejection into a retry, which is what a human driving the IDE would do.
	/// </remarks>
	internal sealed class ComRetryScope : IDisposable
	{
		private readonly IOleMessageFilter previous;

		private bool disposed;

		private ComRetryScope(IOleMessageFilter previous)
		{
			this.previous = previous;
		}

		/// <summary>
		/// Registers the filter for the current thread. The thread has to be single threaded
		/// apartment, otherwise COM refuses the registration.
		/// </summary>
		public static IDisposable Register()
		{
			IOleMessageFilter previous;

			int result = CoRegisterMessageFilter(new RetryFilter(), out previous);

			if (result != 0)
			{
				Marshal.ThrowExceptionForHR(result);
			}

			return new ComRetryScope(previous);
		}

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			disposed = true;

			IOleMessageFilter unused;

			CoRegisterMessageFilter(previous, out unused);
		}

		[DllImport("ole32.dll")]
		private static extern int CoRegisterMessageFilter(
			IOleMessageFilter newFilter,
			out IOleMessageFilter oldFilter
		);

		[ComImport]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("00000016-0000-0000-C000-000000000046")]
		private interface IOleMessageFilter
		{
			[PreserveSig]
			int HandleInComingCall(
				int callType,
				IntPtr threadIdCaller,
				int tickCount,
				IntPtr interfaceInfo
			);

			[PreserveSig]
			int RetryRejectedCall(IntPtr threadIdCallee, int tickCount, int rejectType);

			[PreserveSig]
			int MessagePending(IntPtr threadIdCallee, int tickCount, int pendingType);
		}

		private sealed class RetryFilter : IOleMessageFilter
		{
			private const int ServerCallIsHandled = 0;

			private const int ServerCallRetryLater = 2;

			private const int PendingMsgWaitDefProcess = 2;

			/// <summary>
			/// How long the filter keeps retrying before it lets the call fail. Opening a TwinCAT
			/// solution takes tens of seconds on a cold machine, so the budget is generous.
			/// </summary>
			private const int RetryBudgetMilliseconds = 600000;

			/// <summary>Pause between two attempts, in milliseconds.</summary>
			private const int RetryDelay = 250;

			public int HandleInComingCall(
				int callType,
				IntPtr threadIdCaller,
				int tickCount,
				IntPtr interfaceInfo
			) => ServerCallIsHandled;

			public int RetryRejectedCall(IntPtr threadIdCallee, int tickCount, int rejectType)
			{
				if (rejectType != ServerCallRetryLater)
				{
					// The call was rejected outright rather than postponed. Retrying a refusal
					// would loop forever, so it is handed back to the caller as an exception.
					return -1;
				}

				return tickCount > RetryBudgetMilliseconds ? -1 : RetryDelay;
			}

			public int MessagePending(IntPtr threadIdCallee, int tickCount, int pendingType) =>
				PendingMsgWaitDefProcess;
		}
	}
}
