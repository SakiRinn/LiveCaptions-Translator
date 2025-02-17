using System.Diagnostics;
using System.Windows.Automation;
using System.Collections.Concurrent;  // 添加这一行
using System.Threading;              // 用于 Thread.Sleep
using System;                        // 用于 Math, IntPtr 等

namespace LiveCaptionsTranslator.models
{
    public static class LiveCaptionsHandler
    {
        public static readonly string PROCESS_NAME = "LiveCaptions";

        public static AutomationElement LaunchLiveCaptions()
        {
            // Init
            KillAllProcessesByPName(PROCESS_NAME);
            var process = Process.Start(PROCESS_NAME);

            // Search window with exponential backoff
            AutomationElement? window = null;
            int maxAttempts = 20;
            int attemptCount = 0;
            int baseDelay = 50; // Start with 50ms delay

            while ((window == null || window.Current.ClassName.CompareTo("LiveCaptionsDesktopWindow") != 0) 
                   && attemptCount < maxAttempts)
            {
                window = FindWindowByPId(process.Id);
                if (window == null)
                {
                    int delay = baseDelay * (int)Math.Pow(2, attemptCount);
                    Thread.Sleep(Math.Min(delay, 1000)); // Cap at 1 second
                    attemptCount++;
                }
            }

            if (window == null)
                throw new Exception("Failed to launch Live Captions window!");

            // Hide window
            IntPtr hWnd = new IntPtr((long)window.Current.NativeWindowHandle);
            WindowsAPI.ShowWindow(hWnd, WindowsAPI.SW_MINIMIZE);
            int exStyle = WindowsAPI.GetWindowLong(hWnd, WindowsAPI.GWL_EXSTYLE);
            WindowsAPI.SetWindowLong(hWnd, WindowsAPI.GWL_EXSTYLE, exStyle | WindowsAPI.WS_EX_TOOLWINDOW);

            return window;
        }

        public static void KillLiveCaptions()
        {
            KillAllProcessesByPName(PROCESS_NAME);
        }

        private static void KillAllProcessesByPName(string processName)
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

        private static AutomationElement FindWindowByPId(int processId)
        {
            var condition = new PropertyCondition(AutomationElement.ProcessIdProperty, processId);
            return AutomationElement.RootElement.FindFirst(TreeScope.Children, condition);
        }

        private static readonly ConcurrentDictionary<string, AutomationElement> _elementCache = 
            new ConcurrentDictionary<string, AutomationElement>();

        public static AutomationElement? FindElementByAId(AutomationElement window, string automationId)
        {
            // Try to get from cache first
            if (_elementCache.TryGetValue(automationId, out var cachedElement))
            {
                try
                {
                    // Verify the element is still valid
                    var _ = cachedElement.Current.AutomationId;
                    return cachedElement;
                }
                catch
                {
                    _elementCache.TryRemove(automationId, out _);
                }
            }

            // Use a more efficient search with Condition
            var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
            var element = window.FindFirst(TreeScope.Descendants, condition);
            
            if (element != null)
            {
                _elementCache.TryAdd(automationId, element);
            }
            
            return element;
        }

        public static void PrintAllElementsAId(AutomationElement window)
        {
            var treeWalker = TreeWalker.RawViewWalker;
            var stack = new Stack<AutomationElement>();
            stack.Push(window);

            while (stack.Count > 0)
            {
                var element = stack.Pop();
                if (!string.IsNullOrEmpty(element.Current.AutomationId))
                    Console.WriteLine(element.Current.AutomationId);

                var child = treeWalker.GetFirstChild(element);
                while (child != null)
                {
                    stack.Push(child);
                    child = treeWalker.GetNextSibling(child);
                }
            }
        }

        public static bool ClickSettingsButton(AutomationElement window)
        {
            var settingsButton = FindElementByAId(window, "SettingsButton");
            if (settingsButton != null)
            {
                var invokePattern = settingsButton.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                if (invokePattern != null)
                {
                    invokePattern.Invoke();
                    return true;
                }
            }
            return false;
        }
    }
}
