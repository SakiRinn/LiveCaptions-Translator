using System.Windows.Automation;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LiveCaptionsTranslator.models
{
    public class Caption : INotifyPropertyChanged
    {
        private static Caption? instance = null;
        public event PropertyChangedEventHandler? PropertyChanged;

        private static readonly char[] PUNC_EOS = ".?!。？！".ToCharArray();
        private static readonly char[] PUNC_COMMA = ",，、—\n".ToCharArray();

        private string original = "";
        private string translated = "";

        private int maxIdleInterval;
        private int maxSyncInterval;

        public bool PauseFlag { get; set; } = false;
        public bool TranslateFlag { get; set; } = false;
        private bool EOSFlag { get; set; } = false;

        public string Original
        {
            get => original;
            set
            {
                original = value;
                OnPerpertyChanged("Original");
            }
        }
        public string Translated
        {
            get => translated;
            set
            {
                translated = value;
                OnPerpertyChanged("Translated");
            }
        }

        private Caption()
        {
            maxIdleInterval = 10;
            maxSyncInterval = 5;
        }

        private Caption(int maxIdleInterval, int maxSyncInterval)
        {
            this.maxIdleInterval = maxIdleInterval;
            this.maxSyncInterval = maxSyncInterval;
        }

        public static Caption GetInstance()
        {
            if (instance != null)
                return instance;
            instance = new Caption();
            return instance;
        }

        public static Caption GetInstance(int maxIdleInterval, int maxSyncInterval)
        {
            if (instance != null)
                return instance;
            instance = new Caption(maxIdleInterval, maxSyncInterval);
            return instance;
        }

        public void OnPerpertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public void Sync(AutomationElement window)
        {
            int idleCount = 0;
            int syncCount = 0;

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

                if (Original.CompareTo(latestCaption) != 0)
                {
                    idleCount = 0;
                    syncCount++;
                    Original = latestCaption;

                    if (Array.IndexOf(PUNC_EOS, latestCaption[^1]) != -1 || 
                        Array.IndexOf(PUNC_COMMA, latestCaption[^1]) != -1)
                    {
                        syncCount = 0;
                        TranslateFlag = true;
                        EOSFlag = true;
                    }
                    else
                        EOSFlag = false;
                }
                else
                    idleCount++;

                if (idleCount == maxIdleInterval || syncCount > maxSyncInterval)
                {
                    syncCount = 0;
                    TranslateFlag = true;
                }
                Thread.Sleep(50);
            }
        }

        public async Task Translate()
        {
            while (true)
            {
                if (PauseFlag)
                {
                    Thread.Sleep(1000);
                    continue;
                }
                if (TranslateFlag)
                {
                    Translated = await TranslateAPI.OpenAI(Original);
                    TranslateFlag = false;
                    if (EOSFlag)
                        Thread.Sleep(1000);
                }
                Thread.Sleep(50);
            }
        }

        public static string GetCaptions(AutomationElement window)
        {
            var captionsTextBlock = LiveCaptionsHandler.FindElementByAId(window, "CaptionsTextBlock");
            if (captionsTextBlock == null)
                return string.Empty;
            return captionsTextBlock.Current.Name;
        }
    }
}
