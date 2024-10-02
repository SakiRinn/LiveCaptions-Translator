using System.Windows;
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
                Wpf.Ui.Appearance.SystemThemeWatcher.Watch(
                    this,                                   // Window class
                    WindowBackdropType.Mica,                // Background type
                    true                                    // Whether to change accents automatically
                );
            };
            Loaded += (sender, args) => RootNavigation.Navigate(typeof(CaptionPage));
        }

        void TopmostButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;
            if (this.Topmost)
            {
                this.Topmost = false;
                symbolIcon.Filled = false;
            }
            else
            {
                this.Topmost = true;
                symbolIcon.Filled = true;
            }
        }
    }
}