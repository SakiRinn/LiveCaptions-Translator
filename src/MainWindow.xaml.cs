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
                    this,
                    WindowBackdropType.Mica,
                    true
                );
            };
            Loaded += (sender, args) => RootNavigation.Navigate(typeof(CaptionPage));

            var windowState = WindowHandler.LoadState(this, App.Setting);
            WindowHandler.RestoreState(this, windowState);
            ToggleTopmost(App.Setting.MainWindow.Topmost);
            EnableCaptionLog(App.Setting.MainWindow.CaptionLogEnabled);
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
                    (s, e) => WindowHandler.SaveState(SubtitleWindow, App.Setting);
                SubtitleWindow.LocationChanged +=
                    (s, e) => WindowHandler.SaveState(SubtitleWindow, App.Setting);

                var windowState = WindowHandler.LoadState(SubtitleWindow, App.Setting);
                WindowHandler.RestoreState(SubtitleWindow, windowState);
                SubtitleWindow.Show();
            }
            else if (!SubtitleWindow.IsTranslationOnly)
            {
                // Translation Only
                symbolIcon.Symbol = SymbolRegular.TextAddSpaceBefore24;

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

            if (App.Caption.LogOnlyFlag)
            {
                App.Caption.LogOnlyFlag = false;
                symbolIcon.Filled = false;
            }
            else
            {
                App.Caption.LogOnlyFlag = true;
                symbolIcon.Filled = true;
            }
        }

        private void CaptionLog_OnClickButton_Click(object sender, RoutedEventArgs e)
        {
            App.Setting.MainWindow.CaptionLogEnabled = !App.Setting.MainWindow.CaptionLogEnabled;
            EnableCaptionLog(App.Setting.MainWindow.CaptionLogEnabled);
        }

        private void MainWindow_BoundsChanged(object sender, EventArgs e)
        {
            var window = sender as Window;
            WindowHandler.SaveState(window, App.Setting);
        }

        private void ToggleTopmost(bool enabled)
        {
            var button = topmost as Button;
            var symbolIcon = button?.Icon as SymbolIcon;
            symbolIcon.Filled = enabled;
            this.Topmost = enabled;
            App.Setting.MainWindow.Topmost = enabled;
        }

        private void EnableCaptionLog(bool enable)
        {
            if (captionLog.Icon is SymbolIcon icon)
            {
                if (enable)
                    icon.Symbol = SymbolRegular.History24;
                else
                    icon.Symbol = SymbolRegular.HistoryDismiss24;
                CaptionPage.Instance?.CollapseTranslatedCaption(enable);
            }
        }
    }
}
