using System.Windows.Automation;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using LiveCaptionsTranslator.controllers;

namespace LiveCaptionsTranslator.models
{
    public class Caption : INotifyPropertyChanged
    {
        private static Caption? instance = null;
        public event PropertyChangedEventHandler? PropertyChanged;

        private static readonly char[] PUNC_EOS = ".?!。？！".ToCharArray();
        private static readonly char[] PUNC_COMMA = ",，、—\n".ToCharArray();

        private string displayOriginalCaption = "";
        private string displayTranslatedCaption = "";

        private readonly Queue<CaptionLogItem> captionLog = new(6);
        public class CaptionLogItem
        {
            public string OriginalCaptionLog { get; set; }
            public string TranslatedCaptionLog { get; set; }
        }
        public IEnumerable<CaptionLogItem> CaptionHistory => captionLog.Reverse();
        public static event Action? TranslationLogged;
        public bool TranslateFlag { get; set; } = false;
        public bool EOSFlag { get; set; } = false;
        public bool LogOnlyFlag { get; set; } = false;

        public string OriginalCaption { get; set; } = "";
        public string TranslatedCaption { get; set; } = "";
        public string DisplayOriginalCaption
        {
            get => displayOriginalCaption;
            set
            {
                displayOriginalCaption = value;
                OnPropertyChanged("DisplayOriginalCaption");
            }
        }
        public string DisplayTranslatedCaption
        {
            get => displayTranslatedCaption;
            set
            {
                displayTranslatedCaption = value;
                OnPropertyChanged("DisplayTranslatedCaption");
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
            string originalLatest = "originalLatest";
            string captionLatest = "";

            while (true)
            {
                if (App.Window == null)
                {
                    Thread.Sleep(2000);
                    continue;
                }

                // Is caption textbox change to another sentence
                bool captionTrim = false;
                // Get the text recognized by LiveCaptions.
                string fullText = string.Empty;
                try
                {
                    fullText = GetCaptions(App.Window);         // about 10-20ms
                }
                catch (ElementNotAvailableException ex)
                {
                    App.Window = null;
                    continue;
                }
                if (string.IsNullOrEmpty(fullText))
                    continue;

                // Note: For certain languages (such as Japanese), LiveCaptions excessively uses `\n`.
                // Preprocess - Remove redundant `\n` around punctuation.
                fullText = Regex.Replace(fullText, @"\s*([.!?,])\s*", "$1 ");
                fullText = Regex.Replace(fullText, @"\s*([。！？，、])\s*", "$1");
                // Preprocess - Replace redundant `\n` within sentences with comma or period.
                fullText = ReplaceNewlines(fullText, 32);

                // Get the last sentence.
                int lastEOSIndex;
                if (Array.IndexOf(PUNC_EOS, fullText[^1]) != -1)
                    lastEOSIndex = fullText[0..^1].LastIndexOfAny(PUNC_EOS);
                else
                    lastEOSIndex = fullText.LastIndexOfAny(PUNC_EOS);
                string latestCaption = fullText.Substring(lastEOSIndex + 1);


                // DisplayOriginalCaption: The sentence to be displayed to the user.
                if (DisplayOriginalCaption.CompareTo(latestCaption) != 0)
                {
                    DisplayOriginalCaption = latestCaption;
                    // If the last sentence is too short, extend it by adding the previous sentence when displayed.
                    if (lastEOSIndex > 0 && Encoding.UTF8.GetByteCount(latestCaption) < 12)
                    {
                        captionTrim = true;
                        lastEOSIndex = fullText[0..lastEOSIndex].LastIndexOfAny(PUNC_EOS);
                        DisplayOriginalCaption = fullText.Substring(lastEOSIndex + 1);
                    }
                    // If the last sentence is too long, truncate it when displayed.
                    DisplayOriginalCaption = ShortenDisplaySentence(DisplayOriginalCaption, 160);
                }

                // OriginalCaption: The sentence to be really translated.
                if (OriginalCaption.CompareTo(latestCaption) != 0)
                {
                    OriginalCaption = latestCaption;

                    idleCount = 0;
                    if (Encoding.UTF8.GetByteCount(latestCaption) >= 10)
                        syncCount++;

                    if (Array.IndexOf(PUNC_EOS, OriginalCaption[^1]) != -1)
                    {
                        syncCount = 0;
                        TranslateFlag = true;
                        EOSFlag = true;
                        captionTrim = true;
                    }
                    else
                        EOSFlag = false;

                    if (DisplayOriginalCaption != OriginalCaption)
                        captionTrim = true;

                    // Push current caption to history without waiting for async
                    if (captionTrim)
                    {
                        string oc = captionLatest;
                        string _oc = StringTrim(oc, 3, oc.Length - 3);
                        string _ol = StringTrim(originalLatest, 3, originalLatest.Length - 3);
                        if (_oc != _ol) // Prevent from spamming
                        {
                            originalLatest = oc;
                            var historyTask = Task.Run(() => HistoryCapture(oc));
                        }
                    }
                    else
                        captionLatest = OriginalCaption;
                }
                else
                    idleCount++;

                // `TranslateFlag` determines whether this sentence should be translated.
                // When `OriginalCaption` remains unchanged, `idleCount` +1; when `OriginalCaption` changes, `MaxSyncInterval` +1.
                if (syncCount > App.Settings.MaxSyncInterval ||
                    idleCount == App.Settings.MaxIdleInterval)
                {
                    syncCount = 0;
                    TranslateFlag = true;
                }
                Thread.Sleep(25);
            }
        }

        private string StringTrim(string text, int start, int length)
        {
            try
            {
                return text.Substring(start, length).ToLower();
            }
            catch
            {
                return text.ToLower();
            }
        }

        private async Task HistoryCapture(string original)
        {
            string unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            string translated = "[Paused]";
            string targetLanguage = App.Settings.TargetLanguage;
            string apiName = App.Settings.ApiName;
            bool captionLog = App.Settings.CaptionLogEnable;

            // Insert history database
            try
            {
                if (LogOnlyFlag) // Log only mode no translate
                {
                    SQLiteHistoryLogger.LogTranslation(unixTime, original, "N/A", "N/A", "LogOnly");
                }
                else
                {
                    translated = await TranslationController.Translate(original);
                    SQLiteHistoryLogger.LogTranslation(unixTime, original, translated, targetLanguage, apiName);
                }
                TranslationLogged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Logging history failed: {ex.Message}");
            }

            // Add caption log card
            if (captionLog)
            {
                if (this.captionLog.Count >= App.Settings.CaptionLogMax + 1)
                    this.captionLog.Dequeue();
                this.captionLog.Enqueue(new CaptionLogItem
                {
                    OriginalCaptionLog = original,
                    TranslatedCaptionLog = translated
                });
                OnPropertyChanged(nameof(CaptionHistory));
            }
        }

        public void ClearCaptionLog()
        {
            captionLog.Clear();
            OnPropertyChanged(nameof(CaptionHistory));
        }

        public async Task Translate()
        {
            while (true)
            {
                if (App.Window == null)
                {
                    App.Window = LiveCaptionsHandler.LaunchLiveCaptions();
                    DisplayTranslatedCaption = "[WARNING] LiveCaptions was unexpectedly closed, restarting...";
                }

                if (TranslateFlag)
                {
                    var originalSnapshot = OriginalCaption;

                    // Log Only
                    if (LogOnlyFlag)
                    {
                        // Do not translate
                        TranslatedCaption = string.Empty;
                        DisplayTranslatedCaption = "[Paused]";
                    }
                    else
                    {
                        // Translate and display
                        TranslatedCaption = await TranslationController.Translate(originalSnapshot);
                        DisplayTranslatedCaption = ShortenDisplaySentence(TranslatedCaption, 240);
                    }

                    TranslateFlag = false;
                    if (EOSFlag)
                        Thread.Sleep(600);
                }
                Thread.Sleep(40);
            }
        }

        public static string GetCaptions(AutomationElement window)
        {
            var captionsTextBlock = LiveCaptionsHandler.FindElementByAId(window, "CaptionsTextBlock");
            if (captionsTextBlock == null)
                return string.Empty;
            return captionsTextBlock.Current.Name;
        }

        private static string ShortenDisplaySentence(string displaySentence, int maxByteLength)
        {
            while (Encoding.UTF8.GetByteCount(displaySentence) >= maxByteLength)
            {
                int commaIndex = displaySentence.IndexOfAny(PUNC_COMMA);
                if (commaIndex < 0 || commaIndex + 1 >= displaySentence.Length)
                    break;
                displaySentence = displaySentence.Substring(commaIndex + 1);
            }
            return displaySentence;
        }

        private static string ReplaceNewlines(string text, int byteThreshold)
        {
            string[] splits = text.Split('\n');
            for (int i = 0; i < splits.Length; i++)
            {
                splits[i] = splits[i].Trim();
                if (i == splits.Length - 1)
                    continue;

                char lastChar = splits[i][^1];
                bool isCJ = (lastChar >= '\u4E00' && lastChar <= '\u9FFF') ||
                            (lastChar >= '\u3400' && lastChar <= '\u4DBF') ||
                            (lastChar >= '\u3040' && lastChar <= '\u30FF');
                bool isKorean = (lastChar >= '\uAC00' && lastChar <= '\uD7AF');

                if (Encoding.UTF8.GetByteCount(splits[i]) >= byteThreshold)
                    splits[i] += isCJ && !isKorean ? "。" : ". ";
                else
                    splits[i] += isCJ && !isKorean ? "——" : "—";
            }
            return string.Join("", splits);
        }
    }
}
