using System.ComponentModel;
using System.Diagnostics;
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
            {2, Brushes.Black},
            {3, Brushes.Yellow},
            {4, Brushes.Blue},
            {5, Brushes.LawnGreen},
            {6, Brushes.Red},
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
                if (Encoding.UTF8.GetByteCount(App.Captions.DisplayTranslatedCaption) >= 160)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.TranslatedCaption.FontSize = App.Settings.OverlayFontSize;
                    }), DispatcherPriority.Background);
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.TranslatedCaption.FontSize = App.Settings.OverlayFontSize + 3;
                    }), DispatcherPriority.Background);
                }
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

        protected async override void OnSourceInitialized(EventArgs e)
        {
            int opacity = App.Settings.OverlayOpacity;

            TranslatedCaption.FontWeight = (App.Settings.OverlayFontBold ? FontWeights.Bold : FontWeights.Regular);
            TranslatedCaption.Foreground = ColorList[App.Settings.OverlayFontColor];
            OriginalCaption.Foreground = ColorList[App.Settings.OverlayFontColor];
            BorderBackground.Background = new SolidColorBrush(Color.FromArgb((byte)opacity, (byte)0, (byte)0, (byte)0));
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
            App.Settings.OverlayFontSize++;
        }

        private void FontDecrease_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.OverlayFontSize--;
        }

        private void OpacityIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (App.Settings.OverlayOpacity + 10 < 255)
            {
                App.Settings.OverlayOpacity += 10;
                BorderBackground.Background = new SolidColorBrush(Color.FromArgb((byte)App.Settings.OverlayOpacity, (byte)0, (byte)0, (byte)0));
            }
        }

        private void OpacityDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (App.Settings.OverlayOpacity - 10 > 0)
            {

                App.Settings.OverlayOpacity -= 10;
                BorderBackground.Background = new SolidColorBrush(Color.FromArgb((byte)App.Settings.OverlayOpacity, (byte)0, (byte)0, (byte)0));
            }
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
            App.Settings.OverlayFontBold = !App.Settings.OverlayFontBold;
            TranslatedCaption.FontWeight = (App.Settings.OverlayFontBold ? FontWeights.Bold : FontWeights.Regular);
        }
    }
}
