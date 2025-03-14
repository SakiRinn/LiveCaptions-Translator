using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.utils;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Automation;

namespace LiveCaptionsTranslator
{
    public static class AppMain
    {
        public const int SHORT_THRESHOLD = 12;
        public const int MEDIUM_THRESHOLD = 32;
        public const int LONG_THRESHOLD = 160;
        public const int VERYLONG_THRESHOLD = 200;
        
        public static bool LogOnlyFlag { get; set; } = false;
        
        private static readonly Queue<string> pendingTextQueue = new();

        public static void Sync()
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
                    // Check LiveCaptions.exe still alive
                    var info = App.Window.Current;
                    var name = info.Name;

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
                fullText = TextUtil.ReplaceNewlines(fullText, MEDIUM_THRESHOLD);

                // Get the last sentence.
                int lastEOSIndex;
                if (Array.IndexOf(TextUtil.PUNC_EOS, fullText[^1]) != -1)
                    lastEOSIndex = fullText[0..^1].LastIndexOfAny(TextUtil.PUNC_EOS);
                else
                    lastEOSIndex = fullText.LastIndexOfAny(TextUtil.PUNC_EOS);
                string latestCaption = fullText.Substring(lastEOSIndex + 1);
                
                // If the last sentence is too short, extend it by adding the previous sentence.
                // Note: Expand `lastestCaption` instead of `DisplayOriginalCaption`,
                // because LiveCaptions may generate multiple characters including EOS at once.
                if (lastEOSIndex > 0 && Encoding.UTF8.GetByteCount(latestCaption) < SHORT_THRESHOLD)
                {
                    lastEOSIndex = fullText[0..lastEOSIndex].LastIndexOfAny(TextUtil.PUNC_EOS);
                    latestCaption = fullText.Substring(lastEOSIndex + 1);
                }

                // `DisplayOriginalCaption`: The sentence to be displayed to the user.
                if (App.Caption.DisplayOriginalCaption.CompareTo(latestCaption) != 0)
                {
                    App.Caption.DisplayOriginalCaption = latestCaption;
                    // If the last sentence is too long, truncate it when displayed.
                    App.Caption.DisplayOriginalCaption = 
                        TextUtil.ShortenDisplaySentence(App.Caption.DisplayOriginalCaption, LONG_THRESHOLD);
                }

                // Prepare for `OriginalCaption`. If Expanded, only retain the complete sentence.
                int lastEOS = latestCaption.LastIndexOfAny(TextUtil.PUNC_EOS);
                if (lastEOS != -1)
                    latestCaption = latestCaption.Substring(0, lastEOS + 1);
                
                // `OriginalCaption`: The sentence to be really translated.
                if (App.Caption.OriginalCaption.CompareTo(latestCaption) != 0)
                {
                    App.Caption.OriginalCaption = latestCaption;
                    
                    idleCount = 0;
                    if (Array.IndexOf(TextUtil.PUNC_EOS, App.Caption.OriginalCaption[^1]) != -1)
                    {
                        syncCount = 0;
                        pendingTextQueue.Enqueue(App.Caption.OriginalCaption);
                    }
                    else if (Encoding.UTF8.GetByteCount(App.Caption.OriginalCaption) >= SHORT_THRESHOLD)
                        syncCount++;
                }
                else
                    idleCount++;

                // `TranslateFlag` determines whether this sentence should be translated.
                // When `OriginalCaption` remains unchanged, `idleCount` +1; when `OriginalCaption` changes, `MaxSyncInterval` +1.
                if (syncCount > App.Setting.MaxSyncInterval ||
                    idleCount == App.Setting.MaxIdleInterval)
                {
                    syncCount = 0;
                    pendingTextQueue.Enqueue(App.Caption.OriginalCaption);
                }
                Thread.Sleep(25);
            }
        }

        public static async Task Translate()
        {

            var translationTaskQueue = new TranslationTaskQueue();

            while (true)
            {
                if (App.Window == null)
                {
                    App.Caption.DisplayTranslatedCaption = "[WARNING] LiveCaptions was unexpectedly closed, restarting...";
                    App.Window = LiveCaptionsHandler.LaunchLiveCaptions();
                    App.Caption.DisplayTranslatedCaption = "";
                }

                if (pendingTextQueue.Count > 0)
                {
                    var originalSnapshot = pendingTextQueue.Dequeue();

                    if (LogOnlyFlag)
                    {
                        bool isOverwrite = await Translator.IsOverwrite(originalSnapshot);
                        await Translator.LogOnly(originalSnapshot, isOverwrite);
                    }
                    else
                    {
                        translationTaskQueue.Enqueue(token => Task.Run(
                            () => Translator.Translate(originalSnapshot, token), token)
                        , originalSnapshot);
                    }

                    if (LogOnlyFlag)
                    {
                        App.Caption.TranslatedCaption = string.Empty;
                        App.Caption.DisplayTranslatedCaption = "[Paused]";
                    }
                    else if (!string.IsNullOrEmpty(translationTaskQueue.Output))
                    {
                        App.Caption.TranslatedCaption = translationTaskQueue.Output;
                        App.Caption.DisplayTranslatedCaption = 
                            TextUtil.ShortenDisplaySentence(App.Caption.TranslatedCaption, VERYLONG_THRESHOLD);
                    }

                    // If the original sentence is a complete sentence, pause for better visual experience.
                    if (Array.IndexOf(TextUtil.PUNC_EOS, originalSnapshot[^1]) != -1)
                        Thread.Sleep(600);
                }
                Thread.Sleep(40);
            }
        }
    }
}
