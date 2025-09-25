using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.utils;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using TextBlock = Wpf.Ui.Controls.TextBlock;

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

        private void OpenAIAPIKeyInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            OpenAIAPIKeyInfoFlyout.Show();
        }

        private void OpenAIAPIKeyInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            OpenAIAPIKeyInfoFlyout.Hide();
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