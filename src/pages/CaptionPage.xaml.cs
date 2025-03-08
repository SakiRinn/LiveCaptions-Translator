using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LiveCaptionsTranslator
{
    public partial class CaptionPage : Page
    {
        public static CaptionPage? Instance { get; set; } = null;

        public CaptionPage()
        {
            InitializeComponent();
            DataContext = App.Caption;
            Instance = this;

            Loaded += (s, e) => App.Caption.PropertyChanged += TranslatedChanged;
            Unloaded += (s, e) => App.Caption.PropertyChanged -= TranslatedChanged;

            CollapseTranslatedCaption(App.Setting.MainWindow.CaptionLogEnabled);
        }

        private async void TextBlock_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                try
                {
                    Clipboard.SetText(textBlock.Text);
                    textBlock.ToolTip = "Copied!";
                }
                catch
                {
                    textBlock.ToolTip = "Error to Copy";
                }
                await Task.Delay(500);
                textBlock.ToolTip = "Click to Copy";
            }
        }

        private void TranslatedChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(App.Caption.DisplayTranslatedCaption))
            {
                if (Encoding.UTF8.GetByteCount(App.Caption.DisplayTranslatedCaption) >= 160)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.TranslatedCaption.FontSize = 15;
                    }), DispatcherPriority.Background);
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.TranslatedCaption.FontSize = 18;
                    }), DispatcherPriority.Background);
                }
            }
        }

        public void CollapseTranslatedCaption(bool collapse)
        {
            var converter = new GridLengthConverter();

            if (collapse)
            {
                TranslatedCaption_Row.Height = (GridLength)converter.ConvertFromString("Auto");
                CaptionLogCard.Visibility = Visibility.Visible;
            }
            else
            {
                TranslatedCaption_Row.Height = (GridLength)converter.ConvertFromString("*");
                CaptionLogCard.Visibility = Visibility.Collapsed;
            }
        }
    }
}
