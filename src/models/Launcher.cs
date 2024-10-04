using System.Diagnostics;
using System.Windows.Automation;

namespace LiveCaptionsTranslator.models
{
    public static class Launcher
    {
        public static readonly string PROCESS_NAME = "LiveCaptions";

        public static AutomationElement LaunchLiveCaptions()
        {
            // Init
            KillAllProcessesByName(PROCESS_NAME);
            var process = Process.Start(PROCESS_NAME);

            // Search window
            AutomationElement window = null;
            int attempt_count = 0;
            while (window == null)
            {
                window = FindWindowByPID(process.Id);
                attempt_count++;
                if (attempt_count > 10000)
                    throw new Exception("Failed to launch!");
            }

            // Hide window
            IntPtr hWnd = new IntPtr((long)window.Current.NativeWindowHandle);
            WindowsAPI.ShowWindow(hWnd, WindowsAPI.SW_MINIMIZE);
            int exStyle = WindowsAPI.GetWindowLong(hWnd, WindowsAPI.GWL_EXSTYLE);
            WindowsAPI.SetWindowLong(hWnd, WindowsAPI.GWL_EXSTYLE, exStyle | WindowsAPI.WS_EX_TOOLWINDOW);

            return window;
        }
        
        public static void KillLiveCaptions()
        {
            KillAllProcessesByName(PROCESS_NAME);
        }

        private static void KillAllProcessesByName(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
                return;
            foreach (Process process in processes)
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        private static AutomationElement FindWindowByPID(int processId)
        {
            var condition = new PropertyCondition(AutomationElement.ProcessIdProperty, processId);
            return AutomationElement.RootElement.FindFirst(TreeScope.Children, condition);
        }
    }
}
