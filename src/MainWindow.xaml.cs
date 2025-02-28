using System.Windows;
using System.Text.RegularExpressions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

using LiveCaptionsTranslator.src;

namespace LiveCaptionsTranslator
{
    public partial class MainWindow : FluentWindow
    {
        private uint OverlayMode = 0;

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

            WindowStateRestore(this, "Main");
            ToggleTopmost(App.Settings.MainTopmost);
        }

        private void TopmostButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleTopmost(!Topmost);
        }

        private void OverlaySubtitleModeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            if (OverlayMode == 0)
            {
                // Mode 1: Caption + Translation
                OverlayMode = 1;
                symbolIcon.Symbol = SymbolRegular.TextUnderlineDouble20;

                SubtitleWindow = new SubtitleWindow();
                WindowStateRestore(SubtitleWindow, "Overlay");
                SubtitleWindow.SizeChanged += 
                    (s, e) => WindowStateSave(SubtitleWindow, "Overlay");
                SubtitleWindow.LocationChanged += 
                    (s, e) => WindowStateSave(SubtitleWindow, "Overlay");
                SubtitleWindow.Show();
            }
            else if (OverlayMode == 1)
            {
                // Mode 2: Translation Only
                OverlayMode = 2;
                symbolIcon.Symbol = SymbolRegular.TextAddSpaceBefore20;

                SubtitleWindow.TranslationOnly(true);
                SubtitleWindow.Focus();
            }
            else
            {
                // Mode 0: Close
                OverlayMode = 0;
                symbolIcon.Symbol = SymbolRegular.WindowNew20;

                SubtitleWindow.TranslationOnly(false);
                SubtitleWindow.Close();

                SubtitleWindow = null;
                symbolIcon.Filled = false;
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
            WindowStateSave(window, "Main");
        }

        private void ToggleTopmost(bool enable)
        {
            var button = topmost as Button;
            var symbolIcon = button?.Icon as SymbolIcon;
            Topmost = enable;
            symbolIcon.Filled = enable;
            App.Settings.MainTopmost = enable;
        }

        private void WindowStateSave(Window window, string windowType)
        {
            if (window != null)
            {
                App.Settings.WindowBounds[windowType] = Regex.Replace(
                    window.RestoreBounds.ToString(), @"(\d+\.\d{1})\d+", "$1");
                App.Settings?.Save();
            }
        }

        private void WindowStateRestore(Window window, string windowType)
        {
            if (window != null)
            {
                Rect bounds = Rect.Parse(App.Settings.WindowBounds[windowType]);
                if (!bounds.IsEmpty)
                {
                    window.Top = bounds.Top;
                    window.Left = bounds.Left;

                    // Restore the size only for a manually sized
                    if (window.SizeToContent == SizeToContent.Manual)
                    {
                        window.Width = bounds.Width;
                        window.Height = bounds.Height;
                    }
                }
            }
        }
    }
}
