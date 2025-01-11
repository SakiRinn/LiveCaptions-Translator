using System;
using System.Threading.Tasks;
using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.controllers
{
    public class TranslationController
    {
        public static event Action? TranslationLogged;
        public async Task<string> TranslateAndLogAsync(string text)
        {
            try
            {
                string translatedText = await TranslateAsync(text);
                if (string.IsNullOrEmpty(translatedText) || translatedText.Contains("[Translation Failed]"))
                {
                    return text;
                }
                return translatedText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Translation error: {ex.Message}");
                return text;
            }
        }

        private async Task<string> TranslateAsync(string text)
        {
            try
            {
                string targetLanguage = App.Settings.TargetLanguage;
                string apiName = App.Settings.ApiName;

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
                
                if (!string.IsNullOrEmpty(translatedText))
                {
                    try
                    {
                        await SQLiteHistoryLogger.LogTranslationAsync(text, translatedText, targetLanguage, apiName);
                        TranslationLogged?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error] Logging history failed: {ex.Message}");
                    }
                }

                return translatedText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Translation API error: {ex.Message}");
                return text;
            }
        }
    }
}