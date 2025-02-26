using LiveCaptionsTranslator.models;
using static System.Net.Mime.MediaTypeNames;

namespace LiveCaptionsTranslator.controllers
{
    public class TranslationController
    {
        public static event Action? TranslationLogged;

        public async Task<string> Translate(string text)
        {
            string translatedText;
            try
            {
                translatedText = await TranslateAPI.TranslateFunc(text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Translation failed: {ex.Message}");
                return $"[Translation Failed] {ex.Message}";
            }
            return translatedText;
        }

        public async Task Log(string originalText, string translatedText, bool isOverWrite = false)
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

        public async Task LogOnly(string originalText)
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