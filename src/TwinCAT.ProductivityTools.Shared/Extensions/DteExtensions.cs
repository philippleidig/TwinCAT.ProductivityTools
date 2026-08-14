using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace TwinCAT.ProductivityTools.Extensions
{
	/// <summary>
	/// Helpers around the current Solution Explorer selection.
	/// </summary>
	internal static class DteExtensions
	{
		/// <summary>
		/// Returns the automation object of the first selected item cast to <typeparamref name="T"/>,
		/// or <c>null</c> when nothing suitable is selected.
		/// </summary>
		/// <remarks>
		/// <c>SelectedItems.Item(1)</c> throws when the selection is empty and the underlying COM
		/// objects of the TwinCAT project system throw while a project is loading or unloading.
		/// Callers run inside <c>BeforeQueryStatus</c>, where an exception would break the whole
		/// context menu, so every failure is mapped to <c>null</c>.
		/// </remarks>
		public static T GetSelectedObject<T>(this EnvDTE.DTE dte)
			where T : class
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			return dte.GetSelectedProjectItem()?.Object as T;
		}

		/// <summary>
		/// Returns the first selected <see cref="ProjectItem"/>, or <c>null</c> when the selection
		/// is empty or cannot be read.
		/// </summary>
		public static ProjectItem GetSelectedProjectItem(this EnvDTE.DTE dte)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				SelectedItems selectedItems = dte?.SelectedItems;

				if (selectedItems == null || selectedItems.Count == 0)
				{
					return null;
				}

				return selectedItems.Item(1)?.ProjectItem;
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
