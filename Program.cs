using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Text;

namespace LiveCaptionsTranslator
{
    class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int SW_MINIMIZE = 6;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        private static readonly char[] PUNC_EOS = ".?!。？！".ToCharArray();
        private static readonly char[] PUNC_COMMA = ",，、—\n".ToCharArray();
        private static readonly string PROCESS_NAME = "LiveCaptions";

        public static int translationInterval = 2;

        public static async Task Main()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            var window = LaunchLiveCaptions();
            await Translate(window);
        }

        static AutomationElement LaunchLiveCaptions()
        {
            // Init
            KillAllProcessesByName(PROCESS_NAME);
            var process = Process.Start(PROCESS_NAME);
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
            ShowWindow(hWnd, SW_MINIMIZE);
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            SetWindowLong(hWnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

            return window;
        }

        static async Task Translate(AutomationElement window)
        {
            string translatedCaption = "";
            string caption = "";
            int wait_count = 0;
            int idle_count = 0;
            while (true)
            {
                string fullText = GetCaptions(window).Trim();
                if (string.IsNullOrEmpty(fullText))
                    continue;
                foreach (char eos in PUNC_EOS)
                    fullText = fullText.Replace($"{eos}\n", $"{eos}");

                int lastEOSIndex = -1;
                for (int i = fullText.Length; i > 0; i--)
                {
                    if (Array.IndexOf(PUNC_EOS, fullText[i - 1]) == -1)
                    {
                        lastEOSIndex = fullText[0..i].LastIndexOfAny(PUNC_EOS);
                        break;
                    }
                }

                string latestCaption = fullText.Substring(lastEOSIndex + 1);
                while (Encoding.UTF8.GetByteCount(latestCaption) > 100)
                {
                    int commaIndex = latestCaption.IndexOfAny(PUNC_COMMA);
                    if (commaIndex < 0 || commaIndex + 1 == latestCaption.Length)
                        break;
                    latestCaption = latestCaption.Substring(commaIndex + 1);
                }
                latestCaption = latestCaption.Replace("\n", "——");

                if (caption.CompareTo(latestCaption) != 0)
                {
                    wait_count = 0;
                    caption = latestCaption;
                    if (idle_count > translationInterval || Array.IndexOf(PUNC_EOS, caption[^1]) != -1 ||
                        Array.IndexOf(PUNC_COMMA, caption[^1]) != -1)
                    {
                        translatedCaption = await TranslateAPI.OpenAI(caption);
                        idle_count = 0;
                    }
                    else
                        idle_count++;

                    Console.Clear();
                    Console.WriteLine($"{caption}");
                    Console.WriteLine($"[Translated] {translatedCaption}");
                }
                else
                {
                    wait_count++;
                    if (wait_count == 10)
                    {
                        translatedCaption = await TranslateAPI.OpenAI(caption);
                        idle_count = 0;

                        Console.Clear();
                        Console.WriteLine($"{caption}");
                        Console.WriteLine($"[Translated] {translatedCaption}");
                    }
                }
                Thread.Sleep(50);
            }
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            KillAllProcessesByName(PROCESS_NAME);
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

        static string GetCaptions(AutomationElement window)
        {
            var treeWalker = TreeWalker.RawViewWalker;
            return GetCaptions(treeWalker, window);
        }

        static string GetCaptions(TreeWalker walker, AutomationElement window)
        {
            var stack = new Stack<AutomationElement>();
            stack.Push(window);

            while (stack.Count > 0)
            {
                var element = stack.Pop();
                if (element.Current.AutomationId.CompareTo("CaptionsTextBlock") == 0)
                    return element.Current.Name;

                var child = walker.GetFirstChild(element);
                while (child != null)
                {
                    stack.Push(child);
                    child = walker.GetNextSibling(child);
                }
            }
            return string.Empty;
        }
    }
}