using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Wpf.Ui.Controls;

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
            if (sender is System.Windows.Controls.TextBlock textBlock)
            {
                try
                {
                    Clipboard.SetText(textBlock.Text);
                    (Application.Current.MainWindow as MainWindow)?.AddToast(SymbolRegular.Copy16, "Copied To Clipboard!", 1);
                }
                catch
                {
                    (Application.Current.MainWindow as MainWindow)?.AddToast(SymbolRegular.Copy16, "Error To Clipboard!", 1);
                }
            }
        }
    }
}
