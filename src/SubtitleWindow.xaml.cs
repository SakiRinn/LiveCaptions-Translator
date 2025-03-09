using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace LiveCaptionsTranslator
{
    public partial class SubtitleWindow : Window
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

        private bool isTranslationOnly = false;
        public bool IsTranslationOnly
        {
            get => isTranslationOnly;
            set
            {
                isTranslationOnly = value;
                ResizeForTranslationOnly();
            }
        }

        public SubtitleWindow()
        {
            InitializeComponent();
            DataContext = App.Caption;

            Loaded += (s, e) => App.Caption.PropertyChanged += TranslatedChanged;
            Unloaded += (s, e) => App.Caption.PropertyChanged -= TranslatedChanged;

            this.OriginalCaption.FontWeight = (App.Setting.SubtitleWindow.FontBold == 3 ? FontWeights.Bold : FontWeights.Regular);
            this.TranslatedCaption.FontWeight = (App.Setting.SubtitleWindow.FontBold >= 2 ? FontWeights.Bold : FontWeights.Regular);

            this.TranslatedCaption.Foreground = ColorList[App.Setting.SubtitleWindow.FontColor];
            this.OriginalCaption.Foreground = ColorList[App.Setting.SubtitleWindow.FontColor];
            this.BorderBackground.Background = ColorList[App.Setting.SubtitleWindow.BackgroundColor];
            this.BorderBackground.Opacity = App.Setting.SubtitleWindow.Opacity;

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
            if (e.PropertyName == nameof(App.Caption.DisplayTranslatedCaption))
            {
                ApplyFontSize();
            }
        }
        private void ApplyFontSize()
        {
            if (Encoding.UTF8.GetByteCount(App.Caption.DisplayTranslatedCaption) >= 160)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.OriginalCaption.FontSize = App.Setting.SubtitleWindow.FontSize;
                    this.TranslatedCaption.FontSize = this.OriginalCaption.FontSize + 4;
                }), DispatcherPriority.Background);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.OriginalCaption.FontSize = App.Setting.SubtitleWindow.FontSize + 3;
                    this.TranslatedCaption.FontSize = this.OriginalCaption.FontSize + 4;
                }), DispatcherPriority.Background);
            }
        }

        public void ResizeForTranslationOnly()
        {
            if (isTranslationOnly)
            {
                OriginalCaptionCard.Visibility = Visibility.Collapsed;
                if (this.MinHeight > 40 && this.Height > 40)
                {
                    this.MinHeight -= 40;
                    this.Height -= 40;
                    this.Top += 40;
                }
            }
            else if (OriginalCaptionCard.Visibility == Visibility.Collapsed)
            {
                OriginalCaptionCard.Visibility = Visibility.Visible;
                this.Top -= 40;
                this.Height += 40;
                this.MinHeight += 40;
            }
        }

        // Control Panel

        private void SetWindowExTransparent(IntPtr hwnd)
        {
            const int WS_EX_TRANSPARENT = 0x00000020;
            const int GWL_EXSTYLE = (-20);

            [DllImport("user32.dll")]
            static extern int GetWindowLong(IntPtr hwnd, int index);

            [DllImport("user32.dll")]
            static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
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
            if (App.Setting.SubtitleWindow.FontSize + 1 < 60)
            {
                App.Setting.SubtitleWindow.FontSize++;
                ApplyFontSize();
            }
        }

        private void FontDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (App.Setting.SubtitleWindow.FontSize - 1 > 8)
            {
                App.Setting.SubtitleWindow.FontSize--;
                ApplyFontSize();
            }
        }

        private void FontBold_Click(object sender, RoutedEventArgs e)
        {
            App.Setting.SubtitleWindow.FontBold++;
            if (App.Setting.SubtitleWindow.FontBold > (isTranslationOnly ? 2 : 3))
                App.Setting.SubtitleWindow.FontBold = 1;
            this.OriginalCaption.FontWeight =
                (App.Setting.SubtitleWindow.FontBold == 3 ? FontWeights.Bold : FontWeights.Regular);
            this.TranslatedCaption.FontWeight =
                (App.Setting.SubtitleWindow.FontBold >= 2 ? FontWeights.Bold : FontWeights.Regular);
        }

        private void FontShadow_Click(object sender, RoutedEventArgs e)
        {
            App.Setting.SubtitleWindow.FontShadow++;
            if (App.Setting.SubtitleWindow.FontShadow > (isTranslationOnly ? 2 : 3))
                App.Setting.SubtitleWindow.FontShadow = 1;
            this.OriginalCaptionShadow.Opacity =
                (App.Setting.SubtitleWindow.FontShadow == 3 ? 1.0 : 0.0);
            this.TranslatedCaptionShadow.Opacity =
                (App.Setting.SubtitleWindow.FontShadow >= 2 ? 1.0 : 0.0);
        }

        private void FontColorCycle_Click(object sender, RoutedEventArgs e)
        {
            App.Setting.SubtitleWindow.FontColor++;
            if (App.Setting.SubtitleWindow.FontColor > ColorList.Count)
                App.Setting.SubtitleWindow.FontColor = 1;
            TranslatedCaption.Foreground = ColorList[App.Setting.SubtitleWindow.FontColor];
            OriginalCaption.Foreground = ColorList[App.Setting.SubtitleWindow.FontColor];
        }

        private void OpacityIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (App.Setting.SubtitleWindow.Opacity + 20 < 251)
                App.Setting.SubtitleWindow.Opacity += 20;
            else
                App.Setting.SubtitleWindow.Opacity = 251;
            ApplyBackgroundOpacity();
        }

        private void OpacityDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (App.Setting.SubtitleWindow.Opacity - 20 > 1)
                App.Setting.SubtitleWindow.Opacity -= 20;
            else
                App.Setting.SubtitleWindow.Opacity = 1;

            ApplyBackgroundOpacity();
        }

        private void BackgroundColorCycle_Click(object sender, RoutedEventArgs e)
        {
            App.Setting.SubtitleWindow.BackgroundColor++;
            if (App.Setting.SubtitleWindow.BackgroundColor > ColorList.Count)
                App.Setting.SubtitleWindow.BackgroundColor = 1;
            BorderBackground.Background = ColorList[App.Setting.SubtitleWindow.BackgroundColor];
            BorderBackground.Opacity = App.Setting.SubtitleWindow.Opacity;
        }

        private void ClickThrough_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowExTransparent(hwnd);
            ControlPanel.Visibility = Visibility.Collapsed;
        }

        private void ApplyBackgroundOpacity()
        {
            Color color = ((SolidColorBrush)BorderBackground.Background).Color;
            BorderBackground.Background = new SolidColorBrush(
                Color.FromArgb(App.Setting.SubtitleWindow.Opacity, color.R, color.G, color.B));
        }
    }
}
