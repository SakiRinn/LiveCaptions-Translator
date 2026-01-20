using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Controls;

using LiveCaptionsTranslator.apis;
using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.Utils;
using Button = Wpf.Ui.Controls.Button;
using Color = System.Windows.Media.Color;
using ColorEnum = LiveCaptionsTranslator.Utils.Color;

namespace LiveCaptionsTranslator
{
    public partial class OverlayWindow : Window
    {
        private readonly Dictionary<ColorEnum, SolidColorBrush> colorMap = new ()
        {
            {ColorEnum.White, Brushes.White},
            {ColorEnum.Yellow, Brushes.Yellow},
            {ColorEnum.LimeGreen, Brushes.LimeGreen},
            {ColorEnum.Aqua, Brushes.Aqua},
            {ColorEnum.Blue, Brushes.Blue},
            {ColorEnum.DeepPink, Brushes.DeepPink},
            {ColorEnum.Red, Brushes.Red},
            {ColorEnum.Black, Brushes.Black},
        };
        private CaptionVisible onlyMode = CaptionVisible.Both;

        public CaptionVisible OnlyMode
        {
            get => onlyMode;
            set
            {
                onlyMode = value;
                ResizeForOnlyMode();
            }
        }
        public CaptionLocation SwitchMode { get; set; } = CaptionLocation.TranslationTop;

        private readonly Setting _setting;
        private readonly Caption _caption;

        public OverlayWindow()
        {
            InitializeComponent();

            // Keep overlay captions readable even when app UI is RTL.
            FlowDirection = FlowDirection.LeftToRight;

            _setting = Translator.Setting ?? Setting.Load();
            _caption = Translator.Caption ?? Caption.GetInstance();

            DataContext = _caption;

            // Default: Translation on top, Subtitle on bottom
            Grid.SetRow(TranslatedCaptionCard, 0);
            Grid.SetRow(OriginalCaptionCard, 1);
            SwitchMode = CaptionLocation.TranslationTop;

            Loaded += (_, __) => _caption.PropertyChanged += TranslatedChanged;
            Unloaded += (_, __) => _caption.PropertyChanged -= TranslatedChanged;

            OriginalCaption.FontWeight = _setting.OverlayWindow.FontBold == Utils.FontBold.Both ?
                FontWeights.Bold : FontWeights.Regular;
            TranslatedCaption.FontWeight = _setting.OverlayWindow.FontBold >= Utils.FontBold.TranslationOnly ?
                FontWeights.Bold : FontWeights.Regular;

            OriginalCaptionDecorator.StrokeThickness = _setting.OverlayWindow.FontStroke;
            TranslatedCaptionDecorator.StrokeThickness = _setting.OverlayWindow.FontStroke;

            OriginalCaption.Foreground = colorMap[_setting.OverlayWindow.FontColor];
            UpdateTranslationColor(colorMap[_setting.OverlayWindow.FontColor]);

            BorderBackground.Background = colorMap[_setting.OverlayWindow.BackgroundColor];
            BorderBackground.Opacity = _setting.OverlayWindow.Opacity;

            ApplyFontSize();
            ApplyBackgroundOpacity();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
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

        private void TranslatedChanged(object? sender, PropertyChangedEventArgs e)
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
            if (_setting.OverlayWindow.FontSize + StyleConsts.DELTA_FONT_SIZE < StyleConsts.MAX_FONT_SIZE)
            {
                _setting.OverlayWindow.FontSize += StyleConsts.DELTA_FONT_SIZE;
                ApplyFontSize();
            }
        }

        private void FontDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (_setting.OverlayWindow.FontSize - StyleConsts.DELTA_FONT_SIZE > StyleConsts.MIN_FONT_SIZE)
            {
                _setting.OverlayWindow.FontSize -= StyleConsts.DELTA_FONT_SIZE;
                ApplyFontSize();
            }
        }

        private void FontBold_Click(object sender, RoutedEventArgs e)
        {
            _setting.OverlayWindow.FontBold++;
            if (_setting.OverlayWindow.FontBold > Utils.FontBold.Both)
                _setting.OverlayWindow.FontBold = Utils.FontBold.None;

            switch (_setting.OverlayWindow.FontBold)
            {
                case Utils.FontBold.None:
                    OriginalCaption.FontWeight = FontWeights.Regular;
                    TranslatedCaption.FontWeight = FontWeights.Regular;
                    break;
                case Utils.FontBold.TranslationOnly:
                    OriginalCaption.FontWeight = FontWeights.Regular;
                    TranslatedCaption.FontWeight = FontWeights.Bold;
                    break;
                case Utils.FontBold.SubtitleOnly:
                    OriginalCaption.FontWeight = FontWeights.Bold;
                    TranslatedCaption.FontWeight = FontWeights.Regular;
                    break;
                case Utils.FontBold.Both:
                    OriginalCaption.FontWeight = FontWeights.Bold;
                    TranslatedCaption.FontWeight = FontWeights.Bold;
                    break;
            }
        }

        private void FontStrokeIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (_setting.OverlayWindow.FontStroke + StyleConsts.DELTA_STROKE > StyleConsts.MAX_STROKE)
                return;
            _setting.OverlayWindow.FontStroke += StyleConsts.DELTA_STROKE;
            ApplyFontStroke();
        }

        private void FontStrokeDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (_setting.OverlayWindow.FontStroke - StyleConsts.DELTA_STROKE < StyleConsts.MIN_STROKE)
                return;
            _setting.OverlayWindow.FontStroke -= StyleConsts.DELTA_STROKE;
            ApplyFontStroke();
        }

        private void FontColorCycle_Click(object sender, RoutedEventArgs e)
        {
            _setting.OverlayWindow.FontColor++;
            if (_setting.OverlayWindow.FontColor > ColorEnum.Black)
                _setting.OverlayWindow.FontColor = ColorEnum.White;

            OriginalCaption.Foreground = colorMap[_setting.OverlayWindow.FontColor];
            TranslatedCaption.Foreground = colorMap[_setting.OverlayWindow.FontColor];
            UpdateTranslationColor(colorMap[_setting.OverlayWindow.FontColor]);
        }

        private void BackgroundOpacityIncrease_Click(object sender, RoutedEventArgs e)
        {
            if (_setting.OverlayWindow.Opacity + StyleConsts.DELTA_OPACITY < StyleConsts.MAX_OPACITY)
                _setting.OverlayWindow.Opacity += StyleConsts.DELTA_OPACITY;
            else
                _setting.OverlayWindow.Opacity = StyleConsts.MAX_OPACITY;
            ApplyBackgroundOpacity();
        }

        private void BackgroundOpacityDecrease_Click(object sender, RoutedEventArgs e)
        {
            if (_setting.OverlayWindow.Opacity - StyleConsts.DELTA_OPACITY > StyleConsts.MIN_OPACITY)
                _setting.OverlayWindow.Opacity -= StyleConsts.DELTA_OPACITY;
            else
                _setting.OverlayWindow.Opacity = StyleConsts.MIN_OPACITY;
            ApplyBackgroundOpacity();
        }

        private void BackgroundColorCycle_Click(object sender, RoutedEventArgs e)
        {
            _setting.OverlayWindow.BackgroundColor++;
            if (_setting.OverlayWindow.BackgroundColor > ColorEnum.Black)
                _setting.OverlayWindow.BackgroundColor = ColorEnum.White;
            BorderBackground.Background = colorMap[_setting.OverlayWindow.BackgroundColor];

            BorderBackground.Opacity = _setting.OverlayWindow.Opacity;
            ApplyBackgroundOpacity();
        }

        private void OnlyModeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var symbolIcon = button?.Icon as SymbolIcon;

            if (onlyMode == CaptionVisible.SubtitleOnly)
            {
                // (0) Subtitle + Translation
                symbolIcon!.Symbol = SymbolRegular.PanelBottom20;
                OnlyMode = CaptionVisible.Both;
            }
            else if (onlyMode == CaptionVisible.Both)
            {
                // (1) Translation Only
                symbolIcon!.Symbol = SymbolRegular.PanelTopExpand20;
                OnlyMode = CaptionVisible.TranslationOnly;
            }
            else
            {
                // (2) Subtitle Only
                symbolIcon!.Symbol = SymbolRegular.PanelTopContract20;
                OnlyMode = CaptionVisible.SubtitleOnly;
            }
        }

        private void SwitchModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (SwitchMode == CaptionLocation.TranslationTop)
            {
                Grid.SetRow(TranslatedCaptionCard, 1);
                Grid.SetRow(OriginalCaptionCard, 0);
                SwitchMode = CaptionLocation.SubtitleTop;
            }
            else
            {
                Grid.SetRow(TranslatedCaptionCard, 0);
                Grid.SetRow(OriginalCaptionCard, 1);
                SwitchMode = CaptionLocation.TranslationTop;
            }
        }

        private void ClickThrough_Click(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var extendedStyle = WindowsAPI.GetWindowLong(hwnd, WindowsAPI.GWL_EXSTYLE);
            WindowsAPI.SetWindowLong(hwnd, WindowsAPI.GWL_EXSTYLE, extendedStyle | WindowsAPI.WS_EX_TRANSPARENT);
            ControlPanel.Visibility = Visibility.Collapsed;
        }

        public void ResizeForOnlyMode()
        {
            if (onlyMode == CaptionVisible.TranslationOnly)
            {
                // (1) Translation Only
                OriginalCaptionCard.Visibility = Visibility.Collapsed;
                this.MinHeight -= StyleConsts.DELTA_OVERLAY_HEIGHT;
                this.Height -= StyleConsts.DELTA_OVERLAY_HEIGHT;
                this.Top += StyleConsts.DELTA_OVERLAY_HEIGHT;
            }
            if (onlyMode == CaptionVisible.SubtitleOnly)
            {
                // restore
                OriginalCaptionCard.Visibility = Visibility.Visible;
                this.Top -= StyleConsts.DELTA_OVERLAY_HEIGHT;
                this.Height += StyleConsts.DELTA_OVERLAY_HEIGHT;
                this.MinHeight += StyleConsts.DELTA_OVERLAY_HEIGHT;

                // (2) Subtitle Only
                TranslatedCaptionCard.Visibility = Visibility.Collapsed;
                this.MinHeight -= StyleConsts.DELTA_OVERLAY_HEIGHT;
                this.Height -= StyleConsts.DELTA_OVERLAY_HEIGHT;
            }
            else if (onlyMode == CaptionVisible.Both)
            {
                // restore
                TranslatedCaptionCard.Visibility = Visibility.Visible;
                this.Height += StyleConsts.DELTA_OVERLAY_HEIGHT;
                this.MinHeight += StyleConsts.DELTA_OVERLAY_HEIGHT;
            }
        }

        public void ApplyFontSize()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                OriginalCaption.FontSize = _setting.OverlayWindow.FontSize;
                TranslatedCaption.FontSize = (int)(OriginalCaption.FontSize * 1.25);
            }), DispatcherPriority.Background);
        }

        public void ApplyFontStroke()
        {
            OriginalCaptionDecorator.StrokeThickness = _setting.OverlayWindow.FontStroke;
            TranslatedCaptionDecorator.StrokeThickness = _setting.OverlayWindow.FontStroke;
        }

        public void ApplyBackgroundOpacity()
        {
            Color color = ((SolidColorBrush)BorderBackground.Background).Color;
            BorderBackground.Background = new SolidColorBrush(Color.FromArgb(
                (byte)_setting.OverlayWindow.Opacity, color.R, color.G, color.B));
        }

        private void UpdateTranslationColor(SolidColorBrush brush)
        {
            var color = brush.Color;

            double target = 0.299 * color.R + 0.587 * color.G + 0.114 * color.B > 127 ? 0 : 255;
            byte r = (byte)Math.Clamp(color.R + (target - color.R) * 0.3, 0, 255);
            byte g = (byte)Math.Clamp(color.G + (target - color.G) * 0.4, 0, 255);
            byte b = (byte)Math.Clamp(color.B + (target - color.B) * 0.3, 0, 255);

            NoticePrefixRun.Foreground = brush;
            PreviousTranslationRun.Foreground = brush;
            CurrentTranslationRun.Foreground = new SolidColorBrush(Color.FromRgb(r, g, b));
        }
    }
}
