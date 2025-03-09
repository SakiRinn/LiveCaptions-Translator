using System.Windows.Media;
using System.Windows;

namespace LiveCaptionsTranslator.models
{
    public class TranslationHistoryEntry
    {
        public required string Timestamp { get; set; }
        public required string TimestampFull { get; set; }
        public required string SourceText { get; set; }
        public required string TranslatedText { get; set; }
        public required string TargetLanguage { get; set; }
        public required string ApiUsed { get; set; }
        public double FontSizeOriginal { get; set; }
        public double FontSizeTranslated { get; set; }
        public double FontShadowOriginal { get; set; }
        public double FontShadowTranslated { get; set; }
        public Brush FontColor { get; set; }
        public FontWeight FontWeightOriginal { get; set; }
        public FontWeight FontWeightTranslated { get; set; }
        public Visibility TranslationOnly { get; set; }
    }
}
