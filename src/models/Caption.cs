using System.Windows.Automation;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using LiveCaptionsTranslator.utils;
using System.Diagnostics;

namespace LiveCaptionsTranslator.models
{
    public class Caption : INotifyPropertyChanged
    {
        private static Caption? instance = null;
        public event PropertyChangedEventHandler? PropertyChanged;

        private string displayOriginalCaption = "";
        private string displayTranslatedCaption = "";

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

        public bool TranslateFlag { get; set; } = false;
        public bool LogOnlyFlag { get; set; } = false;

        public class CaptionLogItem
        {
            public required string SourceText { get; set; }
            public required string TranslatedText { get; set; }
        }
        public Queue<CaptionLogItem> LogCards { get; } = new(6);
        public IEnumerable<CaptionLogItem> DisplayLogCards => LogCards.Reverse();

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
            int previousEOSIndex = -1;
            string previousHistory = "--";
            string previousCaption = string.Empty;

            while (true)
            {
                if (App.Window == null)
                {
                    Thread.Sleep(2000);
                    continue;
                }

                // Get the text recognized by LiveCaptions.
                string fullText = string.Empty;
                try
                {
                    fullText = LiveCaptionsHandler.GetCaptions(App.Window);     // 10-20ms
                }
                catch (ElementNotAvailableException)
                {
                    App.Window = null;
                    continue;
                }
                if (string.IsNullOrEmpty(fullText))
                    continue;

                // Note: For certain languages (such as Japanese), LiveCaptions excessively uses `\n`.
                // Preprocess - remove the `.` between 2 uppercase letters.
                fullText = Regex.Replace(fullText, @"(?<=[A-Z])\s*\.\s*(?=[A-Z])", "");
                // Preprocess - Remove redundant `\n` around punctuation.
                fullText = Regex.Replace(fullText, @"\s*([.!?,])\s*", "$1 ");
                fullText = Regex.Replace(fullText, @"\s*([。！？，、])\s*", "$1");
                // Preprocess - Replace redundant `\n` within sentences with comma or period.
                fullText = TextUtil.ReplaceNewlines(fullText, 32);

                // Get the last sentence.
                int lastEOSIndex;
                if (Array.IndexOf(TextUtil.PUNC_EOS, fullText[^1]) != -1)
                    lastEOSIndex = fullText[0..^1].LastIndexOfAny(TextUtil.PUNC_EOS);
                else
                    lastEOSIndex = fullText.LastIndexOfAny(TextUtil.PUNC_EOS);
                string latestCaption = fullText.Substring(lastEOSIndex + 1);

                // DisplayOriginalCaption: The sentence to be displayed to the user.
                if (DisplayOriginalCaption.CompareTo(latestCaption) != 0)
                {
                    string caption = latestCaption;
                    // If the last sentence is too short, extend it by adding the previous sentence when displayed.
                    if (lastEOSIndex > 0 && Encoding.UTF8.GetByteCount(caption) < 12)
                    {
                        lastEOSIndex = fullText[0..lastEOSIndex].LastIndexOfAny(TextUtil.PUNC_EOS);
                        caption = fullText.Substring(lastEOSIndex + 1);
                    }
                    // If the last sentence is too long, truncate it when displayed.
                    DisplayOriginalCaption = TextUtil.ShortenDisplaySentence(caption, 160);
                }

                // If the sentence changerd, push previous DisplayOriginalCaption to history handler
                if (previousEOSIndex != lastEOSIndex)
                {
                    previousEOSIndex = lastEOSIndex;

                    int eosIndex = previousCaption.LastIndexOfAny(TextUtil.PUNC_EOS) + 1;
                    string caption = previousCaption.Substring(0, eosIndex);
                    if (!string.IsNullOrEmpty(caption))
                    {
                        if (previousHistory.CompareTo(caption) != 0) // Prevent from spamming logging
                        {
                            previousHistory = caption;
                            Task.Run(() => HistoryAdd(caption)); // Spawn a new thread to push DisplayOriginalCaption to async function
                        }
                    }
                }
                else // Keep storing previous sentence
                    previousCaption = DisplayOriginalCaption;

                // OriginalCaption: The sentence to be really translated.
                if (OriginalCaption.CompareTo(latestCaption) != 0)
                {
                    OriginalCaption = latestCaption;

                    idleCount = 0;
                    if (Encoding.UTF8.GetByteCount(latestCaption) >= 10)
                        syncCount++;
                    if (Array.IndexOf(TextUtil.PUNC_EOS, OriginalCaption[^1]) != -1)
                    {
                        syncCount = 0;
                        TranslateFlag = true;
                    }
                }
                else
                    idleCount++;

                // `TranslateFlag` determines whether this sentence should be translated.
                // When `OriginalCaption` remains unchanged, `idleCount` +1; when `OriginalCaption` changes, `MaxSyncInterval` +1.
                if (syncCount > App.Setting.MaxSyncInterval ||
                    idleCount == App.Setting.MaxIdleInterval)
                {
                    syncCount = 0;
                    TranslateFlag = true;
                }
                Thread.Sleep(25);
            }
        }

        public async Task Translate()
        {
            var translationTaskQueue = new TranslationTaskQueue();
            while (true)
            {
                if (App.Window == null)
                {
                    DisplayTranslatedCaption = "[WARNING] LiveCaptions was unexpectedly closed, restarting...";
                    App.Window = LiveCaptionsHandler.LaunchLiveCaptions();
                    DisplayTranslatedCaption = "";
                }
                else if (LogOnlyFlag)
                {
                    TranslatedCaption = string.Empty;
                    DisplayTranslatedCaption = "[Paused]";
                }
                else if (!string.IsNullOrEmpty(translationTaskQueue.Output))
                {
                    TranslatedCaption = translationTaskQueue.Output;
                    DisplayTranslatedCaption = TextUtil.ShortenDisplaySentence(TranslatedCaption, 200);
                }

                if (TranslateFlag)
                {
                    var originalSnapshot = OriginalCaption;

                    if (!LogOnlyFlag)
                    {
                        translationTaskQueue.Enqueue(token => Task.Run(
                            () => Translator.Translate(OriginalCaption, token), token)
                        , originalSnapshot);
                    }

                    TranslateFlag = false;
                    // If the original sentence is a complete sentence, pause for better visual experience.
                    if (Array.IndexOf(TextUtil.PUNC_EOS, originalSnapshot[^1]) != -1)
                        Thread.Sleep(600);
                }
                Thread.Sleep(40);
            }
        }

        private async Task HistoryAdd(string original)
        {
            string unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            string translated = "[Paused]";
            string targetLanguage = App.Setting.TargetLanguage;
            string apiName = App.Setting.ApiName;
            bool captionLog = App.Setting.MainWindow.CaptionLogEnabled;

            // Add history to sqlite
            try
            {
                if (LogOnlyFlag) // Log only mode, don't translate
                {
                    SQLiteHistoryLogger.LogTranslation(unixTime, original, "N/A", "N/A", "LogOnly");
                }
                else
                {
                    // Translate the full sentence again due to tick of Task Translate() and TranslateFlag make it lack of translated
                    translated = await Translator.Translate(original);
                    SQLiteHistoryLogger.LogTranslation(unixTime, original, translated, targetLanguage, apiName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Logging history failed: {ex.Message}");
            }

            // Add caption log card
            if (captionLog)
            {
                if (LogCards.Count >= App.Setting?.MainWindow.CaptionLogMax)
                    LogCards.Dequeue();
                LogCards.Enqueue(new CaptionLogItem
                {
                    SourceText = original,
                    TranslatedText = translated
                });
                OnPropertyChanged("DisplayLogCards");
            }
        }
    }
}