namespace LiveCaptionsTranslator.models
{
    public class TranslationHistoryEntry
    {
        public string Timestamp { get; set; }
        public string TimestampFull { get; set; }
        public string SourceText { get; set; }
        public string TranslatedText { get; set; }
        public string TargetLanguage { get; set; }
        public string ApiUsed { get; set; }
    }
}
