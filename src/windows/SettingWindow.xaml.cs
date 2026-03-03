using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

using LiveCaptionsTranslator.apis;
using LiveCaptionsTranslator.models;
using Button = Wpf.Ui.Controls.Button;
using TextBlock = Wpf.Ui.Controls.TextBlock;
using ComboBox = System.Windows.Controls.ComboBox;

namespace LiveCaptionsTranslator
{
    public partial class SettingWindow : FluentWindow
    {
        private System.Windows.Controls.Button currentSelected;
        private Dictionary<string, FrameworkElement> sectionReferences;

        public SettingWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
            DataContext = Translator.Setting;

            Loaded += (sender, args) =>
            {
                SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
                Initialize();
                SelectButton(PromptButton);
            };
        }

        private void Initialize()
        {
            sectionReferences = new Dictionary<string, FrameworkElement>
            {
                { "General", ContentPanel },
                { "Prompt", PromptSection }
            };

            foreach (var apiName in TranslateAPI.TRANSLATE_FUNCTIONS.Keys.Where(apiName =>
                         !TranslateAPI.NO_CONFIG_APIS.Contains(apiName)))
            {
                sectionReferences[apiName] = FindName($"{apiName}Section") as StackPanel;
                SwitchConfig(apiName, Translator.Setting.ConfigIndices[apiName]);
            }
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string apiName = button.Tag as string;
                var configs = Translator.Setting.Configs[apiName];
                var configIndex = Translator.Setting.ConfigIndices[apiName];

                var type = Type.GetType($"LiveCaptionsTranslator.models.{apiName}Config");
                var config = Activator.CreateInstance(type) as TranslateAPIConfig;
                configs.Insert(configIndex + 1, config);
                SwitchConfig(apiName, configIndex + 1);

                Translator.Setting.OnPropertyChanged("Configs");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string apiName = button.Tag as string;
                var configs = Translator.Setting.Configs[apiName];
                var configIndex = Translator.Setting.ConfigIndices[apiName];

                if (configs.Count <= 1)
                {
                    (FindName($"{apiName}DeleteFlyout") as Flyout)?.Show();
                    return;
                }
                configs.RemoveAt(configIndex);
                SwitchConfig(apiName, Math.Max(0, Math.Min(configs.Count - 1, configIndex)));

                Translator.Setting.OnPropertyChanged("Configs");
            }
        }

        private void PriorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string apiName = button.Tag as string;
                var configIndex = Translator.Setting.ConfigIndices[apiName];
                SwitchConfig(apiName, configIndex - 1);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string apiName = button.Tag as string;
                var configIndex = Translator.Setting.ConfigIndices[apiName];
                SwitchConfig(apiName, configIndex + 1);
            }
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button)
            {
                SelectButton(button);
                string targetSection = button.Tag.ToString();
                if (sectionReferences.TryGetValue(targetSection, out FrameworkElement element))
                    element.BringIntoView();
            }
        }

        private void OpenAIAPIUrlInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            OpenAIAPIUrlInfoFlyout.Show();
        }

        private void OpenAIAPIUrlInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            OpenAIAPIUrlInfoFlyout.Hide();
        }

        private void OllamaAPIUrlInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            OllamaAPIUrlInfoFlyout.Show();
        }

        private void OllamaAPIUrlInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            OllamaAPIUrlInfoFlyout.Hide();
        }

        private void LMStudioAPIUrlInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            LMStudioAPIUrlInfoFlyout.Show();
        }

        private void LMStudioAPIUrlInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            LMStudioAPIUrlInfoFlyout.Hide();
        }

        private async void LoadModelsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string apiName && apiName == "LMStudio")
            {
                string baseUrl = (Translator.Setting["LMStudio"] as LMStudioConfig)?.ApiUrl ?? "";

                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    System.Windows.MessageBox.Show("Please set the API URL first.", "Load Models", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                button.IsEnabled = false;
                try
                {
                    var models = await ModelsApiService.FetchModelsAsync(apiName, baseUrl);
                    var comboBox = FindName($"{apiName}ModelComboBox") as ComboBox;
                    if (comboBox != null)
                    {
                        comboBox.ItemsSource = models;
                        if (models.Count > 0)
                            System.Windows.MessageBox.Show($"Loaded {models.Count} model(s).", "Load Models", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        else
                            System.Windows.MessageBox.Show("No models found or unable to connect. Check that the server is running.", "Load Models", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    }
                }
                finally
                {
                    button.IsEnabled = true;
                }
            }
        }

        private void LMStudioModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ModelsApiService.ModelInfo mi)
            {
                var config = Translator.Setting["LMStudio"] as LMStudioConfig;
                if (config != null)
                {
                    config.ModelName = mi.Id;
                    if (sender is ComboBox cb)
                        cb.Text = mi.Id;
                }
            }
        }

        private void SwitchConfig(string apiName, int index)
        {
            if (index < 0 || index >= Translator.Setting.Configs[apiName].Count)
                return;

            if (Translator.Setting.ConfigIndices[apiName] != index)
                Translator.Setting.ConfigIndices[apiName] = index;

            if (FindName($"{apiName}Index") is TextBlock indexTextBlock)
            {
                int total = Translator.Setting.Configs[apiName].Count;
                indexTextBlock.Text = $"{index + 1}/{total}";
            }
            Translator.Setting.OnPropertyChanged(null);
        }

        private void SelectButton(System.Windows.Controls.Button button)
        {
            if (currentSelected != null)
                currentSelected.Background = new SolidColorBrush(Colors.Transparent);
            button.Background = (Brush)FindResource("ControlFillColorSecondaryBrush");
            currentSelected = button;
        }
    }
}