﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Appearance;

using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator
{
    public partial class SettingPage : Page
    {
        public SettingPage()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
            DataContext = App.Settings;

            translateAPIBox.ItemsSource = App.Settings.Configs.Keys;
            translateAPIBox.SelectedIndex = 0;
            LoadAPISetting();

            targetLangBox.SelectionChanged += targetLangBox_SelectionChanged;
            targetLangBox.LostFocus += targetLangBox_LostFocus;
        }

        private void Button_LiveCaptions(object sender, RoutedEventArgs e)
        {
            if (App.Window == null)
                return;

            var button = sender as Wpf.Ui.Controls.Button;
            var text = ButtonText.Text;

            if (text == "Show")
            {
                LiveCaptionsHandler.RestoreLiveCaptions(App.Window);
                ButtonText.Text = "Hide";
            }
            else
            {
                LiveCaptionsHandler.HideLiveCaptions(App.Window);
                ButtonText.Text = "Show";
            }
        }

        private void LoadAPISetting()
        {
            string targetLang = App.Settings.TargetLanguage;
            var supportedLanguages = App.Settings.CurrentAPIConfig.SupportedLanguages;
            targetLangBox.ItemsSource = supportedLanguages.Keys;

            // Add custom target language to ComboBox
            if (!supportedLanguages.ContainsKey(targetLang))
            {
                supportedLanguages[targetLang] = targetLang;
            }
            targetLangBox.SelectedItem = targetLang;

            foreach (UIElement element in PageGrid.Children)
            {
                if (element is Grid childGrid)
                    childGrid.Visibility = Visibility.Collapsed;
            }
            var settingGrid = FindName($"{App.Settings.ApiName}Grid") as Grid ?? FindName($"NoSettingGrid") as Grid;
            settingGrid.Visibility = Visibility.Visible;
        }

        private void translateAPIBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAPISetting();
        }

        private void targetLangBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (targetLangBox.SelectedItem != null)
            {
                App.Settings.TargetLanguage = targetLangBox.SelectedItem.ToString();
            }
        }

        private void captionLogMax_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = (sender as ComboBox).SelectedIndex;
            if (index < App.Settings.CaptionLogMax)
            {
                App.Captions.ClearCaptionLog();
            }
            App.Settings.CaptionLogMax = index;
        }

        private void targetLangBox_LostFocus(object sender, RoutedEventArgs e)
        {
            App.Settings.TargetLanguage = targetLangBox.Text;
        }

        private void TargetLangButton_MouseEnter(object sender, MouseEventArgs e)
        {
            TargetLangInfoFlyout.Show();
        }

        private void TargetLangButton_MouseLeave(object sender, MouseEventArgs e)
        {
            TargetLangInfoFlyout.Hide();
        }

        private void LiveCaptionsButton_MouseEnter(object sender, MouseEventArgs e)
        {
            LiveCaptionsInfoFlyout.Show();
        }

        private void LiveCaptionsButton_MouseLeave(object sender, MouseEventArgs e)
        {
            LiveCaptionsInfoFlyout.Hide();
        }

        private void FrequencyButton_MouseEnter(object sender, MouseEventArgs e)
        {
            FrequencyInfoFlyout.Show();
        }

        private void FrequencyButton_MouseLeave(object sender, MouseEventArgs e)
        {
            FrequencyInfoFlyout.Hide();
        }
    }
}