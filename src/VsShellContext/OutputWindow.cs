using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Outstance.VsShellContext
{
    public static class OutputWindow
    {
        private static IVsOutputWindow _outWindow;
        private static Guid _customGuid = new Guid("ECE0AA10-F96E-4258-AC0D-58D2BBFABF52");

        public static void Log(object toWrite, bool openWindow = false)
        {
            if (toWrite == null)
                return;
            
            if (_outWindow == null)
                _outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            string customTitle = "VS Shell Context";
            _outWindow.CreatePane(ref _customGuid, customTitle, 1, 1);
            
            IVsOutputWindowPane pane;
            _outWindow.GetPane(ref _customGuid, out pane);

            pane.OutputString(toWrite.ToString());
            pane.OutputString(Environment.NewLine);
            if (openWindow)
                pane.Activate();
        }
        
        
    }
}