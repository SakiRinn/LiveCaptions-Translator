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
            DataContext = App.Captions;

            Loaded += (s, e) => App.Captions.PropertyChanged += TranslatedChanged;
            Unloaded += (s, e) => App.Captions.PropertyChanged -= TranslatedChanged;

            ApplyFontSize();
            TranslatedCaption.Foreground = ColorList[App.Settings.OverlayFontColor];
            OriginalCaption.Foreground = ColorList[App.Settings.OverlayFontColor];
            BorderBackground.Background = ColorList[App.Settings.OverlayBackgroundColor];
            BorderBackground.Opacity = App.Settings.OverlayOpacity;
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
            if (e.PropertyName == nameof(App.Captions.DisplayTranslatedCaption))
            {
                ApplyFontSize();
            }
        }
        private void ApplyFontSize()
        {
            if (Encoding.UTF8.GetByteCount(App.Captions.DisplayTranslatedCaption) >= 160)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.OriginalCaption.FontSize = App.Settings.OverlayFontSize;
                    this.TranslatedCaption.FontSize = this.OriginalCaption.FontSize + 4;
                }), DispatcherPriority.Background);
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.OriginalCaption.FontSize = App.Settings.OverlayFontSize + 3;
                    this.TranslatedCaption.FontSize = this.OriginalCaption.FontSize + 4;
                }), DispatcherPriority.Background);
            }

            this.OriginalCaption.FontWeight = (App.Settings.OverlayFontBold == 3 ? FontWeights.Bold : FontWeights.Regular);
            this.TranslatedCaption.FontWeight = (App.Settings.OverlayFontBold >= 2 ? FontWeights.Bold : FontWeights.Regular);
            this.OriginalCaptionShadow.Opacity = (App.Settings.OverlayFontShadow == 3 ? 1.0 : 0.0);
            this.TranslatedCaptionShadow.Opacity = (App.Settings.OverlayFontShadow >= 2 ? 1.0 : 0.0);
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
            if (App.Settings.OverlayFontSize + 1 < 60)
            {
                App.Settings.OverlayFontSize++;
                ApplyFontSize();
            }
        }

        private void FontDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (App.Settings.OverlayFontSize - 1 > 8)
            {
                App.Settings.OverlayFontSize--;
                ApplyFontSize();
            }
        }

        private void OpacityIncrease_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.OverlayOpacity += 0.05;
            if (App.Settings.OverlayOpacity >= 1)
                App.Settings.OverlayOpacity = 1.0; // Opacity = 0 make Border unhove
            BorderBackground.Opacity = App.Settings.OverlayOpacity;
        }

        private void OpacityDecrease_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.OverlayOpacity -= 0.05;
            if (App.Settings.OverlayOpacity <= 0)
                App.Settings.OverlayOpacity = 0.01;
            BorderBackground.Opacity = App.Settings.OverlayOpacity;
        }

        private void FontColorCycle_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.OverlayFontColor++;
            if (App.Settings.OverlayFontColor > ColorList.Count)
                App.Settings.OverlayFontColor = 1;
            TranslatedCaption.Foreground = ColorList[App.Settings.OverlayFontColor];
            OriginalCaption.Foreground = ColorList[App.Settings.OverlayFontColor];
        }

        private void ClickThrough_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowExTransparent(hwnd);

            ControlPanel.Visibility = Visibility.Collapsed;
        }

        private void FontBold_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.OverlayFontBold++;
            if (App.Settings.OverlayFontBold > (isTranslationOnly ? 2 : 3))
                App.Settings.OverlayFontBold = 1;
            ApplyFontSize();
        }
        private void BackgroundColorCycle_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.OverlayBackgroundColor++;
            if (App.Settings.OverlayBackgroundColor > ColorList.Count)
                App.Settings.OverlayBackgroundColor = 1;
            BorderBackground.Background = ColorList[App.Settings.OverlayBackgroundColor];
            BorderBackground.Opacity = App.Settings.OverlayOpacity;
        }

        private void FontShadow_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.OverlayFontShadow++;
            if (App.Settings.OverlayFontShadow > (isTranslationOnly ? 2 : 3))
                App.Settings.OverlayFontShadow = 1;
            ApplyFontSize();
        }
    }
}
