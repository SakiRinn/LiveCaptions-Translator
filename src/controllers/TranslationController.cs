using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.controllers
{
    public class TranslationController
    {
        public static event Action? TranslationLogged;

        public async Task<string> TranslateAndLog(string text, bool doLog = true)
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

            return translatedText;
        }

        public async Task<string> Translate(string text)
        {
            return await TranslateAndLog(text, false);
        }
    }
}