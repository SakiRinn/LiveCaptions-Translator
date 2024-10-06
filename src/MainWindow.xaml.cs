using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace LiveCaptionsTranslator
{
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += (sender, args) =>
            {
                SystemThemeWatcher.Watch(
                    this,                                   // Window class
                    WindowBackdropType.Mica,                // Background type
                    true                                    // Whether to change accents automatically
                );
            };
            Loaded += (sender, args) => RootNavigation.Navigate(typeof(CaptionPage));

            ApplicationThemeManager.ApplySystemTheme();
        }

        void TopmostButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;
            if (Topmost)
            {
                Topmost = false;
                symbolIcon.Filled = false;
            }
            else
            {
                Topmost = true;
                symbolIcon.Filled = true;
            }
        }

        void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;
            if (App.Captions.PauseFlag)
            {
                App.Captions.PauseFlag = false;
                symbolIcon.Filled = false;
            }
            else
            {
                App.Captions.PauseFlag = true;
                symbolIcon.Filled = true;
            }
        }
    }
}
