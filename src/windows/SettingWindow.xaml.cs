using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

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
                InitializeSectionReferences();
                SelectButton(GeneralButton);
            };
        }

        private void InitializeSectionReferences()
        {
            sectionReferences = new Dictionary<string, FrameworkElement>
            {
                { "General", ContentPanel },
                { "Prompt", PromptSection },
                { "Ollama", OllamaSection },
                { "OpenAI", OpenAISection },
                { "OpenRouter", OpenRouterSection },
                { "DeepL", DeepLSection },
                { "Youdao", YoudaoSection },
                { "MTranServer", MTranServerSection },
                { "Baidu", BaiduSection },
            };
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

        private void SelectButton(System.Windows.Controls.Button button)
        {
            if (currentSelected != null)
                currentSelected.Background = new SolidColorBrush(Colors.Transparent);
            button.Background = (Brush)FindResource("ControlFillColorSecondaryBrush");
            currentSelected = button;
        }

        private void OpenAIAPIKeyInfo_MouseEnter(object sender, MouseEventArgs e)
        {
            OpenAIAPIKeyInfoFlyout.Show();
        }

        private void OpenAIAPIKeyInfo_MouseLeave(object sender, MouseEventArgs e)
        {
            OpenAIAPIKeyInfoFlyout.Hide();
        }
    }
}