using LiveCaptionsTranslator.models;
using System.Diagnostics;

namespace LiveCaptionsTranslator.controllers
{
    public static class TranslationController
    {
        public static event Action? TranslationLogged;

        public static async Task<string> Translate(string text)
        {
            string translatedText;
            try
            {
#if DEBUG
                var sw = Stopwatch.StartNew();
#endif
                translatedText = await TranslateAPI.TranslateFunc(text);
#if DEBUG
                sw.Stop();
                translatedText = $"[{sw.ElapsedMilliseconds} ms] " + translatedText;
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Translation failed: {ex.Message}");
                return $"[Translation Failed] {ex.Message}";
            }
            return translatedText;
        }

        public static async Task Log(string originalText, string translatedText, bool isOverWrite = false)
        {
            string targetLanguage = App.Settings.TargetLanguage;
            string apiName = App.Settings.ApiName;

            try
            {
                if (isOverWrite)
                    await SQLiteHistoryLogger.DeleteLatestTranslation();
                await SQLiteHistoryLogger.LogTranslation(originalText, translatedText, targetLanguage, apiName);
                TranslationLogged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Logging history failed: {ex.Message}");
            }
        }

        public static async Task LogOnly(string originalText)
        {
            try
            {
                await SQLiteHistoryLogger.LogTranslation(originalText, "N/A", "N/A", "LogOnly");
                TranslationLogged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Logging history failed: {ex.Message}");
            }
        }
    }
}