﻿using System.ComponentModel;
using System.Text;
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

        private void ClearHistory_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            App.Captions.ClearHistory();
        }

        private void TranslatedChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(App.Captions.Translated))
            {
                if (Encoding.UTF8.GetByteCount(App.Captions.Translated) > 150)
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
    }
}
