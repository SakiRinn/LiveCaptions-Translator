using System.Windows.Automation;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using LiveCaptionsTranslator.utils;

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

        public bool TranslateFlag { get; set; } = false;
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
                // Preprocess - remove the `.` between 2 uppercase letters.
                fullText = Regex.Replace(fullText, @"(?<=[A-Z])\s*\.\s*(?=[A-Z])", "");
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
                    }
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
                } else if (LogOnlyFlag)
                {
                    TranslatedCaption = string.Empty;
                    DisplayTranslatedCaption = "[Paused]";
                } else if (!string.IsNullOrEmpty(translationTaskQueue.Output))
                {
                    TranslatedCaption = translationTaskQueue.Output;
                    DisplayTranslatedCaption = ShortenDisplaySentence(TranslatedCaption, 200);
                }

                if (TranslateFlag)
                {
                    var originalSnapshot = OriginalCaption;

                    // If the old sentence is the prefix of the new sentence,
                    // overwrite the previous entry when logging.
                    string lastLoggedOriginal = await SQLiteHistoryLogger.LoadLatestSourceText();
                    bool isOverWrite = !string.IsNullOrEmpty(lastLoggedOriginal)
                        && originalSnapshot.StartsWith(lastLoggedOriginal);

                    if (LogOnlyFlag)
                    {
                        var LogOnlyTask = Task.Run(
                            () => Translator.LogOnly(originalSnapshot, isOverWrite)
                        );
                    }
                    else
                    {
                        translationTaskQueue.Enqueue(token => Task.Run(() =>
                        {
                            var TranslateTask = Translator.Translate(OriginalCaption, token);
                            var LogTask = Translator.Log(
                                originalSnapshot, TranslateTask.Result, App.Settings, isOverWrite, token);
                            return TranslateTask;
                        }));
                    }

                    TranslateFlag = false;
                    // If the original sentence is a complete sentence, pause for better visual experience.
                    if (Array.IndexOf(PUNC_EOS, originalSnapshot[^1]) != -1)
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
