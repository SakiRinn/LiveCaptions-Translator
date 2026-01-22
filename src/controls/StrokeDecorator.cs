using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace LiveCaptionsTranslator
{
    public class StrokeDecorator : Decorator
    {
        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            nameof(Stroke),
            typeof(Brush),
            typeof(StrokeDecorator),
            new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            nameof(StrokeThickness),
            typeof(double),
            typeof(StrokeDecorator),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            if (visualRemoved is TextBlock oldTextBlock)
            {
                UnregisterTextBlockListeners(oldTextBlock);
                UnregisterRunListeners(oldTextBlock);
            }

            if (visualAdded is TextBlock newTextBlock)
            {
                RegisterTextBlockListeners(newTextBlock);
                RegisterRunListeners(newTextBlock);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (Child is not TextBlock textBlock)
            {
                base.OnRender(drawingContext);
                return;
            }

            var pen = new Pen(Stroke, StrokeThickness)
            {
                DashCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                StartLineCap = PenLineCap.Round
            };

            var typeface = new Typeface(
                textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch);
            if (textBlock.Inlines != null && textBlock.Inlines.Count > 0)
            {
                // Inlines, process each Run
                var formattedText = new FormattedText(
                    string.Concat(textBlock.Inlines.OfType<Run>().Select(r => r.Text ?? string.Empty)),
                    CultureInfo.CurrentUICulture,
                    textBlock.FlowDirection,
                    typeface,
                    textBlock.FontSize,
                    textBlock.Foreground,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                formattedText.TextAlignment = textBlock.TextAlignment;
                formattedText.Trimming = textBlock.TextTrimming;
                formattedText.MaxTextWidth = textBlock.ActualWidth > 0 ? textBlock.ActualWidth : double.MaxValue;
                formattedText.MaxTextHeight = textBlock.ActualHeight > 0 ? textBlock.ActualHeight : double.MaxValue;

                int pos = 0;
                foreach (var run in textBlock.Inlines.OfType<Run>())
                {
                    int len = (run.Text ?? string.Empty).Length;
                    if (len == 0)
                        continue;
                    formattedText.SetFontFamily(run.FontFamily ?? textBlock.FontFamily, pos, len);
                    formattedText.SetFontSize(GetValue(run, Run.FontSizeProperty, textBlock.FontSize), pos, len);
                    formattedText.SetFontWeight(GetValue(run, Run.FontWeightProperty, textBlock.FontWeight), pos, len);
                    formattedText.SetFontStyle(GetValue(run, Run.FontStyleProperty, textBlock.FontStyle), pos, len);
                    formattedText.SetFontStretch(GetValue(run, Run.FontStretchProperty, textBlock.FontStretch), pos,
                        len);
                    formattedText.SetForegroundBrush(run.Foreground ?? textBlock.Foreground, pos, len);
                    pos += len;
                }

                var geometry = formattedText.BuildGeometry(new Point(0, 0));
                drawingContext.DrawGeometry(null, pen, geometry);
                drawingContext.DrawText(formattedText, new Point(0, 0));
            }
            else
            {
                // No Inlines, process the TextBlock
                var formattedText = new FormattedText(
                    textBlock.Text ?? string.Empty,
                    CultureInfo.CurrentUICulture,
                    textBlock.FlowDirection,
                    typeface,
                    textBlock.FontSize,
                    textBlock.Foreground,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                formattedText.TextAlignment = textBlock.TextAlignment;
                formattedText.Trimming = textBlock.TextTrimming;
                formattedText.MaxTextWidth = textBlock.ActualWidth > 0 ? textBlock.ActualWidth : double.MaxValue;
                formattedText.MaxTextHeight = textBlock.ActualHeight > 0 ? textBlock.ActualHeight : double.MaxValue;

                if (textBlock.TextWrapping == TextWrapping.NoWrap)
                    formattedText.MaxLineCount = 1;

                var geometry = formattedText.BuildGeometry(new Point(0, 0));
                drawingContext.DrawGeometry(null, pen, geometry);
                drawingContext.DrawText(formattedText, new Point(0, 0));
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (Child != null)
            {
                Child.Measure(constraint);
                return Child.DesiredSize;
            }

            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            if (Child != null)
            {
                Child.Opacity = 0;
                Child.Arrange(new Rect(arrangeSize));
            }

            return arrangeSize;
        }

        private void RegisterTextBlockListeners(TextBlock textBlock)
        {
            RegisterPropertyListener(textBlock, TextBlock.TextProperty);
            RegisterPropertyListener(textBlock, TextBlock.FontSizeProperty);
            RegisterPropertyListener(textBlock, TextBlock.FontWeightProperty);
            RegisterPropertyListener(textBlock, TextBlock.FontStyleProperty);
            RegisterPropertyListener(textBlock, TextBlock.FontFamilyProperty);
            RegisterPropertyListener(textBlock, TextBlock.FontStretchProperty);
            RegisterPropertyListener(textBlock, TextBlock.ForegroundProperty);
            RegisterPropertyListener(textBlock, TextBlock.TextAlignmentProperty);
            RegisterPropertyListener(textBlock, TextBlock.TextTrimmingProperty);
            RegisterPropertyListener(textBlock, TextBlock.TextWrappingProperty);
        }

        private void UnregisterTextBlockListeners(TextBlock textBlock)
        {
            UnregisterPropertyListener(textBlock, TextBlock.TextProperty);
            UnregisterPropertyListener(textBlock, TextBlock.FontSizeProperty);
            UnregisterPropertyListener(textBlock, TextBlock.FontWeightProperty);
            UnregisterPropertyListener(textBlock, TextBlock.FontStyleProperty);
            UnregisterPropertyListener(textBlock, TextBlock.FontFamilyProperty);
            UnregisterPropertyListener(textBlock, TextBlock.FontStretchProperty);
            UnregisterPropertyListener(textBlock, TextBlock.ForegroundProperty);
            UnregisterPropertyListener(textBlock, TextBlock.TextAlignmentProperty);
            UnregisterPropertyListener(textBlock, TextBlock.TextTrimmingProperty);
            UnregisterPropertyListener(textBlock, TextBlock.TextWrappingProperty);
        }

        private void RegisterRunListeners(TextBlock textBlock)
        {
            if (textBlock.Inlines == null)
                return;
            foreach (var run in textBlock.Inlines.OfType<Run>())
            {
                RegisterPropertyListener(run, Run.ForegroundProperty);
                RegisterPropertyListener(run, Run.FontSizeProperty);
                RegisterPropertyListener(run, Run.FontWeightProperty);
                RegisterPropertyListener(run, Run.FontStyleProperty);
                RegisterPropertyListener(run, Run.FontFamilyProperty);
                RegisterPropertyListener(run, Run.FontStretchProperty);
                RegisterPropertyListener(run, Run.TextDecorationsProperty);
            }
        }

        private void UnregisterRunListeners(TextBlock textBlock)
        {
            if (textBlock.Inlines == null)
                return;
            foreach (var run in textBlock.Inlines.OfType<Run>())
            {
                UnregisterPropertyListener(run, Run.ForegroundProperty);
                UnregisterPropertyListener(run, Run.FontSizeProperty);
                UnregisterPropertyListener(run, Run.FontWeightProperty);
                UnregisterPropertyListener(run, Run.FontStyleProperty);
                UnregisterPropertyListener(run, Run.FontFamilyProperty);
                UnregisterPropertyListener(run, Run.FontStretchProperty);
                UnregisterPropertyListener(run, Run.TextDecorationsProperty);
            }
        }

        private void RegisterPropertyListener(DependencyObject obj, DependencyProperty property)
        {
            var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                property, obj.GetType());
            descriptor?.AddValueChanged(obj, OnTextBlockPropertyChanged);
        }

        private void UnregisterPropertyListener(DependencyObject obj, DependencyProperty property)
        {
            var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                property, obj.GetType());
            descriptor?.RemoveValueChanged(obj, OnTextBlockPropertyChanged);
        }

        private void OnTextBlockPropertyChanged(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        private static T GetValue<T>(DependencyObject obj, DependencyProperty property, T fallback)
        {
            return obj.ReadLocalValue(property) != DependencyProperty.UnsetValue ?
                (T)obj.GetValue(property) : fallback;
        }
    }
}