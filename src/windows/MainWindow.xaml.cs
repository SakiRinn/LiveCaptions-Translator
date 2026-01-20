using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

using LiveCaptionsTranslator.utils;
using LiveCaptionsTranslator.Utils;
using Button = Wpf.Ui.Controls.Button;

namespace LiveCaptionsTranslator
{
    public partial class MainWindow : FluentWindow
    {
        public OverlayWindow? OverlayWindow { get; set; } = null;
        public bool IsAutoHeight { get; set; } = true;

        public MainWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();

            Loaded += (s, e) =>
            {
                SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
                RootNavigation.Navigate(typeof(CaptionPage));
                IsAutoHeight = true;
                CheckForFirstUse();

                // Fire-and-forget, but keep a reference so exceptions aren't silently ignored by analyzers.
                _ = CheckForUpdates();
            };

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            var setting = Translator.Setting;
            if (setting is not null)
            {
                var windowState = WindowHandler.LoadState(this, setting);
                if (windowState.Left <= 0 || windowState.Left >= screenWidth ||
                    windowState.Top <= 0 || windowState.Top >= screenHeight)
                {
                    WindowHandler.RestoreState(this, new Rect(
                        (screenWidth - 775) / 2, screenHeight * 3 / 4 - 167, 775, 167));
                }
                else
                    WindowHandler.RestoreState(this, windowState);

                ToggleTopmost(setting.MainWindow.Topmost);
                ShowLogCard(setting.MainWindow.CaptionLogEnabled);
            }
        }

        private void TopmostButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleTopmost(!this.Topmost);
        }

        private void OverlayModeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            if (OverlayWindow == null)
            {
                symbolIcon.Symbol = SymbolRegular.ClosedCaption24;
                symbolIcon.Filled = true;

                OverlayWindow = new OverlayWindow();
                OverlayWindow.SizeChanged +=
                    (s, e) => WindowHandler.SaveState(OverlayWindow, Translator.Setting);
                OverlayWindow.LocationChanged +=
                    (s, e) => WindowHandler.SaveState(OverlayWindow, Translator.Setting);

                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;

                var windowState = WindowHandler.LoadState(OverlayWindow, Translator.Setting);
                if (windowState.Left <= 0 || windowState.Left >= screenWidth ||
                    windowState.Top <= 0 || windowState.Top >= screenHeight)
                {
                    WindowHandler.RestoreState(OverlayWindow, new Rect(
                        (screenWidth - 650) / 2, screenHeight * 5 / 6 - 135, 650, 135));
                }
                else
                    WindowHandler.RestoreState(OverlayWindow, windowState);

                OverlayWindow.Show();
            }
            else
            {
                symbolIcon.Symbol = SymbolRegular.ClosedCaptionOff24;
                symbolIcon.Filled = false;

                switch (OverlayWindow.OnlyMode)
                {
                    case CaptionVisible.TranslationOnly:
                        OverlayWindow.OnlyMode = CaptionVisible.SubtitleOnly;
                        OverlayWindow.OnlyMode = CaptionVisible.Both;
                        break;
                    case CaptionVisible.SubtitleOnly:
                        OverlayWindow.OnlyMode = CaptionVisible.Both;
                        break;
                }

                OverlayWindow.Close();
                OverlayWindow = null;
            }
        }

        private void LogOnlyButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            if (Translator.LogOnlyFlag)
            {
                Translator.LogOnlyFlag = false;
                if (symbolIcon is not null)
                    symbolIcon.Filled = false;
            }
            else
            {
                Translator.LogOnlyFlag = true;
                if (symbolIcon is not null)
                    symbolIcon.Filled = true;
            }

            Translator.Caption?.Contexts.Clear();
        }

        private void CaptionLogButton_Click(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting is null)
                return;

            Translator.Setting.MainWindow.CaptionLogEnabled = !Translator.Setting.MainWindow.CaptionLogEnabled;
            ShowLogCard(Translator.Setting.MainWindow.CaptionLogEnabled);
            CaptionPage.Instance?.AutoHeight();
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            if (Translator.Setting is null)
                return;

            var window = sender as Window;
            if (window is not null)
                WindowHandler.SaveState(window, Translator.Setting);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MainWindow_LocationChanged(sender, e);
            IsAutoHeight = false;
        }

        private void MainContent_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Re-run auto sizing on demand (e.g. after language switch).
            TriggerAutoSizeForCurrentContent();
        }

        private void TriggerAutoSizeForCurrentContent()
        {
            // Keep the user's manual resizing respected if they changed size.
            // This is only a "best effort" refinement when language changes.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var prev = SizeToContent;
                SizeToContent = SizeToContent.WidthAndHeight;
                UpdateLayout();

                // Clamp so window won't become too small/large.
                const double minW = 750;  // matches XAML MinWidth
                const double maxW = 1400;
                const double minH = 170;  // matches XAML MinHeight
                const double maxH = 900;

                Width = Math.Max(minW, Math.Min(maxW, ActualWidth));
                Height = Math.Max(minH, Math.Min(maxH, ActualHeight));

                SizeToContent = prev;
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public void ToggleTopmost(bool enabled)
        {
            var button = TopmostButton as Button;
            var symbolIcon = button?.Icon as SymbolIcon;
            if (symbolIcon is not null)
                symbolIcon.Filled = enabled;

            Topmost = enabled;
            if (Translator.Setting is not null)
                Translator.Setting.MainWindow.Topmost = enabled;
        }

        private void CheckForFirstUse()
        {
            if (!Translator.FirstUseFlag)
                return;

            RootNavigation.Navigate(typeof(SettingPage));

            if (Translator.Window is not null)
                LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);

            Dispatcher.InvokeAsync(() =>
            {
                var welcomeWindow = new WelcomeWindow
                {
                    Owner = this
                };

                // Auto-size WelcomeWindow on first show.
                welcomeWindow.Loaded += (_, __) =>
                {
                    welcomeWindow.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var prev = welcomeWindow.SizeToContent;
                        welcomeWindow.SizeToContent = SizeToContent.Height;
                        welcomeWindow.UpdateLayout();
                        welcomeWindow.SizeToContent = prev;
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                };

                welcomeWindow.Show();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private async Task CheckForUpdates()
        {
            if (Translator.FirstUseFlag)
                return;

            string latestVersion = string.Empty;
            try
            {
                latestVersion = await UpdateUtil.GetLatestVersion();
            }
            catch (Exception ex)
            {
                SnackbarHost.Show(
                    TryFindResource("MC0") as string ?? "[ERROR] Update Check Failed.",
                    ex.Message,
                    TryFindResource("MC1") as string ?? "error");

                return;
            }

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            var ignoredVersion = Translator.Setting.IgnoredUpdateVersion;
            if (!string.IsNullOrEmpty(ignoredVersion) && ignoredVersion == latestVersion)
                return;
            if (!string.IsNullOrEmpty(latestVersion) && latestVersion != currentVersion)
            {
                var dialog = new Wpf.Ui.Controls.MessageBox
                {
                    Title = TryFindResource("MC2") as string ?? "New Version Available",
                    Content = string.Format(
                        TryFindResource("MC3") as string ??
                        "A new version has been detected: {0}\nCurrent version: {1}\nPlease visit GitHub to download the latest release.",
                        latestVersion,
                        currentVersion),
                    PrimaryButtonText = TryFindResource("MC4") as string ?? "Update",
                    CloseButtonText = TryFindResource("MC5") as string ?? "Ignore this version"
                };
                var result = await dialog.ShowDialogAsync();

                if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
                {
                    var url = UpdateUtil.GitHubReleasesUrl;
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        SnackbarHost.Show(
                            TryFindResource("MC6") as string ?? "[ERROR] Open Browser Failed.",
                            ex.Message,
                            TryFindResource("MC1") as string ?? "error");
                    }
                }
                else
                    Translator.Setting.IgnoredUpdateVersion = latestVersion;
            }
        }

        public void ShowLogCard(bool enabled)
        {
            if (CaptionLogButton.Icon is SymbolIcon icon)
            {
                if (enabled)
                    icon.Symbol = SymbolRegular.History24;
                else
                    icon.Symbol = SymbolRegular.HistoryDismiss24;
                CaptionPage.Instance?.CollapseTranslatedCaption(enabled);
            }
        }

        public void AutoHeightAdjust(int minHeight = -1, int maxHeight = -1)
        {
            if (minHeight > 0 && Height < minHeight)
            {
                Height = minHeight;
                IsAutoHeight = true;
            }

            if (IsAutoHeight && maxHeight > 0 && Height > maxHeight)
                Height = maxHeight;
        }

        private void SettingNavItem_Click(object sender, RoutedEventArgs e)
        {
            // Best-effort: try to find a SettingPage in the visual tree and ask it to auto-fit.
            // If we cannot locate it, still do a window-level measurement.
            try
            {
                var sp = FindDescendant<SettingPage>(this);
                sp?.RequestAutoFitWidth();
                if (sp is not null)
                    return;
            }
            catch
            {
                // ignore and fall back
            }

            TriggerAutoSizeForCurrentContent();
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
                if (child is T typed)
                    return typed;

                var found = FindDescendant<T>(child);
                if (found is not null)
                    return found;
            }

            return null;
        }
    }
}