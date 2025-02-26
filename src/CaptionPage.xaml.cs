using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LiveCaptionsTranslator
{
    public partial class CaptionPage : Page
    {
        public CaptionPage()
        {
            InitializeComponent();
            DataContext = App.Captions;
            App.Captions.PropertyChanged += TranslatedChanged;
        }

        private void TranslatedChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(App.Captions.DisplayTranslatedCaption))
            {
                if (Encoding.UTF8.GetByteCount(App.Captions.DisplayTranslatedCaption) >= 128)
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

        private async void TextBlock_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                Clipboard.SetText(textBlock.Text);
                textBlock.ToolTip = "Copied!";
                await Task.Delay(500);
                textBlock.ToolTip = "Click to Copy";
            }
        }
    }
}
