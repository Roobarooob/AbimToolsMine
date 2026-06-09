using Autodesk.Revit.UI;
using System;
using System.Windows;
using System.Windows.Interop;

namespace AbimToolsMine
{
    internal static class RevitWindowOwner
    {
        public static void SetOwner(Window window, UIApplication application)
        {
            if (window == null || application == null)
                return;

            try
            {
                IntPtr revitHandle = application.MainWindowHandle;
                if (revitHandle != IntPtr.Zero)
                    new WindowInteropHelper(window).Owner = revitHandle;
            }
            catch
            {
                // Revit can still show the window without an owner.
            }
        }
    }
}
