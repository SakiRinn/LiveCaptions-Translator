using LiveCaptionsTranslator.models;
using System.Windows;
using WpfButton = Wpf.Ui.Controls.Button;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using LiveCaptionsTranslator.src;

namespace LiveCaptionsTranslator
{
    public partial class MainWindow : FluentWindow
    {
        private Window? subtitleWindow = null;

        private bool isLogonlyEnabled = false;

        public MainWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();

            Loaded += (sender, args) =>
            {
                SystemThemeWatcher.Watch(
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
                if (App.Window == null)
                    App.Window = LiveCaptionsHandler.LaunchLiveCaptions();
                App.Captions.PauseFlag = false;
                symbolIcon.Filled = false;
            }
            else
            {
                App.Captions.PauseFlag = true;
                symbolIcon.Filled = true;
            }
        }

        void OverlaySubtitleModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not WpfButton button || button.Icon is not SymbolIcon symbolIcon) return;

            if (subtitleWindow == null)
            {
                subtitleWindow = new SubtitleWindow();
                subtitleWindow.Show();
                symbolIcon.Filled = true;
            }
            else
            {
                subtitleWindow.Close();
                subtitleWindow = null;
                symbolIcon.Filled = false;
            }
        }

        private void Logonly_OnClickButton_Click(object sender, RoutedEventArgs e)
        {
            if (logonly.Icon is SymbolIcon icon)
            {
                if (isLogonlyEnabled)
                {
                    icon.Symbol = SymbolRegular.TextGrammarWand24;
                    App.Captions.LogonlyFlag = false;
                }
                else
                {
                    icon.Symbol = SymbolRegular.TextGrammarArrowLeft24; 
                    App.Captions.LogonlyFlag = true;
                }

                isLogonlyEnabled = !isLogonlyEnabled;
            }
        }
    }
}
