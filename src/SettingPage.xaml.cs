﻿using LiveCaptionsTranslator.models;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Appearance;

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
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            LiveCaptionsHandler.ClickSettingsButton(App.Window);
        }

        private void LoadAPISetting()
        {
            var supportedLanguages = App.Settings.CurrentAPIConfig.SupportedLanguages;
            targetLangBox.ItemsSource = supportedLanguages.Keys;
            targetLangBox.SelectedIndex = 0;

            foreach (UIElement element in PageGrid.Children)
            {
                if (element is Grid childGrid)
                    childGrid.Visibility = Visibility.Collapsed;
            }
            var settingGrid = this.FindName($"{App.Settings.ApiName}Grid") as Grid;
            settingGrid.Visibility = Visibility.Visible;
        }

        private void translateAPIBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAPISetting();
            
            // Initialize the appropriate caption provider
            if (App.Captions != null)
            {
                App.Captions.InitializeProvider(App.Settings.ApiName);
            }
        }
    }
}
