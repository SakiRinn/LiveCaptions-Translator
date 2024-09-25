using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;

namespace LiveCaptionsTranslator
{
    public partial class App : Application
    {
        private static AutomationElement? _window = null;

        public static AutomationElement Window
        {
            get => _window;
            set => _window = value;
        }

        App()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            App.Window = LaunchLiveCaptions();
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            KillAllProcessesByName(WindowsAPI.PROCESS_NAME);
        }

        static void KillAllProcessesByName(string processName)
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

        static AutomationElement FindWindowByPID(int processId)
        {
            var condition = new PropertyCondition(AutomationElement.ProcessIdProperty, processId);
            return AutomationElement.RootElement.FindFirst(TreeScope.Children, condition);
        }

        static AutomationElement LaunchLiveCaptions()
        {
            // Init
            KillAllProcessesByName(WindowsAPI.PROCESS_NAME);
            var process = Process.Start(WindowsAPI.PROCESS_NAME);
            AutomationElement windowElement = FindWindowByPID(process.Id);

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
            WindowsAPI.SetWindowLong(hWnd, WindowsAPI.GWL_EXSTYLE,
                exStyle | WindowsAPI.WS_EX_TOOLWINDOW);

            return window;
        }
    }
}
