namespace LiveCaptionsTranslator.Utils
{
    public enum FontBold
    {
        None = 0,
        TranslationOnly = 1,
        SubtitleOnly = 2,
        Both = 3
    }

    public enum CaptionVisible
    {
        Both = 0,
        TranslationOnly = 1,
        SubtitleOnly = 2
    }
    
    public enum CaptionLocation
    {
        TranslationTop = 0,
        SubtitleTop = 1
    }
    
    public enum Color
    {
        White = 1,
        Yellow = 2,
        LimeGreen = 3,
        Aqua = 4,
        Blue = 5,
        DeepPink = 6,
        Red = 7,
        Black = 8
    }

    public static class StyleConsts
    {
        public const int MAX_FONT_SIZE = 40;
        public const int MIN_FONT_SIZE = 8;
        public const int DELTA_FONT_SIZE = 1;
        
        public const int MAX_OPACITY = 251;
        public const int MIN_OPACITY = 1;
        public const int DELTA_OPACITY = 25;

        public const double MAX_STROKE = 7.5;
        public const double MIN_STROKE = 0.0;
        public const double DELTA_STROKE = 1.5;
        
        public const int DELTA_OVERLAY_HEIGHT = 40;
    }
}
