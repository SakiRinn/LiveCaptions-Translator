using LiveCaptionsTranslator.models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

            targetLangBox.SelectionChanged += targetLangBox_SelectionChanged;
            targetLangBox.LostFocus += targetLangBox_LostFocus;
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            LiveCaptionsHandler.ClickSettingsButton(App.Window);
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
            var settingGrid = this.FindName($"{App.Settings.ApiName}Grid") as Grid;
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

        private void targetLangBox_LostFocus(object sender, RoutedEventArgs e)
        {
            App.Settings.TargetLanguage = targetLangBox.Text;
        }
        
        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            InfoPopup.IsOpen = true;
            PageGrid.PreviewMouseDown += PageGrid_Click;
        }
        
        private void PageGrid_Click(object sender, MouseButtonEventArgs e)
        {
            if (InfoPopup.IsOpen)
            {
                InfoPopup.IsOpen = false;
                PageGrid.PreviewMouseDown -= PageGrid_Click;
            }
        }
    }
}