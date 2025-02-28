using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using LiveCaptionsTranslator.src;

namespace LiveCaptionsTranslator
{
    public partial class MainWindow : FluentWindow
    {
        private Window? subtitleWindow = null;

        private bool isLogOnlyEnabled = false;

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

            WindowStateRestore(this, "Main");
            ToggleTopmost(App.Settings.MainTopmost);
        }

        void TopmostButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleTopmost(!Topmost);
        }

        private void ToggleTopmost(bool enable)
        {
            var button = topmost as Button;
            var symbolIcon = button?.Icon as SymbolIcon;
            Topmost = enable;
            symbolIcon.Filled = enable;
            App.Settings.MainTopmost = enable;
        }

        void OverlaySubtitleModeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            if (subtitleWindow == null)
            {
                subtitleWindow = new SubtitleWindow();
                WindowStateRestore(subtitleWindow, "Overlay");
                subtitleWindow.SizeChanged += (s, e) =>
                {
                    WindowStateSave(subtitleWindow, "Overlay");
                };
                subtitleWindow.LocationChanged += (s, e) =>
                {
                    WindowStateSave(subtitleWindow, "Overlay");
                };
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

        private void LogOnly_OnClickButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            if (isLogOnlyEnabled)
            {
                App.Captions.LogOnlyFlag = false;
                symbolIcon.Filled = false;
            }
            else
            {
                App.Captions.LogOnlyFlag = true;
                symbolIcon.Filled = true;
            }
            isLogOnlyEnabled = !isLogOnlyEnabled;
        }

        private void MainWindow_BoundsChanged(object sender, EventArgs e)
        {
            var window = sender as Window;
            WindowStateSave(window, "Main");
        }

        private void WindowStateSave(Window windows, string windowsType)
        {
            if (windows != null)
            {
                App.Settings.WindowBounds[windowsType] = windows.RestoreBounds.ToString();
                App.Settings?.Save();
            }
        }

        private void WindowStateRestore(Window windows, string windowsType)
        {
            if (windows != null)
            {

                Rect bounds = Rect.Parse(App.Settings.WindowBounds[windowsType]);
                if (!bounds.IsEmpty)
                {
                    windows.Top = bounds.Top;
                    windows.Left = bounds.Left;

                    // Restore the size only for a manually sized
                    if (windows.SizeToContent == SizeToContent.Manual)
                    {
                        windows.Width = bounds.Width;
                        windows.Height = bounds.Height;
                    }
                }
            }
        }

        private void CaptionLog_OnClickButton_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.CaptionLogEnable = !App.Settings.CaptionLogEnable;
            EnableCaptionLog(App.Settings.CaptionLogEnable);
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
                    App.Captions.ClearCaptionLog();
                }
            }
        }
    }
}
