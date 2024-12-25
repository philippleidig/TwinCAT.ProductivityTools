using Microsoft.VisualStudio.PlatformUI;

namespace TwinCAT.ProductivityTools
{
    public class BaseDialogWindow : DialogWindow
	{
        public BaseDialogWindow()
        {
            this.HasMaximizeButton = true;
            this.HasMinimizeButton = true;
        }
    }
}
