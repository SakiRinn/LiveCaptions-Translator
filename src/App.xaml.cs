using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator
{
    public partial class App : Application
    {
        public static AutomationElement Window { get; } = LaunchLiveCaptions();
        public static Caption Captions { get; } = Caption.GetInstance();

        App()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            Task.Run(() => Captions?.Sync(Window));
            Task.Run(() => Captions?.Translate());
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
