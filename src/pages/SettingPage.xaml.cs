using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Wpf.Ui.Appearance;

using LiveCaptionsTranslator.apis;
using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.utils;
using Wpf.Ui.Controls;

namespace LiveCaptionsTranslator
{
    public partial class SettingPage : Page
    {
        private static SettingWindow? SettingWindow;
        private bool _suppressUiLanguageSelectionChanged;

        // Cache non-null references to avoid repeated null-dereference warnings.
        private readonly Setting _setting;
        private readonly Caption _caption;

        public SettingPage()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();

            // Ensure we always have usable instances for bindings and event handlers.
            _setting = Translator.Setting ?? Setting.Load();
            _caption = Translator.Caption ?? Caption.GetInstance();

            DataContext = _setting;

            Loaded += (s, e) =>
            {
                var mw = App.Current.MainWindow as MainWindow;
                if (mw is not null)
                    mw.AutoHeightAdjust(maxHeight: (int)mw.MinHeight);

                CheckForFirstUse();

                InitializeUILanguageSelection();
            };

            // Keep selection in sync when user returns to this page.
            IsVisibleChanged += (_, __) => SyncUILanguageSelectionFromSetting();

            TranslateAPIBox.ItemsSource = _setting.Configs.Keys;
            TranslateAPIBox.SelectedIndex = 0;

            LoadAPISetting();
        }

        public void SyncUILanguageSelectionFromSetting()
        {
            if (!IsLoaded)
                return;

            _suppressUiLanguageSelectionChanged = true;
            try
            {
                var current = _setting.UiLanguageFileName;
                UILanguageBox.SelectedIndex = current?.ToLowerInvariant() switch
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
                _suppressUiLanguageSelectionChanged = false;
            }
        }

        private void LiveCaptionsButton_click(object sender, RoutedEventArgs e)
        {
            if (Translator.Window == null)
                return;

            bool isHide = Translator.Window.Current.BoundingRectangle == Rect.Empty;
            if (isHide)
            {
                LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);
                ButtonText.Text = TryFindResource("S45") as string ?? "Hide";
            }
            else
            {
                LiveCaptionsHandler.HideLiveCaptions(Translator.Window);
                ButtonText.Text = TryFindResource("S46") as string ?? "Show";
            }
        }

        private void TranslateAPIBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAPISetting();
        }

        private void TargetLangBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TargetLangBox.SelectedItem != null)
                _setting.TargetLanguage = TargetLangBox.SelectedItem.ToString() ?? _setting.TargetLanguage;
        }

        private void TargetLangBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _setting.TargetLanguage = TargetLangBox.Text;
        }

        private void APISettingButton_click(object sender, RoutedEventArgs e)
        {
            if (SettingWindow != null && SettingWindow.IsLoaded)
                SettingWindow.Activate();
            else
            {
                SettingWindow = new SettingWindow();
                SettingWindow.Closed += (sender, args) => SettingWindow = null;
                SettingWindow.Show();
            }
        }

        private void Contexts_ValueChanged(object sender, NumberBoxValueChangedEventArgs args)
        {
            if (_setting.DisplaySentences > _setting.NumContexts)
                _setting.DisplaySentences = _setting.NumContexts;
        }

        private void DisplaySentences_ValueChanged(object sender, NumberBoxValueChangedEventArgs args)
        {
            if (_setting.DisplaySentences > _setting.NumContexts)
                _setting.NumContexts = _setting.DisplaySentences;

            _caption.OnPropertyChanged("DisplayLogCards");
            _caption.OnPropertyChanged("OverlayPreviousTranslation");
        }

        private void LiveCaptionsInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            LiveCaptionsInfoFlyout.Show();
        }

        private void LiveCaptionsInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            LiveCaptionsInfoFlyout.Hide();
        }

        private void FrequencyInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            FrequencyInfoFlyout.Show();
        }

        private void FrequencyInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            FrequencyInfoFlyout.Hide();
        }

        private void TranslateAPIInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            TranslateAPIInfoFlyout.Show();
        }

        private void TranslateAPIInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            TranslateAPIInfoFlyout.Hide();
        }

        private void TargetLangInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            TargetLangInfoFlyout.Show();
        }

        private void TargetLangInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            TargetLangInfoFlyout.Hide();
        }

        private void CaptionLogMaxInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            CaptionLogMaxInfoFlyout.Show();
        }

        private void CaptionLogMaxInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            CaptionLogMaxInfoFlyout.Hide();
        }

        private void ContextAwareInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            ContextAwareInfoFlyout.Show();
        }

        private void ContextAwareInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            ContextAwareInfoFlyout.Hide();
        }

        private void UILanguageInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            UILanguageInfoFlyout.Show();
        }

        private void UILanguageInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            UILanguageInfoFlyout.Hide();
        }

        private void CheckForFirstUse()
        {
            if (Translator.FirstUseFlag)
                ButtonText.Text = TryFindResource("S45") as string ?? "Hide";
        }

        public void LoadAPISetting()
        {
            // Guard against unavailable configs.
            var apiName = _setting.ApiName;
            if (!_setting.Configs.ContainsKey(apiName))
                return;

            var configType = _setting[apiName].GetType();
            var languagesProp = configType.GetProperty(
                "SupportedLanguages", BindingFlags.Public | BindingFlags.Static);

            // Traverse base classes to find `SupportedLanguages`
            while (configType != null && languagesProp == null)
            {
                configType = configType.BaseType;
                if (configType != null)
                    languagesProp = configType.GetProperty(
                        "SupportedLanguages", BindingFlags.Public | BindingFlags.Static);
            }

            languagesProp ??= typeof(TranslateAPIConfig).GetProperty(
                "SupportedLanguages", BindingFlags.Public | BindingFlags.Static);

            if (languagesProp?.GetValue(null) is not Dictionary<string, string> supportedLanguages)
                return;

            TargetLangBox.ItemsSource = supportedLanguages.Keys;

            string targetLang = _setting.TargetLanguage;
            if (!supportedLanguages.ContainsKey(targetLang))
                supportedLanguages[targetLang] = targetLang; // add custom language

            TargetLangBox.SelectedItem = targetLang;
        }

        private void UILanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressUiLanguageSelectionChanged || !IsLoaded)
                return;

            string fileName = UILanguageBox.SelectedIndex switch
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

            _setting.UiLanguageFileName = fileName;

            // Sync other open windows' language dropdowns.
            foreach (Window w in Application.Current.Windows)
            {
                if (w is WelcomeWindow ww)
                    ww.SyncLanguageSelectionFromSetting();
            }

            var content = Content;
            Content = null;
            Content = content;

            RequestAutoFitWidth();
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

                if (src.Contains($"localization/{fileName}", StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            if (index is null)
                throw new IOException($"Cannot locate resource 'localization/{fileName}'.");

            var target = merged[index.Value];
            merged.RemoveAt(index.Value);
            merged.Add(target);
        }

        private void InitializeUILanguageSelection()
        {
            _suppressUiLanguageSelectionChanged = true;
            try
            {
                var current = _setting.UiLanguageFileName;
                if (string.IsNullOrWhiteSpace(current))
                    current = GetActiveLocalizationFileName();

                UILanguageBox.SelectedIndex = current.ToLowerInvariant() switch
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

                ActivateLocalizationDictionary(current);
                ApplyFlowDirectionForLocalization(current);
            }
            finally
            {
                _suppressUiLanguageSelectionChanged = false;
            }
        }

        private static string GetActiveLocalizationFileName()
        {
            for (int i = Application.Current.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                var src = Application.Current.Resources.MergedDictionaries[i].Source?.ToString();
                if (src is null)
                    continue;

                if (src.Contains("localization/zh-cn.xaml", StringComparison.OrdinalIgnoreCase))
                    return "zh-cn.xaml";
                if (src.Contains("localization/ar.xaml", StringComparison.OrdinalIgnoreCase))
                    return "ar.xaml";
                if (src.Contains("localization/bn.xaml", StringComparison.OrdinalIgnoreCase))
                    return "bn.xaml";

                if (src.Contains("localization/zh-tw.xaml", StringComparison.OrdinalIgnoreCase))
                    return "zh-tw.xaml";
                if (src.Contains("localization/cs-cz.xaml", StringComparison.OrdinalIgnoreCase))
                    return "cs-cz.xaml";
                if (src.Contains("localization/de-de.xaml", StringComparison.OrdinalIgnoreCase))
                    return "de-de.xaml";
                if (src.Contains("localization/es-mx.xaml", StringComparison.OrdinalIgnoreCase))
                    return "es-mx.xaml";
                if (src.Contains("localization/fr-fr.xaml", StringComparison.OrdinalIgnoreCase))
                    return "fr-fr.xaml";
                if (src.Contains("localization/it-it.xaml", StringComparison.OrdinalIgnoreCase))
                    return "it-it.xaml";
                if (src.Contains("localization/ja-jp.xaml", StringComparison.OrdinalIgnoreCase))
                    return "ja-jp.xaml";
                if (src.Contains("localization/ko-kr.xaml", StringComparison.OrdinalIgnoreCase))
                    return "ko-kr.xaml";
                if (src.Contains("localization/lt-lt.xaml", StringComparison.OrdinalIgnoreCase))
                    return "lt-lt.xaml";
                if (src.Contains("localization/nl-nl.xaml", StringComparison.OrdinalIgnoreCase))
                    return "nl-nl.xaml";
                if (src.Contains("localization/pl-pl.xaml", StringComparison.OrdinalIgnoreCase))
                    return "pl-pl.xaml";
                if (src.Contains("localization/pt-br.xaml", StringComparison.OrdinalIgnoreCase))
                    return "pt-br.xaml";
                if (src.Contains("localization/pt-pt.xaml", StringComparison.OrdinalIgnoreCase))
                    return "pt-pt.xaml";
                if (src.Contains("localization/ru-ru.xaml", StringComparison.OrdinalIgnoreCase))
                    return "ru-ru.xaml";
                if (src.Contains("localization/sv-se.xaml", StringComparison.OrdinalIgnoreCase))
                    return "sv-se.xaml";
                if (src.Contains("localization/tr-tr.xaml", StringComparison.OrdinalIgnoreCase))
                    return "tr-tr.xaml";
                if (src.Contains("localization/vi-vn.xaml", StringComparison.OrdinalIgnoreCase))
                    return "vi-vn.xaml";

                if (src.Contains("localization/en-us.xaml", StringComparison.OrdinalIgnoreCase))
                    return "en-us.xaml";
            }

            return "en-us.xaml";
        }

        private static void ApplyFlowDirectionForLocalization(string fileName)
        {
            bool isArabic = string.Equals(fileName, "ar.xaml", StringComparison.OrdinalIgnoreCase);

            if (Application.Current.MainWindow is FrameworkElement fe)
            {
                fe.FlowDirection = isArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                fe.Language = XmlLanguage.GetLanguage(isArabic ? "ar" : "en");
            }

            // Overlay window stays LTR.
            if ((Application.Current.MainWindow as MainWindow)?.OverlayWindow is { } overlay)
            {
                if (overlay.Content is FrameworkElement overlayContent)
                {
                    overlayContent.FlowDirection = FlowDirection.LeftToRight;
                    overlayContent.Language = XmlLanguage.GetLanguage(isArabic ? "ar" : "en");
                }
            }
        }

        /// <summary>
        /// Re-measure SettingPage content and auto-fit the MainWindow width.
        /// Intended to be called after localization changes initiated outside this page (e.g., WelcomeWindow).
        /// </summary>
        public void RequestAutoFitWidth()
        {
            // Auto-size main window width to fit SettingPage content.
            if (Application.Current.MainWindow is MainWindow mw)
            {
                mw.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var prev = mw.SizeToContent;
                    mw.SizeToContent = SizeToContent.Width;
                    mw.UpdateLayout();

                    const double minW = 860;
                    const double maxW = 1200;
                    mw.Width = Math.Max(minW, Math.Min(maxW, mw.ActualWidth));

                    mw.SizeToContent = prev;
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }
    }
}