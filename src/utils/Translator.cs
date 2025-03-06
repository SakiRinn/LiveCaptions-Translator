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
                Stopwatch? sw = null;
                if (App.Settings.Latency)
                {
                    sw = Stopwatch.StartNew();
                }

                translatedText = await TranslateAPI.TranslateFunc(text, token);

                if (sw != null)
                {
                    sw.Stop();
                    translatedText = $"[{sw.ElapsedMilliseconds} ms] " + translatedText;
                }
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