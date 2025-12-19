using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Controls;

using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class OverlayWindow : Window
    {
        private Dictionary<int, Brush> ColorList = new Dictionary<int, Brush> {
            {1, Brushes.White},
            {2, Brushes.Yellow},
            {3, Brushes.LimeGreen},
            {4, Brushes.Aqua},
            {5, Brushes.Blue},
            {6, Brushes.DeepPink},
            {7, Brushes.Red},
            {8, Brushes.Black},
        };
        private int onlyMode = 0;

        public int OnlyMode
        {
            get => onlyMode;
            set
            {
                onlyMode = value;
                ResizeForOnlyMode();
            }
        }

        public OverlayWindow()
        {
            InitializeComponent();
            DataContext = Translator.Caption;

            Loaded += (s, e) => Translator.Caption.PropertyChanged += TranslatedChanged;
            Unloaded += (s, e) => Translator.Caption.PropertyChanged -= TranslatedChanged;

            this.OriginalCaption.FontWeight =
                (Translator.Setting.OverlayWindow.FontBold == 3 ? FontWeights.Bold : FontWeights.Regular);
            this.TranslatedCaption.FontWeight =
                (Translator.Setting.OverlayWindow.FontBold >= 2 ? FontWeights.Bold : FontWeights.Regular);

            this.OriginalCaptionShadow.Opacity =
                (Translator.Setting.OverlayWindow.FontShadow == 3 ? 1.0 : 0.0);
            this.TranslatedCaptionShadow.Opacity =
                (Translator.Setting.OverlayWindow.FontShadow >= 2 ? 1.0 : 0.0);

            this.OriginalCaption.StrokeThickness =
                (Translator.Setting.OverlayWindow.FontStroke == 3 ? 3 : 0);
            this.TranslatedCaption.StrokeThickness =
                (Translator.Setting.OverlayWindow.FontStroke >= 2 ? 3 : 0);

            this.TranslatedCaption.Foreground = ColorList[Translator.Setting.OverlayWindow.FontColor];
            this.OriginalCaption.Foreground = ColorList[Translator.Setting.OverlayWindow.FontColor];
            this.BorderBackground.Background = ColorList[Translator.Setting.OverlayWindow.BackgroundColor];
            this.BorderBackground.Opacity = Translator.Setting.OverlayWindow.Opacity;

            ApplyFontSize();
            ApplyBackgroundOpacity();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void TopThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            double newHeight = this.Height - e.VerticalChange;

            if (newHeight >= this.MinHeight)
            {
                this.Top += e.VerticalChange;
                this.Height = newHeight;
            }
        }

        private void BottomThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            double newHeight = this.Height + e.VerticalChange;

            if (newHeight >= this.MinHeight)
            {
                this.Height = newHeight;
            }
        }

        private void LeftThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = this.Width - e.HorizontalChange;

            if (newWidth >= this.MinWidth)
            {
                this.Left += e.HorizontalChange;
                this.Width = newWidth;
            }
        }

        private void RightThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = this.Width + e.HorizontalChange;

            if (newWidth >= this.MinWidth)
            {
                this.Width = newWidth;
            }
        }

        private void TopLeftThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            TopThumb_OnDragDelta(sender, e);
            LeftThumb_OnDragDelta(sender, e);
        }

        private void TopRightThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            TopThumb_OnDragDelta(sender, e);
            RightThumb_OnDragDelta(sender, e);
        }

        private void BottomLeftThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            BottomThumb_OnDragDelta(sender, e);
            LeftThumb_OnDragDelta(sender, e);
        }

        private void BottomRightThumb_OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            BottomThumb_OnDragDelta(sender, e);
            RightThumb_OnDragDelta(sender, e);
        }

        private void TranslatedChanged(object sender, PropertyChangedEventArgs e)
        {
            ApplyFontSize();
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            ControlPanel.Visibility = Visibility.Visible;
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            ControlPanel.Visibility = Visibility.Hidden;
        }

        private void FontIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting.OverlayWindow.FontSize + 1 < 60)
            {
                Translator.Setting.OverlayWindow.FontSize++;
                ApplyFontSize();
            }
        }

        private void FontDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting.OverlayWindow.FontSize - 1 > 8)
            {
                Translator.Setting.OverlayWindow.FontSize--;
                ApplyFontSize();
            }
        }

        private void FontBold_Click(object sender, RoutedEventArgs e)
        {
            Translator.Setting.OverlayWindow.FontBold++;
            if (Translator.Setting.OverlayWindow.FontBold > 3)
                Translator.Setting.OverlayWindow.FontBold = 1;
            this.OriginalCaption.FontWeight =
                (Translator.Setting.OverlayWindow.FontBold == 3 ? FontWeights.Bold : FontWeights.Regular);
            this.TranslatedCaption.FontWeight =
                (Translator.Setting.OverlayWindow.FontBold >= 2 ? FontWeights.Bold : FontWeights.Regular);
        }

        private void FontShadow_Click(object sender, RoutedEventArgs e)
        {
            Translator.Setting.OverlayWindow.FontShadow++;
            if (Translator.Setting.OverlayWindow.FontShadow > 3)
                Translator.Setting.OverlayWindow.FontShadow = 1;
            this.OriginalCaptionShadow.Opacity =
                (Translator.Setting.OverlayWindow.FontShadow == 3 ? 1.0 : 0.0);
            this.TranslatedCaptionShadow.Opacity =
                (Translator.Setting.OverlayWindow.FontShadow >= 2 ? 1.0 : 0.0);
        }

        private void FontStroke_Click(object sender, RoutedEventArgs e)
        {
            Translator.Setting.OverlayWindow.FontStroke++;
            if (Translator.Setting.OverlayWindow.FontStroke > 3)
                Translator.Setting.OverlayWindow.FontStroke = 1;
            this.OriginalCaption.StrokeThickness =
                (Translator.Setting.OverlayWindow.FontStroke == 3 ? 3 : 0);
            this.TranslatedCaption.StrokeThickness =
                (Translator.Setting.OverlayWindow.FontStroke >= 2 ? 3 : 0);
        }

        private void FontColorCycle_Click(object sender, RoutedEventArgs e)
        {
            Translator.Setting.OverlayWindow.FontColor++;
            if (Translator.Setting.OverlayWindow.FontColor > ColorList.Count)
                Translator.Setting.OverlayWindow.FontColor = 1;
            TranslatedCaption.Foreground = ColorList[Translator.Setting.OverlayWindow.FontColor];
            OriginalCaption.Foreground = ColorList[Translator.Setting.OverlayWindow.FontColor];
        }

        private void OpacityIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting.OverlayWindow.Opacity + 20 < 251)
                Translator.Setting.OverlayWindow.Opacity += 20;
            else
                Translator.Setting.OverlayWindow.Opacity = 251;
            ApplyBackgroundOpacity();
        }

        private void OpacityDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting.OverlayWindow.Opacity - 20 > 1)
                Translator.Setting.OverlayWindow.Opacity -= 20;
            else
                Translator.Setting.OverlayWindow.Opacity = 1;
            ApplyBackgroundOpacity();
        }

        private void BackgroundColorCycle_Click(object sender, RoutedEventArgs e)
        {
            Translator.Setting.OverlayWindow.BackgroundColor++;
            if (Translator.Setting.OverlayWindow.BackgroundColor > ColorList.Count)
                Translator.Setting.OverlayWindow.BackgroundColor = 1;
            BorderBackground.Background = ColorList[Translator.Setting.OverlayWindow.BackgroundColor];

            BorderBackground.Opacity = Translator.Setting.OverlayWindow.Opacity;
            ApplyBackgroundOpacity();
        }

        private void OnlyModeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            if (onlyMode == 2)
            {
                // (0) Subtitle + Translation
                symbolIcon.Symbol = SymbolRegular.PanelBottom20;
                OnlyMode = 0;
            }
            else if (onlyMode == 0)
            {
                // (1) Translation Only
                symbolIcon.Symbol = SymbolRegular.PanelTopExpand20;
                OnlyMode = 1;
            }
            else
            {
                // (2) Subtitle Only
                symbolIcon.Symbol = SymbolRegular.PanelTopContract20;
                OnlyMode = 2;
            }
        }

        private void ClickThrough_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var extendedStyle = WindowsAPI.GetWindowLong(hwnd, WindowsAPI.GWL_EXSTYLE);
            WindowsAPI.SetWindowLong(hwnd, WindowsAPI.GWL_EXSTYLE, extendedStyle | WindowsAPI.WS_EX_TRANSPARENT);
            ControlPanel.Visibility = Visibility.Collapsed;
        }

        public void ApplyFontSize()
        {
            if (Encoding.UTF8.GetByteCount(Translator.Caption.OverlayTranslatedCaption) >= TextUtil.VERYLONG_THRESHOLD)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.OriginalCaption.FontSize = Translator.Setting.OverlayWindow.FontSize;
                    this.TranslatedCaption.FontSize = (int)(this.OriginalCaption.FontSize * 1.1);
                }), DispatcherPriority.Background);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.OriginalCaption.FontSize = Translator.Setting.OverlayWindow.FontSize;
                    this.TranslatedCaption.FontSize = (int)(this.OriginalCaption.FontSize * 1.25);
                }), DispatcherPriority.Background);
            }
        }

        public void ResizeForOnlyMode()
        {
            if (onlyMode == 1)
            {
                // (1) Translation Only
                OriginalCaptionCard.Visibility = Visibility.Collapsed;
                this.MinHeight -= 40;
                this.Height -= 40;
                this.Top += 40;
            }
            if (onlyMode == 2)
            {
                // restore
                OriginalCaptionCard.Visibility = Visibility.Visible;
                this.Top -= 40;
                this.Height += 40;
                this.MinHeight += 40;

                // (2) Subtitle Only
                TranslatedCaptionCard.Visibility = Visibility.Collapsed;
                this.MinHeight -= 40;
                this.Height -= 40;
            }
            else if (onlyMode == 0)
            {
                // restore
                TranslatedCaptionCard.Visibility = Visibility.Visible;
                this.Height += 40;
                this.MinHeight += 40;
            }
        }

        public void ApplyBackgroundOpacity()
        {
            Color color = ((SolidColorBrush)BorderBackground.Background).Color;
            BorderBackground.Background = new SolidColorBrush(
                Color.FromArgb(Translator.Setting.OverlayWindow.Opacity, color.R, color.G, color.B));
        }
    }
}
