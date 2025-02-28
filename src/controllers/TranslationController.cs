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
    }
}