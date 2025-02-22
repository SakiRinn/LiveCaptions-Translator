using LiveCaptionsTranslator.models;
using System.Windows;
using WpfButton = Wpf.Ui.Controls.Button;
using SystemControls = System.Windows.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Input;

namespace LiveCaptionsTranslator
{
    public partial class MainWindow : FluentWindow
    {
        // TODO: Extract them into a new SubtitleWindow class.
        private Window? subtitleWindow = null;
        private bool isResizing = false;
        private bool isLogonlyEnabled = false;
        private Point startPoint;
        private Size startSize;
        private Point startLocation;
        private Window? translationOnlyWindow = null;

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

        // TODO: Extract them into a new SubtitleWindow class.
        void SubtitleModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not WpfButton button || button.Icon is not SymbolIcon symbolIcon) return;

            if (subtitleWindow == null)
            {
                subtitleWindow = new Window
                {
                    Title = "Subtitle Mode",
                    Width = 800,
                    Height = 150,
                    MinWidth = 400,
                    MinHeight = 100,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = new SolidColorBrush(Colors.Transparent),
                    Topmost = true,
                    ShowInTaskbar = false,
                    ResizeMode = ResizeMode.CanResize
                };

                var resizeGrid = new SystemControls.Grid();

                var border = new SystemControls.Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(5)
                };

                var contentGrid = new SystemControls.Grid();
                contentGrid.RowDefinitions.Add(new SystemControls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                contentGrid.RowDefinitions.Add(new SystemControls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var originalText = new SystemControls.TextBlock
                {
                    Foreground = new SolidColorBrush(Colors.White),
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(10),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Left,
                    LineHeight = 24,
                    FontFamily = new FontFamily("Microsoft YaHei")
                };
                originalText.SetBinding(SystemControls.TextBlock.TextProperty, new Binding("PresentedCaption") { Source = App.Captions });
                SystemControls.Grid.SetRow(originalText, 0);

                var translatedText = new SystemControls.TextBlock
                {
                    Foreground = new SolidColorBrush(Colors.White),
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(10),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Left,
                    LineHeight = 24,
                    FontFamily = new FontFamily("Microsoft YaHei")
                };
                translatedText.SetBinding(SystemControls.TextBlock.TextProperty, new Binding("TranslatedCaption") { Source = App.Captions });
                SystemControls.Grid.SetRow(translatedText, 1);

                void UpdateFontSize(SystemControls.TextBlock textBlock, double containerHeight)
                {
                    textBlock.FontSize = Math.Max(14, Math.Min(22, containerHeight / 4));
                }

                subtitleWindow.SizeChanged += (s, e) =>
                {
                    double rowHeight = subtitleWindow.ActualHeight / 2;
                    UpdateFontSize(originalText, rowHeight);
                    UpdateFontSize(translatedText, rowHeight);
                };

                contentGrid.Children.Add(originalText);
                contentGrid.Children.Add(translatedText);
                border.Child = contentGrid;
                resizeGrid.Children.Add(border);

                const int resizeMargin = 8;
                resizeGrid.MouseMove += (s, e) =>
                {
                    if (isResizing)
                    {
                        var currentPos = e.GetPosition(subtitleWindow);
                        double diffX = currentPos.X - startPoint.X;
                        double diffY = currentPos.Y - startPoint.Y;

                        if (resizeGrid.Cursor == Cursors.SizeWE)
                        {
                            if (startPoint.X < resizeGrid.ActualWidth / 2)
                            {
                                double newWidth = Math.Max(subtitleWindow.MinWidth, startSize.Width - diffX);
                                subtitleWindow.Left = startLocation.X + (startSize.Width - newWidth);
                                subtitleWindow.Width = newWidth;
                            }
                            else
                            {
                                subtitleWindow.Width = Math.Max(subtitleWindow.MinWidth, startSize.Width + diffX);
                            }
                        }
                        else if (resizeGrid.Cursor == Cursors.SizeNS)
                        {
                            if (startPoint.Y < resizeGrid.ActualHeight / 2)
                            {
                                double newHeight = Math.Max(subtitleWindow.MinHeight, startSize.Height - diffY);
                                subtitleWindow.Top = startLocation.Y + (startSize.Height - newHeight);
                                subtitleWindow.Height = newHeight;
                            }
                            else
                            {
                                subtitleWindow.Height = Math.Max(subtitleWindow.MinHeight, startSize.Height + diffY);
                            }
                        }
                        else if (resizeGrid.Cursor == Cursors.SizeNWSE)
                        {
                            if (startPoint.X < resizeGrid.ActualWidth / 2)
                            {
                                double newWidth = Math.Max(subtitleWindow.MinWidth, startSize.Width - diffX);
                                double newHeight = Math.Max(subtitleWindow.MinHeight, startSize.Height - diffY);
                                subtitleWindow.Left = startLocation.X + (startSize.Width - newWidth);
                                subtitleWindow.Top = startLocation.Y + (startSize.Height - newHeight);
                                subtitleWindow.Width = newWidth;
                                subtitleWindow.Height = newHeight;
                            }
                            else
                            {
                                subtitleWindow.Width = Math.Max(subtitleWindow.MinWidth, startSize.Width + diffX);
                                subtitleWindow.Height = Math.Max(subtitleWindow.MinHeight, startSize.Height + diffY);
                            }
                        }
                        else if (resizeGrid.Cursor == Cursors.SizeNESW)
                        {
                            if (startPoint.X > resizeGrid.ActualWidth / 2)
                            {
                                subtitleWindow.Width = Math.Max(subtitleWindow.MinWidth, startSize.Width + diffX);
                                double newHeight = Math.Max(subtitleWindow.MinHeight, startSize.Height - diffY);
                                subtitleWindow.Top = startLocation.Y + (startSize.Height - newHeight);
                                subtitleWindow.Height = newHeight;
                            }
                            else
                            {
                                double newWidth = Math.Max(subtitleWindow.MinWidth, startSize.Width - diffX);
                                subtitleWindow.Left = startLocation.X + (startSize.Width - newWidth);
                                subtitleWindow.Width = newWidth;
                                subtitleWindow.Height = Math.Max(subtitleWindow.MinHeight, startSize.Height + diffY);
                            }
                        }
                        return;
                    }

                    var pos = e.GetPosition(resizeGrid);
                    bool left = pos.X < resizeMargin;
                    bool right = pos.X > resizeGrid.ActualWidth - resizeMargin;
                    bool top = pos.Y < resizeMargin;
                    bool bottom = pos.Y > resizeGrid.ActualHeight - resizeMargin;

                    if (left && top || right && bottom)
                        resizeGrid.Cursor = Cursors.SizeNWSE;
                    else if (right && top || left && bottom)
                        resizeGrid.Cursor = Cursors.SizeNESW;
                    else if (left || right)
                        resizeGrid.Cursor = Cursors.SizeWE;
                    else if (top || bottom)
                        resizeGrid.Cursor = Cursors.SizeNS;
                    else if (pos.Y <= 30)
                        resizeGrid.Cursor = Cursors.Hand;
                    else
                        resizeGrid.Cursor = null;
                };

                resizeGrid.MouseLeftButtonDown += (s, e) =>
                {
                    var pos = e.GetPosition(resizeGrid);
                    bool isEdge = pos.X < resizeMargin || pos.X > resizeGrid.ActualWidth - resizeMargin ||
                                  pos.Y < resizeMargin || pos.Y > resizeGrid.ActualHeight - resizeMargin;

                    if (isEdge)
                    {
                        isResizing = true;
                        startPoint = pos;
                        startSize = new Size(subtitleWindow.Width, subtitleWindow.Height);
                        startLocation = new Point(subtitleWindow.Left, subtitleWindow.Top);
                        resizeGrid.CaptureMouse();
                        e.Handled = true;
                    }
                    else if (e.ClickCount == 2)
                    {
                        subtitleWindow.WindowState = subtitleWindow.WindowState == WindowState.Maximized
                            ? WindowState.Normal
                            : WindowState.Maximized;
                    }
                    else if (pos.Y <= 30)
                    {
                        subtitleWindow?.DragMove();
                    }
                };

                resizeGrid.MouseLeftButtonUp += (s, e) =>
                {
                    isResizing = false;
                    resizeGrid.ReleaseMouseCapture();
                };

                subtitleWindow.Content = resizeGrid;
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

        // TODO: Extract them into a new SubtitleWindow class.
        void TranslationOnlyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not WpfButton button || button.Icon is not SymbolIcon symbolIcon) return;

            if (translationOnlyWindow == null)
            {
                translationOnlyWindow = new Window
                {
                    Title = "Translation Only Mode",
                    Width = 800,
                    Height = 80,
                    MinWidth = 400,
                    MinHeight = 50,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = new SolidColorBrush(Colors.Transparent),
                    Topmost = true,
                    ShowInTaskbar = false,
                    ResizeMode = ResizeMode.CanResize
                };

                var resizeGrid = new SystemControls.Grid();

                var border = new SystemControls.Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(5)
                };

                var translatedText = new SystemControls.TextBlock
                {
                    Foreground = new SolidColorBrush(Colors.White),
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                translatedText.SetBinding(SystemControls.TextBlock.TextProperty, new Binding("TranslatedCaption") { Source = App.Captions });

                void UpdateFontSize(SystemControls.TextBlock textBlock, double containerHeight)
                {
                    textBlock.FontSize = Math.Max(12, Math.Min(24, containerHeight / 4));
                }

                translationOnlyWindow.SizeChanged += (s, e) =>
                {
                    UpdateFontSize(translatedText, translationOnlyWindow.ActualHeight);
                };

                border.Child = translatedText;
                resizeGrid.Children.Add(border);

                const int resizeMargin = 8;
                resizeGrid.MouseMove += (s, e) =>
                {
                    if (isResizing)
                    {
                        var currentPos = e.GetPosition(translationOnlyWindow);
                        double diffX = currentPos.X - startPoint.X;
                        double diffY = currentPos.Y - startPoint.Y;

                        if (resizeGrid.Cursor == Cursors.SizeWE)
                        {
                            if (startPoint.X < resizeGrid.ActualWidth / 2)
                            {
                                double newWidth = Math.Max(translationOnlyWindow.MinWidth, startSize.Width - diffX);
                                translationOnlyWindow.Left = startLocation.X + (startSize.Width - newWidth);
                                translationOnlyWindow.Width = newWidth;
                            }
                            else
                            {
                                translationOnlyWindow.Width = Math.Max(translationOnlyWindow.MinWidth, startSize.Width + diffX);
                            }
                        }
                        else if (resizeGrid.Cursor == Cursors.SizeNS)
                        {
                            if (startPoint.Y < resizeGrid.ActualHeight / 2)
                            {
                                double newHeight = Math.Max(translationOnlyWindow.MinHeight, startSize.Height - diffY);
                                translationOnlyWindow.Top = startLocation.Y + (startSize.Height - newHeight);
                                translationOnlyWindow.Height = newHeight;
                            }
                            else
                            {
                                translationOnlyWindow.Height = Math.Max(translationOnlyWindow.MinHeight, startSize.Height + diffY);
                            }
                        }
                        return;
                    }

                    var pos = e.GetPosition(resizeGrid);
                    bool left = pos.X < resizeMargin;
                    bool right = pos.X > resizeGrid.ActualWidth - resizeMargin;
                    bool top = pos.Y < resizeMargin;
                    bool bottom = pos.Y > resizeGrid.ActualHeight - resizeMargin;

                    if (left || right)
                        resizeGrid.Cursor = Cursors.SizeWE;
                    else if (top || bottom)
                        resizeGrid.Cursor = Cursors.SizeNS;
                    else if (pos.Y <= 30)
                        resizeGrid.Cursor = Cursors.Hand;
                    else
                        resizeGrid.Cursor = null;
                };

                resizeGrid.MouseLeftButtonDown += (s, e) =>
                {
                    var pos = e.GetPosition(resizeGrid);
                    bool isEdge = pos.X < resizeMargin || pos.X > resizeGrid.ActualWidth - resizeMargin ||
                                  pos.Y < resizeMargin || pos.Y > resizeGrid.ActualHeight - resizeMargin;

                    if (isEdge)
                    {
                        isResizing = true;
                        startPoint = pos;
                        startSize = new Size(translationOnlyWindow.Width, translationOnlyWindow.Height);
                        startLocation = new Point(translationOnlyWindow.Left, translationOnlyWindow.Top);
                        resizeGrid.CaptureMouse();
                        e.Handled = true;
                    }
                    else if (pos.Y <= 30)
                    {
                        translationOnlyWindow?.DragMove();
                    }
                };

                resizeGrid.MouseLeftButtonUp += (s, e) =>
                {
                    isResizing = false;
                    resizeGrid.ReleaseMouseCapture();
                };

                translationOnlyWindow.Content = resizeGrid;
                translationOnlyWindow.Show();

                symbolIcon.Filled = true;
            }
            else
            {
                translationOnlyWindow.Close();
                translationOnlyWindow = null;
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
        
        protected override void OnClosed(EventArgs e)
        {
            subtitleWindow?.Close();
            translationOnlyWindow?.Close();
            base.OnClosed(e);
        }
    }
}
