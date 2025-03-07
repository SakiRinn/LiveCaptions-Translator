using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class MainWindow : FluentWindow
    {
        public SubtitleWindow? SubtitleWindow { get; set; } = null;

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

            var windowState = WindowHandler.LoadState(this, App.Settings);
            WindowHandler.RestoreState(this, windowState);
            ToggleTopmost(App.Settings.MainWindow.Topmost);
            EnableCaptionLog(App.Settings.MainWindow.CaptionLogEnabled);
        }

        private void TopmostButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleTopmost(!this.Topmost);
        }

        private void OverlaySubtitleModeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            if (SubtitleWindow == null)
            {
                // Caption + Translation
                symbolIcon.Symbol = SymbolRegular.TextUnderlineDouble20;

                SubtitleWindow = new SubtitleWindow();
                SubtitleWindow.SizeChanged +=
                    (s, e) => WindowHandler.SaveState(SubtitleWindow, App.Settings);
                SubtitleWindow.LocationChanged +=
                    (s, e) => WindowHandler.SaveState(SubtitleWindow, App.Settings);

                var windowState = WindowHandler.LoadState(SubtitleWindow, App.Settings);
                WindowHandler.RestoreState(SubtitleWindow, windowState);
                SubtitleWindow.Show();
            }
            else if (!SubtitleWindow.IsTranslationOnly)
            {
                // Translation Only
                symbolIcon.Symbol = SymbolRegular.TextAddSpaceBefore20;

                SubtitleWindow.IsTranslationOnly = true;
                SubtitleWindow.Focus();
            }
            else
            {
                // Closed
                symbolIcon.Symbol = SymbolRegular.WindowNew20;

                SubtitleWindow.IsTranslationOnly = false;
                SubtitleWindow.Close();
                SubtitleWindow = null;
            }
        }

        private void LogOnly_OnClickButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            if (App.Captions.LogOnlyFlag)
            {
                App.Captions.LogOnlyFlag = false;
                symbolIcon.Filled = false;
            }
            else
            {
                App.Captions.LogOnlyFlag = true;
                symbolIcon.Filled = true;
            }
        }

        private void MainWindow_BoundsChanged(object sender, EventArgs e)
        {
            var window = sender as Window;
            WindowHandler.SaveState(window, App.Settings);
        }

        private void ToggleTopmost(bool enabled)
        {
            var button = topmost as Button;
            var symbolIcon = button?.Icon as SymbolIcon;
            symbolIcon.Filled = enabled;
            this.Topmost = enabled;
            App.Settings.MainWindow.Topmost = enabled;
        }

        private void CaptionLog_OnClickButton_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.MainWindow.CaptionLogEnabled = !App.Settings.MainWindow.CaptionLogEnabled;
            EnableCaptionLog(App.Settings.MainWindow.CaptionLogEnabled);
        }

        private void EnableCaptionLog(bool enable)
        {
            if (captionLog.Icon is SymbolIcon icon)
            {
                if (enable)
                {
                    icon.Symbol = SymbolRegular.History24;
                }
                else
                {
                    icon.Symbol = SymbolRegular.HistoryDismiss24;
                    App.Captions?.ClearCaptionLog();
                }
                var captionPage = this.Content as CaptionPage;
                captionPage?.CollapseTranslatedCaption(enable);
            }
        }
    }
}
