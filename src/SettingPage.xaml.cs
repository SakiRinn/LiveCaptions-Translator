using LiveCaptionsTranslator.models;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Appearance;
using System.Threading.Tasks;

namespace LiveCaptionsTranslator
{
    public partial class SettingPage : Page
    {
        public SettingPage()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
            DataContext = App.Settings;

            foreach (ComboBoxItem item in translateAPIBox.Items)
            {
                if (item.Content.ToString() == App.Settings.ApiName)
                {
                    translateAPIBox.SelectedItem = item;
                    break;
                }
            }

            LoadAPISetting();
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            LiveCaptionsHandler.ClickSettingsButton(App.Window);
        }

        private void LoadAPISetting()
        {
            var supportedLanguages = App.Settings.CurrentAPIConfig.SupportedLanguages;
            Dispatcher.Invoke(() =>
            {
                targetLangBox.ItemsSource = supportedLanguages.Keys;
                targetLangBox.SelectedIndex = 0;

                ollamaSettings.Visibility = Visibility.Collapsed;
                openAISettings.Visibility = Visibility.Collapsed;
                googleTranslateSettings.Visibility = Visibility.Collapsed;
                openRouterSettings.Visibility = Visibility.Collapsed;

                switch (App.Settings.ApiName)
                {
                    case "Ollama":
                        ollamaSettings.Visibility = Visibility.Visible;
                        break;
                    case "OpenAI":
                        openAISettings.Visibility = Visibility.Visible;
                        break;
                    case "OpenRouter":
                        openRouterSettings.Visibility = Visibility.Visible;
                        break;
                    case "GoogleTranslate":
                        googleTranslateSettings.Visibility = Visibility.Visible;
                        break;
                }
            });
        }

        private async void translateAPIBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox comboBox) return;

            var selectedItem = comboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            string apiName = selectedItem.Content.ToString() ?? "";
            TranslateAPIConfig newConfig = App.Settings.Configs.ContainsKey(apiName) 
                ? App.Settings.Configs[apiName]  // 如果存在，使用已有配置
                : CreateNewConfig(apiName);      // 如果不存在，创建新配置

            await Dispatcher.InvokeAsync(() =>
            {
                App.Settings.CurrentAPIConfig = newConfig;
                App.Settings.ApiName = apiName;
            });

            await Task.Run(() => LoadAPISetting());
        }

        private TranslateAPIConfig CreateNewConfig(string apiName)
        {
            TranslateAPIConfig config;
            switch (apiName)
            {
                case "Ollama":
                    config = new OllamaConfig();
                    break;
                case "OpenAI":
                    config = new OpenAIConfig();
                    break;
                case "OpenRouter":
                    config = new OpenRouterConfig();
                    break;
                case "GoogleTranslate":
                    config = new GoogleTranslateConfig();
                    break;
                default:
                    throw new ArgumentException("Unknown API name");
            }

            App.Settings.Configs[apiName] = config;
            return config;
        }

        private void OpenRouterApiKey_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (App.Settings.CurrentAPIConfig is OpenRouterConfig config)
            {
                config.ApiKey = ((PasswordBox)sender).Password;
            }
        }
    }
}
