using System.Diagnostics;

using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.utils
{
    public static class Translator
    {
        public static event Action? TranslationLogged;

        public static async Task<string> Translate(string text, CancellationToken token = default)
        {
            string translatedText;
            try
            {
#if DEBUG
                var sw = Stopwatch.StartNew();
#endif
                translatedText = await TranslateAPI.TranslateFunc(text, token);
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