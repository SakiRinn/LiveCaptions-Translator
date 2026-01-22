using System.Windows;

using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class App : Application
    {
        App()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            // Ensure UI language is applied on startup.
            ApplyPersistedUiLanguage();

            Translator.Setting?.Save();

            Task.Run(() => Translator.SyncLoop());
            Task.Run(() => Translator.TranslateLoop());
            Task.Run(() => Translator.DisplayLoop());
        }

        private static void ApplyPersistedUiLanguage()
        {
            var fileName = Translator.Setting?.UiLanguageFileName;
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "en-us.xaml";

            var merged = Current.Resources.MergedDictionaries;

            // Always keep en-us as the LAST dictionary.
            // Any incomplete language dictionaries should be placed BEFORE en-us
            // so missing keys automatically fall back to en-us.
            var enIndex = FindDictionaryIndex(merged, "localization/en-us.xaml");
            if (enIndex is null)
                return; // app.xaml must include en-us

            // Move selected language dictionary just BEFORE en-us (not after).
            var langIndex = FindDictionaryIndex(merged, $"localization/{fileName}");
            if (langIndex is null)
                return; // unknown language dictionary not merged

            // If selected is en-us, just ensure it's last.
            if (fileName.Equals("en-us.xaml", StringComparison.OrdinalIgnoreCase))
            {
                var en = merged[enIndex.Value];
                merged.RemoveAt(enIndex.Value);
                merged.Add(en);
                return;
            }

            // Recompute enIndex if it shifts.
            enIndex = FindDictionaryIndex(merged, "localization/en-us.xaml");
            if (enIndex is null)
                return;

            var lang = merged[langIndex.Value];
            merged.RemoveAt(langIndex.Value);

            // Recompute enIndex again after removal.
            enIndex = FindDictionaryIndex(merged, "localization/en-us.xaml");
            if (enIndex is null)
                return;

            merged.Insert(enIndex.Value, lang);
        }

        private static int? FindDictionaryIndex(System.Collections.Generic.IList<ResourceDictionary> merged, string contains)
        {
            for (int i = 0; i < merged.Count; i++)
            {
                var src = merged[i].Source?.ToString();
                if (src is null)
                    continue;

                if (src.Contains(contains, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return null;
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            if (Translator.Window != null)
            {
                LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);
                LiveCaptionsHandler.KillLiveCaptions(Translator.Window);
            }
        }
    }
}
