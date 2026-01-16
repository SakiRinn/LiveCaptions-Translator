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

            // Cancel old TextBlock listening
            if (visualRemoved is TextBlock oldTextBlock)
            {
                var textProperty = TextBlock.TextProperty;
                var binding = System.Windows.Data.BindingOperations.GetBindingExpression(oldTextBlock, textProperty);
                if (binding != null)
                    oldTextBlock.TargetUpdated -= OnTextBlockPropertyChanged;
            }

            // New TextBlock listening
            if (visualAdded is TextBlock newTextBlock)
            {
                // Text
                var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                    TextBlock.TextProperty, typeof(TextBlock));
                descriptor?.AddValueChanged(newTextBlock, OnTextBlockPropertyChanged);
                // Font Size
                descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                    TextBlock.FontSizeProperty, typeof(TextBlock));
                descriptor?.AddValueChanged(newTextBlock, OnTextBlockPropertyChanged);
                // Font Weight (Font Bold)
                descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                    TextBlock.FontWeightProperty, typeof(TextBlock));
                descriptor?.AddValueChanged(newTextBlock, OnTextBlockPropertyChanged);
                // Foreground (Font Color)
                descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                    TextBlock.ForegroundProperty, typeof(TextBlock));
                descriptor?.AddValueChanged(newTextBlock, OnTextBlockPropertyChanged);
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
                    formattedText.SetFontStretch(GetValue(run, Run.FontStretchProperty, textBlock.FontStretch), pos, len);
                    formattedText.SetForegroundBrush(run.Foreground ?? textBlock.Foreground, pos, len);
                    pos += len;
                }

                var geometry = formattedText.BuildGeometry(new Point(0, 0));
                drawingContext.DrawGeometry(null, pen, geometry);
                drawingContext.DrawGeometry(textBlock.Foreground, null, geometry);
            }
            else
            {
                // No Inlines, process Text
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
                drawingContext.DrawGeometry(textBlock.Foreground, null, geometry);
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

        private void OnTextBlockPropertyChanged(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        private static T GetValue<T>(DependencyObject obj, DependencyProperty property, T fallback)
        {
            return obj.ReadLocalValue(property) != DependencyProperty.UnsetValue
                ? (T)obj.GetValue(property)
                : fallback;
        }
    }
}
