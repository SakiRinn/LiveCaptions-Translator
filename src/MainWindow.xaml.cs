using LiveCaptionsTranslator.models;
using System.Windows;
using WpfButton = Wpf.Ui.Controls.Button;
using WpfTextBlock = Wpf.Ui.Controls.TextBlock;
using SystemControls = System.Windows.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LiveCaptionsTranslator
{
    public partial class MainWindow : FluentWindow
    {
        private Window? subtitleWindow = null;

        public MainWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();

            Loaded += async (sender, args) =>
            {
                SystemThemeWatcher.Watch(
                    this,                                   
                    WindowBackdropType.Mica,                
                    true                                    
                );
                RootNavigation.Navigate(typeof(CaptionPage));
            };
        }

        void TopmostButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not WpfButton button || button.Icon is not SymbolIcon symbolIcon) return;
            
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

        async void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not WpfButton button || button.Icon is not SymbolIcon symbolIcon) return;

            if (App.Captions?.PauseFlag ?? false)
            {
                if (App.Window == null)
                {
                    App.Window = await Task.Run(() => LiveCaptionsHandler.LaunchLiveCaptions());
                }
                if (App.Captions != null) App.Captions.PauseFlag = false;
                symbolIcon.Filled = false;
            }
            else
            {
                if (App.Captions != null) App.Captions.PauseFlag = true;
                symbolIcon.Filled = true;
            }
        }

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
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = new SolidColorBrush(Colors.Transparent),
                    Topmost = true,
                    ShowInTaskbar = false
                };

                var grid = new SystemControls.Grid();
                grid.RowDefinitions.Add(new SystemControls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new SystemControls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var originalText = new SystemControls.TextBlock
                {
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 18,
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    Padding = new Thickness(10)
                };
                originalText.SetBinding(SystemControls.TextBlock.TextProperty, new Binding("Original") { Source = App.Captions });
                SystemControls.Grid.SetRow(originalText, 0);

                var translatedText = new SystemControls.TextBlock
                {
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 18,
                    TextWrapping = TextWrapping.Wrap,
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    Padding = new Thickness(10)
                };
                translatedText.SetBinding(SystemControls.TextBlock.TextProperty, new Binding("Translated") { Source = App.Captions });
                SystemControls.Grid.SetRow(translatedText, 1);

                grid.Children.Add(originalText);
                grid.Children.Add(translatedText);
                
                grid.MouseLeftButtonDown += (s, e) => subtitleWindow?.DragMove();

                subtitleWindow.Content = grid;
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

        protected override void OnClosed(EventArgs e)
        {
            subtitleWindow?.Close();
            base.OnClosed(e);
        }
    }
}
