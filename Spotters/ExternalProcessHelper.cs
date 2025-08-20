using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Spotters
{
    public class ExternalProcessHelper
    {
        [DllImport("User32.dll")]
        static extern int SetForegroundWindow(IntPtr point);

        public static Process GetExternalProcess(string processName)
        {
            // Just OBS for now...
            return Process.GetProcessesByName(processName).FirstOrDefault();
        }

        public static void SendToggleRecordHotkey(Process p, string hotkey)
        {
            IntPtr h = p.MainWindowHandle;
            SetForegroundWindow(h);

            // Fixed Shift+Ctrl+R shortcut for now...
            System.Windows.Forms.SendKeys.SendWait(hotkey);
        }
    }
}
