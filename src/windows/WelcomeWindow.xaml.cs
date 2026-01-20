using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;

namespace LiveCaptionsTranslator
{
    public partial class WelcomeWindow : FluentWindow
    {
        private bool _suppressLanguageSelectionChanged;

        public WelcomeWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();

            Loaded += (s, e) =>
            {
                SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
                InitializeLanguageSelection();

                // Auto-size on first show to match current localization (startup).
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var prev = SizeToContent;
                    SizeToContent = SizeToContent.WidthAndHeight;
                    UpdateLayout();

                    const double minW = 650;
                    const double maxW = 980;
                    const double minH = 420;
                    const double maxH = 800;

                    Width = Math.Max(minW, Math.Min(maxW, ActualWidth));
                    Height = Math.Max(minH, Math.Min(maxH, ActualHeight));

                    SizeToContent = prev;
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            };

            Closed += (_, __) =>
            {
                // No-op: language is persisted immediately when user changes the dropdown.
            };
        }

        public void SyncLanguageSelectionFromSetting()
        {
            if (!IsLoaded)
                return;

            _suppressLanguageSelectionChanged = true;
            try
            {
                var current = Translator.Setting?.UiLanguageFileName;
                WelcomeLanguageBox.SelectedIndex = current?.ToLowerInvariant() switch
                {
                    "zh-cn.xaml" => 1,
                    "ar.xaml" => 2,
                    "bn.xaml" => 3,
                    "cs-cz.xaml" => 4,
                    "de-de.xaml" => 5,
                    "es-mx.xaml" => 6,
                    "fr-fr.xaml" => 7,
                    "it-it.xaml" => 8,
                    "ja-jp.xaml" => 9,
                    "ko-kr.xaml" => 10,
                    "lt-lt.xaml" => 11,
                    "nl-nl.xaml" => 12,
                    "pl-pl.xaml" => 13,
                    "pt-br.xaml" => 14,
                    "pt-pt.xaml" => 15,
                    "ru-ru.xaml" => 16,
                    "sv-se.xaml" => 17,
                    "tr-tr.xaml" => 18,
                    "vi-vn.xaml" => 19,
                    "zh-tw.xaml" => 20,
                    _ => 0
                };
            }
            finally
            {
                _suppressLanguageSelectionChanged = false;
            }
        }

        private void PersistCurrentUiLanguage()
        {
            var fileName = WelcomeLanguageBox.SelectedIndex switch
            {
                1 => "zh-cn.xaml",
                2 => "ar.xaml",
                3 => "bn.xaml",
                4 => "cs-cz.xaml",
                5 => "de-de.xaml",
                6 => "es-mx.xaml",
                7 => "fr-fr.xaml",
                8 => "it-it.xaml",
                9 => "ja-jp.xaml",
                10 => "ko-kr.xaml",
                11 => "lt-lt.xaml",
                12 => "nl-nl.xaml",
                13 => "pl-pl.xaml",
                14 => "pt-br.xaml",
                15 => "pt-pt.xaml",
                16 => "ru-ru.xaml",
                17 => "sv-se.xaml",
                18 => "tr-tr.xaml",
                19 => "vi-vn.xaml",
                20 => "zh-tw.xaml",
                _ => "en-us.xaml"
            };

            if (Translator.Setting is not null)
                Translator.Setting.UiLanguageFileName = fileName;
        }

        private void InitializeLanguageSelection()
        {
            _suppressLanguageSelectionChanged = true;
            try
            {
                var current = Translator.Setting?.UiLanguageFileName;
                if (string.IsNullOrWhiteSpace(current))
                    current = GetActiveLocalizationFileName();

                // Ensure current dictionary is active for this window
                ActivateLocalizationDictionary(current);

                WelcomeLanguageBox.SelectedIndex = current.ToLowerInvariant() switch
                {
                    "zh-cn.xaml" => 1,
                    "ar.xaml" => 2,
                    "bn.xaml" => 3,
                    "cs-cz.xaml" => 4,
                    "de-de.xaml" => 5,
                    "es-mx.xaml" => 6,
                    "fr-fr.xaml" => 7,
                    "it-it.xaml" => 8,
                    "ja-jp.xaml" => 9,
                    "ko-kr.xaml" => 10,
                    "lt-lt.xaml" => 11,
                    "nl-nl.xaml" => 12,
                    "pl-pl.xaml" => 13,
                    "pt-br.xaml" => 14,
                    "pt-pt.xaml" => 15,
                    "ru-ru.xaml" => 16,
                    "sv-se.xaml" => 17,
                    "tr-tr.xaml" => 18,
                    "vi-vn.xaml" => 19,
                    "zh-tw.xaml" => 20,
                    _ => 0
                };
            }
            finally
            {
                _suppressLanguageSelectionChanged = false;
            }
        }

        private void WelcomeLanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressLanguageSelectionChanged || !IsLoaded)
                return;

            string fileName = WelcomeLanguageBox.SelectedIndex switch
            {
                1 => "zh-cn.xaml",
                2 => "ar.xaml",
                3 => "bn.xaml",
                4 => "cs-cz.xaml",
                5 => "de-de.xaml",
                6 => "es-mx.xaml",
                7 => "fr-fr.xaml",
                8 => "it-it.xaml",
                9 => "ja-jp.xaml",
                10 => "ko-kr.xaml",
                11 => "lt-lt.xaml",
                12 => "nl-nl.xaml",
                13 => "pl-pl.xaml",
                14 => "pt-br.xaml",
                15 => "pt-pt.xaml",
                16 => "ru-ru.xaml",
                17 => "sv-se.xaml",
                18 => "tr-tr.xaml",
                19 => "vi-vn.xaml",
                20 => "zh-tw.xaml",
                _ => "en-us.xaml"
            };

            ActivateLocalizationDictionary(fileName);
            ApplyFlowDirectionForLocalization(fileName);

            if (Translator.Setting is not null)
                Translator.Setting.UiLanguageFileName = fileName;

            // Sync SettingPage dropdown if it exists anywhere in open windows.
            foreach (Window w in Application.Current.Windows)
            {
                if (w is not FrameworkElement root)
                    continue;

                var sp = FindDescendant<SettingPage>(root);
                if (sp is null)
                    continue;

                sp.SyncUILanguageSelectionFromSetting();
                // Also request SettingPage/MainWindow to re-measure and auto-fit width.
                sp.RequestAutoFitWidth();
            }

            // Refresh visual tree so DynamicResource re-evaluates for this window.
            var content = Content;
            Content = null;
            Content = content;

            // Auto-size like SettingPage: measure both width and height and clamp.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var prev = SizeToContent;
                SizeToContent = SizeToContent.WidthAndHeight;
                UpdateLayout();

                // Clamp to avoid extreme resize.
                const double minW = 650;
                const double maxW = 980;
                const double minH = 420;
                const double maxH = 800;

                Width = Math.Max(minW, Math.Min(maxW, ActualWidth));
                Height = Math.Max(minH, Math.Min(maxH, ActualHeight));

                SizeToContent = prev;
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
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

        private static void ActivateLocalizationDictionary(string fileName)
        {
            var merged = Application.Current.Resources.MergedDictionaries;

            int? index = null;
            for (int i = 0; i < merged.Count; i++)
            {
                var src = merged[i].Source?.ToString();
                if (src is null)
                    continue;

                if (src.Contains($"localization/{fileName}", System.StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            if (index is null)
                throw new System.IO.IOException($"Cannot locate resource 'localization/{fileName}'.");

            var target = merged[index.Value];
            merged.RemoveAt(index.Value);
            merged.Add(target);
        }

        private static string GetActiveLocalizationFileName()
        {
            for (int i = Application.Current.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                var src = Application.Current.Resources.MergedDictionaries[i].Source?.ToString();
                if (src is null)
                    continue;

                if (src.Contains("localization/zh-cn.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "zh-cn.xaml";
                if (src.Contains("localization/ar.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "ar.xaml";
                if (src.Contains("localization/bn.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "bn.xaml";

                if (src.Contains("localization/zh-tw.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "zh-tw.xaml";
                if (src.Contains("localization/cs-cz.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "cs-cz.xaml";
                if (src.Contains("localization/de-de.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "de-de.xaml";
                if (src.Contains("localization/es-mx.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "es-mx.xaml";
                if (src.Contains("localization/fr-fr.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "fr-fr.xaml";
                if (src.Contains("localization/it-it.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "it-it.xaml";
                if (src.Contains("localization/ja-jp.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "ja-jp.xaml";
                if (src.Contains("localization/ko-kr.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "ko-kr.xaml";
                if (src.Contains("localization/lt-lt.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "lt-lt.xaml";
                if (src.Contains("localization/nl-nl.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "nl-nl.xaml";
                if (src.Contains("localization/pl-pl.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "pl-pl.xaml";
                if (src.Contains("localization/pt-br.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "pt-br.xaml";
                if (src.Contains("localization/pt-pt.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "pt-pt.xaml";
                if (src.Contains("localization/ru-ru.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "ru-ru.xaml";
                if (src.Contains("localization/sv-se.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "sv-se.xaml";
                if (src.Contains("localization/tr-tr.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "tr-tr.xaml";
                if (src.Contains("localization/vi-vn.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "vi-vn.xaml";

                if (src.Contains("localization/en-us.xaml", System.StringComparison.OrdinalIgnoreCase))
                    return "en-us.xaml";
            }

            return "en-us.xaml";
        }

        private static void ApplyFlowDirectionForLocalization(string fileName)
        {
            bool isArabic = string.Equals(fileName, "ar.xaml", System.StringComparison.OrdinalIgnoreCase);

            // Apply to all open windows (including FluentWindow title bars) as requested.
            foreach (Window w in Application.Current.Windows)
            {
                if (w is FrameworkElement fe)
                {
                    fe.FlowDirection = isArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                    fe.Language = System.Windows.Markup.XmlLanguage.GetLanguage(isArabic ? "ar" : "en");
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}

