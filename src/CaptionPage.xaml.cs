using System.ComponentModel;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
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

            EnagleCaptionLog(App.Settings.EnableCaptionLog);
        }

        private void TranslatedChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(App.Captions.TranslatedCaption))
            {
                if (Encoding.UTF8.GetByteCount(App.Captions.TranslatedCaption) > 150)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TranslatedCaption.FontSize = 15;
                    }), DispatcherPriority.Background);
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TranslatedCaption.FontSize = 18;
                    }), DispatcherPriority.Background);
                }
            }
        }

        private void ClearHistory_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            App.Captions.ClearHistory();
        }

        private void EnableCaptionLog_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var Swtich = sender as ToggleSwitch;
            EnagleCaptionLog(Swtich.IsChecked.Value);
        }

        private void EnagleCaptionLog(bool enable)
        {  
            if (enable)
            {
                CaptionLogCard.Visibility = System.Windows.Visibility.Visible;
                ClearCaptionLog.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                CaptionLogCard.Visibility = System.Windows.Visibility.Collapsed;
                ClearCaptionLog.Visibility = System.Windows.Visibility.Collapsed;
                App.Captions.ClearHistory();
            }
            App.Settings.EnableCaptionLog = enable;
            EnableCaptionLog.IsChecked = enable;
        }
    }
}
