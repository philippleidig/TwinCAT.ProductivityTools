using System;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using TwinCAT.ProductivityTools.InfoBars;
using Task = System.Threading.Tasks.Task;

namespace TwinCAT.ProductivityTools.Extensions
{
	public static class AsyncPackageExtensions
	{
		public static void AddService<TService, TInterface>(
			this AsyncPackage package,
			TService service
		)
			where TService : class, TInterface
		{
			package.AddService(
				typeof(TInterface),
				(container, cancellation, type) => Task.FromResult<object>(service),
				true
			);
		}
	}
}
