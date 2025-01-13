using System.Windows.Automation;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using LiveCaptionsTranslator.controllers;

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

        public bool PauseFlag { get; set; } = false;
        public bool TranslateFlag { get; set; } = false;
        private bool EOSFlag { get; set; } = false;

        public string Original
        {
            get => original;
            set
            {
                original = value;
                OnPropertyChanged("Original");
            }
        }
        public string Translated
        {
            get => translated;
            set
            {
                translated = value;
                OnPropertyChanged("Translated");
            }
        }

        private Caption() { }

        public static Caption GetInstance()
        {
            if (instance != null)
                return instance;
            instance = new Caption();
            return instance;
        }

        public void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public void Sync()
        {
            int idleCount = 0;
            int syncCount = 0;

            while (true)
            {
                if (PauseFlag || App.Window == null)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                string fullText = GetCaptions(App.Window).Trim();
                if (string.IsNullOrEmpty(fullText))
                    continue;
                foreach (char eos in PUNC_EOS)
                    fullText = fullText.Replace($"{eos}\n", $"{eos}");

                int lastEOSIndex;
                if (Array.IndexOf(PUNC_EOS, fullText[^1]) != -1)
                    lastEOSIndex = fullText[0..^1].LastIndexOfAny(PUNC_EOS);
                else
                    lastEOSIndex = fullText.LastIndexOfAny(PUNC_EOS);
                string latestCaption = fullText.Substring(lastEOSIndex + 1);

                while (lastEOSIndex > 0 && Encoding.UTF8.GetByteCount(latestCaption) < 15)
                {
                    lastEOSIndex = fullText[0..lastEOSIndex].LastIndexOfAny(PUNC_EOS);
                    latestCaption = fullText.Substring(lastEOSIndex + 1);
                }

                while (Encoding.UTF8.GetByteCount(latestCaption) > 150)
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

                if (syncCount > App.Settings.MaxSyncInterval || 
                    idleCount == App.Settings.MaxIdleInterval)
                {
                    syncCount = 0;
                    TranslateFlag = true;
                }
                Thread.Sleep(50);
            }
        }

        public async Task Translate()
        {
            var controller = new TranslationController();
            while (true)
            {
                for (int pauseCount = 0; PauseFlag; pauseCount++)
                {
                    if (pauseCount > 60 && App.Window != null)
                    {
                        App.Window = null;
                        LiveCaptionsHandler.KillLiveCaptions();
                    }
                    Thread.Sleep(1000);
                }

                if (TranslateFlag)
                {
                    Translated = await controller.TranslateAndLogAsync(Original);
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
