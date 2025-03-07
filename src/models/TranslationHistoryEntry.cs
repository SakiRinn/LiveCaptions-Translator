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
    }
}
