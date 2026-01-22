using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Appearance;

using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.utils;
using Wpf.Ui.Controls;

namespace LiveCaptionsTranslator
{
    public partial class SettingPage : Page
    {
        private static SettingWindow? SettingWindow;
        private bool _suppressUiLanguageSelectionChanged;
        private bool _autoFitWidthDone; // ensure width auto-fit runs once at startup

        public SettingPage()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
            DataContext = Translator.Setting;

            Loaded += (s, e) =>
            {
                (App.Current.MainWindow as MainWindow)?.AutoHeightAdjust(maxHeight: (int)App.Current.MainWindow.MinHeight);
                CheckForFirstUse();
                InitializeUILanguageSelection();

                if (!_autoFitWidthDone)
                {
                    _autoFitWidthDone = true;
                    RequestAutoFitWidth();
                }
            };

            IsVisibleChanged += (_, __) => SyncUILanguageSelectionFromSetting();

            TranslateAPIBox.ItemsSource = Translator.Setting?.Configs.Keys;
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
                UILanguageBox.SelectedIndex = LocalizationHelper.GetComboIndexFromFileName(Translator.Setting?.UiLanguageFileName);
            }
            finally
            {
                _suppressUiLanguageSelectionChanged = false;
            }
        }

        private void InitializeUILanguageSelection()
        {
            _suppressUiLanguageSelectionChanged = true;
            try
            {
                var current = Translator.Setting?.UiLanguageFileName;
                if (string.IsNullOrWhiteSpace(current))
                    current = LocalizationHelper.GetActiveLocalizationFileName();

                UILanguageBox.SelectedIndex = LocalizationHelper.GetComboIndexFromFileName(current);
                LocalizationHelper.ActivateLocalizationDictionary(current);
                LocalizationHelper.ApplyFlowDirectionForLocalization(current, keepOverlayWindowLtr: true);
            }
            finally
            {
                _suppressUiLanguageSelectionChanged = false;
            }
        }

        private void UILanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressUiLanguageSelectionChanged || !IsLoaded)
                return;

            string fileName = LocalizationHelper.GetFileNameFromComboIndex(UILanguageBox.SelectedIndex);

            LocalizationHelper.ActivateLocalizationDictionary(fileName);
            LocalizationHelper.ApplyFlowDirectionForLocalization(fileName, keepOverlayWindowLtr: true);

            if (Translator.Setting is not null)
                Translator.Setting.UiLanguageFileName = fileName;

            foreach (Window w in Application.Current.Windows)
            {
                if (w is WelcomeWindow ww)
                {
                    ww.SyncLanguageSelectionFromSetting();
                    ww.RequestAutoFitHeight(); // trigger welcome window height auto-fit on setting change
                }
            }

            var content = Content;
            Content = null;
            Content = content;

            RequestAutoFitWidth();
        }

        public void RequestAutoFitWidth()
        {
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

        private void LiveCaptionsButton_click(object sender, RoutedEventArgs e)
        {
            if (Translator.Window == null)
                return;

            var button = sender as Wpf.Ui.Controls.Button;
            var text = ButtonText.Text;

            bool isHide = Translator.Window.Current.BoundingRectangle == Rect.Empty;
            if (isHide)
            {
                LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);
                ButtonText.Text = (string)Application.Current.TryFindResource("SettingPage_LiveCaptionsToggle_Hide");
            }
            else
            {
                LiveCaptionsHandler.HideLiveCaptions(Translator.Window);
                ButtonText.Text = (string)Application.Current.TryFindResource("SettingPage_LiveCaptionsToggle_Show");
            }
        }

        private void TranslateAPIBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAPISetting();
        }

        private void TargetLangBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TargetLangBox.SelectedItem != null)
                Translator.Setting.TargetLanguage = TargetLangBox.SelectedItem.ToString();
        }

        private void TargetLangBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Translator.Setting.TargetLanguage = TargetLangBox.Text;
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
            if (Translator.Setting.DisplaySentences > Translator.Setting.NumContexts)
                Translator.Setting.DisplaySentences = Translator.Setting.NumContexts;
        }

        private void DisplaySentences_ValueChanged(object sender, NumberBoxValueChangedEventArgs args)
        {
            if (Translator.Setting.DisplaySentences > Translator.Setting.NumContexts)
                Translator.Setting.NumContexts = Translator.Setting.DisplaySentences;
            Translator.Caption.OnPropertyChanged("DisplayLogCards");
            Translator.Caption.OnPropertyChanged("OverlayPreviousTranslation");
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
                ButtonText.Text = (string)Application.Current.TryFindResource("SettingPage_LiveCaptionsToggle_Hide");
        }

        public void LoadAPISetting()
        {
            var configType = Translator.Setting[Translator.Setting.ApiName].GetType();
            var languagesProp = configType.GetProperty(
                "SupportedLanguages", BindingFlags.Public | BindingFlags.Static);

            while (configType != null && languagesProp == null)
            {
                configType = configType.BaseType;
                languagesProp = configType.GetProperty(
                    "SupportedLanguages", BindingFlags.Public | BindingFlags.Static);
            }
            if (languagesProp == null)
                languagesProp = typeof(TranslateAPIConfig).GetProperty(
                    "SupportedLanguages", BindingFlags.Public | BindingFlags.Static);

            var supportedLanguages = (Dictionary<string, string>)languagesProp.GetValue(null);
            TargetLangBox.ItemsSource = supportedLanguages.Keys;

            string targetLang = Translator.Setting.TargetLanguage;
            if (!supportedLanguages.ContainsKey(targetLang))
                supportedLanguages[targetLang] = targetLang;
            TargetLangBox.SelectedItem = targetLang;
        }
    }
}