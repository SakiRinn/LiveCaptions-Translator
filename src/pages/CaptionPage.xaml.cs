using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using LiveCaptionsTranslator.Utils;

namespace LiveCaptionsTranslator
{
    public partial class CaptionPage : Page
    {
        public const int CARD_HEIGHT = 110;

        private static CaptionPage instance;
        public static CaptionPage Instance => instance;

        public CaptionPage()
        {
            InitializeComponent();
            DataContext = Translator.Caption;
            instance = this;

            Loaded += (s, e) =>
            {
                AutoHeight();
                (App.Current.MainWindow as MainWindow).CaptionLogButton.Visibility = Visibility.Visible;
            };
            Unloaded += (s, e) =>
            {
                (App.Current.MainWindow as MainWindow).CaptionLogButton.Visibility = Visibility.Collapsed;
            };

            CollapseTranslatedCaption(Translator.Setting.MainWindow.CaptionLogEnabled);
            ApplyFontSizes();
        }

        private async void TextBlock_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                try
                {
                    Clipboard.SetText(textBlock.Text);
                    SnackbarHost.Show("Copied.", textBlock.Text, SnackbarType.Info, 100);
                }
                catch
                {
                    SnackbarHost.Show("Copy Failed.", string.Empty, SnackbarType.Error, 100);
                }
                await Task.Delay(500);
            }
        }

        private void ApplyFontSizes()
        {
            OriginalCaption.FontSize = Translator.Setting.MainWindow.OriginalFontSize;
            TranslatedCaption.FontSize = Translator.Setting.MainWindow.TranslatedFontSize;
        }

        private void OriginalCard_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
                return;
            Translator.Setting.MainWindow.OriginalFontSize =
                AdjustFontSize(Translator.Setting.MainWindow.OriginalFontSize, e.Delta);
            ApplyFontSizes();
            e.Handled = true;
        }

        private void TranslatedCard_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
                return;
            Translator.Setting.MainWindow.TranslatedFontSize =
                AdjustFontSize(Translator.Setting.MainWindow.TranslatedFontSize, e.Delta);
            ApplyFontSizes();
            e.Handled = true;
        }

        private static int AdjustFontSize(int current, int wheelDelta)
        {
            int next = current + (wheelDelta > 0 ? StyleConsts.DELTA_FONT_SIZE : -StyleConsts.DELTA_FONT_SIZE);
            return Math.Clamp(next, StyleConsts.MIN_FONT_SIZE, StyleConsts.MAX_FONT_SIZE);
        }

        public void CollapseTranslatedCaption(bool isCollapsed)
        {
            var converter = new GridLengthConverter();

            if (isCollapsed)
            {
                TranslatedCaption_Row.Height = (GridLength)converter.ConvertFromString("Auto");
                LogCards.Visibility = Visibility.Visible;
            }
            else
            {
                TranslatedCaption_Row.Height = (GridLength)converter.ConvertFromString("*");
                LogCards.Visibility = Visibility.Collapsed;
            }
        }

        public void AutoHeight()
        {
            if (Translator.Setting.MainWindow.CaptionLogEnabled)
                (App.Current.MainWindow as MainWindow).AutoHeightAdjust(
                    minHeight: CARD_HEIGHT * (Translator.Setting.DisplaySentences + 1),
                    maxHeight: CARD_HEIGHT * (Translator.Setting.DisplaySentences + 1));
            else
                (App.Current.MainWindow as MainWindow).AutoHeightAdjust(
                    minHeight: (int)App.Current.MainWindow.MinHeight,
                    maxHeight: (int)App.Current.MainWindow.MinHeight);
        }
    }
}
