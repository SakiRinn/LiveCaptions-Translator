using System.Windows.Automation;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LiveCaptionsTranslator
{
    class Caption : INotifyPropertyChanged
    {
        public static int translationInterval = 2;

        private static readonly char[] PUNC_EOS = ".?!。？！".ToCharArray();
        private static readonly char[] PUNC_COMMA = ",，、—\n".ToCharArray();

        private string _original;
        private string _translated;

        public string Original
        {
            get => _original;
            set
            {
                _original = value;
                OnPerpertyChanged("Original");
            }
        }

        public string Translated
        {
            get => _translated;
            set
            {
                _translated = value;
                OnPerpertyChanged("Translated");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPerpertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public async Task Translate(AutomationElement window)
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

                    this.Original = caption;
                    this.Translated = translatedCaption;
                }
                else
                {
                    wait_count++;
                    if (wait_count == 10)
                    {
                        translatedCaption = await TranslateAPI.OpenAI(caption);
                        idle_count = 0;

                        this.Original = caption;
                        this.Translated = translatedCaption;
                    }
                }
                Thread.Sleep(50);
            }
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