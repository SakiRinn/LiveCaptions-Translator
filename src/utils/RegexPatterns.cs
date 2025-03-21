using System.Text.RegularExpressions;

namespace LiveCaptionsTranslator.utils
{
    public static partial class RegexPatterns
    {
        // Remove the `.` between two uppercase letters. (Cope with acronym)
        [GeneratedRegex(@"([A-Z])\s*\.\s*([A-Z])(?![A-Za-z]+)")]
        public static partial Regex Acronym();

        // If an acronym is followed by a word, preserve the space between them.
        [GeneratedRegex(@"([A-Z])\s*\.\s*([A-Z])(?=[A-Za-z]+)")]
        public static partial Regex AcronymWithWords();

        // Remove redundant spaces and `\n` around punctuation.
        [GeneratedRegex(@"\s*([.!?,])\s*")]
        public static partial Regex PunctuationSpace();

        // If it is Chinese or Japanese punctuation, no need to insert spaces.
        [GeneratedRegex(@"\s*([。！？，、])\s*")]
        public static partial Regex CJPunctuationSpace();
        
        [GeneratedRegex(@"^(\[.+\] )?(.*)$")]
        public static partial Regex NoticePrefixAndTranslation();
        
        [GeneratedRegex(@"^\[.+\] ")]
        public static partial Regex NoticePrefix();
        
        [GeneratedRegex(@"^(https?:\/\/)")]
        public static partial Regex HttpPrefix();
        
        [GeneratedRegex(@"\/{2,}")]
        public static partial Regex MultipleSlashes();
    }
}
