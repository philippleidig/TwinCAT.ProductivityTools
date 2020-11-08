using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace TwinCAT.Remote.ProductivityTools
{
    class RelayCommand
    {
        private readonly Package _package;

        public RelayCommand(Package package, int commandId, Guid commandSet, Action<object, EventArgs> menuCallback, Action<object, EventArgs> beforeQueryStatus = null)
        {
            _package = package;

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (commandService != null)
            {
                var menuCommandID = new CommandID(commandSet, commandId);
                var menuItem = new OleMenuCommand(menuCallback.Invoke, menuCommandID);
                if (beforeQueryStatus != null)
                {
                    menuItem.BeforeQueryStatus += beforeQueryStatus.Invoke;
                }

                commandService.AddCommand(menuItem);
            }
        }

        private IServiceProvider ServiceProvider => _package;
    }

}
