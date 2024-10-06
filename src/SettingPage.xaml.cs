using LiveCaptionsTranslator.models;
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
            DataContext = App.Settings;

            translateAPIBox.ItemsSource = App.Settings.Configs.Keys;
            translateAPIBox.SelectedIndex = 0;
            targetLangBox.ItemsSource = TranslateAPI.OPENAI_SUPPORTED_LANGS.Keys;
            targetLangBox.SelectedIndex = 0;

            ApplicationThemeManager.ApplySystemTheme();
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            LiveCaptionsHandler.ClickSettingsButton(App.Window);
        }
    }
}
