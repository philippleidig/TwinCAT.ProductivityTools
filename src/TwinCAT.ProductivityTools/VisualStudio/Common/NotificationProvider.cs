using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace VisualStudio.Extension
{
    public static class NotificationProvider
    {
        public static IServiceProvider ServiceProvider;

        public static void DisplayInStatusBar(string message)
        {
            var statusBar = GetStatusBar(ServiceProvider);
            int frozen;
            statusBar.IsFrozen(out frozen);
            if (frozen == 0)
            {
                statusBar.SetText(message);
            }
        }

        private static IVsStatusbar GetStatusBar(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService(typeof(SVsStatusbar)) as IVsStatusbar;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
        public static void ShowWarningMessage(string message, string title)
        {
            VsShellUtilities.ShowMessageBox(
               ServiceProvider,
               message,
               title,
               OLEMSGICON.OLEMSGICON_WARNING,
               OLEMSGBUTTON.OLEMSGBUTTON_OK,
               OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
        public static void ShowInfoMessage(string message, string title)
        {
            VsShellUtilities.ShowMessageBox(
               ServiceProvider,
               message,
               title,
               OLEMSGICON.OLEMSGICON_INFO,
               OLEMSGBUTTON.OLEMSGBUTTON_OK,
               OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public static void ShowErrorMessage(Exception exception, string title)
        {
            ShowErrorMessage(exception.Message, title);
        }

        public static void ShowErrorMessage(string message, string title)
        {
            VsShellUtilities.ShowMessageBox(
                ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        public static bool ShowQueryMessage(string message, string title)
        {
            VsShellMessageBoxResult result = (VsShellMessageBoxResult) VsShellUtilities.ShowMessageBox(
                ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_QUERY,
                OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);

            return result != VsShellMessageBoxResult.OK ? false : true;
        }
    }
}
