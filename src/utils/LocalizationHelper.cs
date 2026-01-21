using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.utils
{
    internal static class LocalizationHelper
    {
        // Derive supported file list from LanguageCatalog to keep a single source of truth
        internal static readonly IReadOnlyList<string> SupportedFiles = LanguageCatalog.SupportedUiLanguages
            .Select(l => l.CultureCode + ".xaml")
            .ToList();

        private static readonly Dictionary<string, int> FileToIndex = SupportedFiles
            .Select((f, i) => new { f, i })
            .ToDictionary(x => x.f, x => x.i, StringComparer.OrdinalIgnoreCase);

        internal static int GetComboIndexFromFileName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return 0;

            return FileToIndex.TryGetValue(fileName.Trim(), out var idx) ? idx : 0;
        }

        internal static string GetFileNameFromComboIndex(int selectedIndex)
        {
            if (selectedIndex < 0 || selectedIndex >= SupportedFiles.Count)
                return SupportedFiles[0];

            return SupportedFiles[selectedIndex];
        }

        internal static void ActivateLocalizationDictionary(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "en-us.xaml";

            var merged = Application.Current.Resources.MergedDictionaries;

            // Ensure fallback en-us is present somewhere
            int? enIndex = null;
            for (int i = 0; i < merged.Count; i++)
            {
                var src = merged[i].Source?.ToString();
                if (src is null)
                    continue;
                if (src.Contains("localization/en-us.xaml", StringComparison.OrdinalIgnoreCase))
                {
                    enIndex = i;
                    break;
                }
            }
            if (enIndex is null)
            {
                merged.Add(new ResourceDictionary
                {
                    Source = new Uri("localization/en-us.xaml", UriKind.Relative)
                });
            }

            // Locate the target language dictionary if already merged
            int? langIndex = null;
            for (int i = 0; i < merged.Count; i++)
            {
                var src = merged[i].Source?.ToString();
                if (src is null)
                    continue;

                if (src.Contains($"localization/{fileName}", StringComparison.OrdinalIgnoreCase))
                {
                    langIndex = i;
                    break;
                }
            }

            // If selecting en-us, just make sure it's last so it takes precedence
            if (fileName.Equals("en-us.xaml", StringComparison.OrdinalIgnoreCase))
            {
                // Move en-us to the end
                int? findEn = null;
                for (int i = 0; i < merged.Count; i++)
                {
                    var src = merged[i].Source?.ToString();
                    if (src is null)
                        continue;
                    if (src.Contains("localization/en-us.xaml", StringComparison.OrdinalIgnoreCase))
                    {
                        findEn = i;
                        break;
                    }
                }
                if (findEn is not null)
                {
                    var enDict = merged[findEn.Value];
                    merged.RemoveAt(findEn.Value);
                    merged.Add(enDict);
                }
                return;
            }

            // Add or move the selected dictionary to be the LAST one, so it overrides fallback
            if (langIndex is not null)
            {
                var dict = merged[langIndex.Value];
                merged.RemoveAt(langIndex.Value);
                merged.Add(dict);
            }
            else
            {
                merged.Add(new ResourceDictionary
                {
                    Source = new Uri($"localization/{fileName}", UriKind.Relative)
                });
            }
        }

        internal static string GetActiveLocalizationFileName()
        {
            for (int i = Application.Current.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                var src = Application.Current.Resources.MergedDictionaries[i].Source?.ToString();
                if (src is null)
                    continue;

                foreach (var file in SupportedFiles)
                {
                    if (src.Contains($"localization/{file}", StringComparison.OrdinalIgnoreCase))
                        return file;
                }
            }

            return SupportedFiles[0];
        }

        internal static void ApplyFlowDirectionForLocalization(string? fileName, bool keepOverlayWindowLtr)
        {
            bool isArabic = string.Equals(fileName, "ar.xaml", StringComparison.OrdinalIgnoreCase);

            if (keepOverlayWindowLtr)
            {
                // Apply to MainWindow
                if (Application.Current.MainWindow is FrameworkElement fe)
                {
                    fe.FlowDirection = isArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                    fe.Language = XmlLanguage.GetLanguage(isArabic ? "ar" : "en");
                }

                // Ensure OverlayWindow content stays LTR
                if ((Application.Current.MainWindow as MainWindow)?.OverlayWindow is { } overlay)
                {
                    if (overlay.Content is FrameworkElement overlayContent)
                    {
                        overlayContent.FlowDirection = FlowDirection.LeftToRight;
                        overlayContent.Language = XmlLanguage.GetLanguage(isArabic ? "ar" : "en");
                    }
                }

                // Also apply to WelcomeWindow if present to mirror MainWindow behavior
                foreach (Window w in Application.Current.Windows)
                {
                    if (w is WelcomeWindow ww)
                    {
                        ww.FlowDirection = isArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                        if (ww.Content is FrameworkElement wwc)
                        {
                            wwc.Language = XmlLanguage.GetLanguage(isArabic ? "ar" : "en");
                        }
                    }
                }

                return;
            }

            // Apply globally to all open windows
            foreach (Window w in Application.Current.Windows)
            {
                if (w is FrameworkElement fe)
                {
                    fe.FlowDirection = isArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                    fe.Language = XmlLanguage.GetLanguage(isArabic ? "ar" : "en");
                }
            }
        }

        internal static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                if (child is T typed)
                    return typed;

                var found = FindDescendant<T>(child);
                if (found is not null)
                    return found;
            }

            return null;
        }
    }
}
