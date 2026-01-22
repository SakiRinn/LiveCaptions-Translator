using System.Collections.ObjectModel;
using System.Globalization;

namespace LiveCaptionsTranslator.models
{
    public class LanguageItem
    {
        public string DisplayName { get; set; }
        public string CultureCode { get; set; }
    }

    public static class LanguageCatalog
    {

        public static readonly ReadOnlyCollection<LanguageItem> SupportedUiLanguages = new(new List<LanguageItem>
        {
            new() { DisplayName = "English", CultureCode = "en-us" },
            new() { DisplayName = "\u7B80\u4F53\u4E2D\u6587", CultureCode = "zh-cn" },
            new() { DisplayName = "\u0627\u0644\u0639\u0631\u0628\u064A\u0629", CultureCode = "ar" },
            new() { DisplayName = "\u09AC\u09BE\u0982\u09B2\u09BE", CultureCode = "bn" },
            new() { DisplayName = "\u010Ce\u0161tina", CultureCode = "cs-cz" },
            new() { DisplayName = "Deutsch", CultureCode = "de-de" },
            new() { DisplayName = "Espa\u00F1ol (M\u00E9xico)", CultureCode = "es-mx" },
            new() { DisplayName = "Fran\u00E7ais", CultureCode = "fr-fr" },
            new() { DisplayName = "Italiano", CultureCode = "it-it" },
            new() { DisplayName = "\u65E5\u672C\u8A9E", CultureCode = "ja-jp" },
            new() { DisplayName = "\uD55C\uAD6D\uC5B4", CultureCode = "ko-kr" },
            new() { DisplayName = "Lietuvi\u0173", CultureCode = "lt-lt" },
            new() { DisplayName = "Nederlands", CultureCode = "nl-nl" },
            new() { DisplayName = "Polski", CultureCode = "pl-pl" },
            new() { DisplayName = "Portugu\u00EAs (Brasil)", CultureCode = "pt-br" },
            new() { DisplayName = "Portugu\u00EAs (Portugal)", CultureCode = "pt-pt" },
            new() { DisplayName = "\u0420\u0443\u0441\u0441\u043A\u0438\u0439", CultureCode = "ru-ru" },
            new() { DisplayName = "Svenska", CultureCode = "sv-se" },
            new() { DisplayName = "T\u00FCrk\u00E7e", CultureCode = "tr-tr" },
            new() { DisplayName = "Ti\u1EBFng Vi\u1EC7t", CultureCode = "vi-vn" },
            new() { DisplayName = "\u7E41\u9AD4\u4E2D\u6587 (\u53F0\u7063)", CultureCode = "zh-tw" },
        });
    }
}
