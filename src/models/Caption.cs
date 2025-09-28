using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator.models
{
    public class Caption : INotifyPropertyChanged
    {
        private static Caption? instance = null;
        public event PropertyChangedEventHandler? PropertyChanged;

        private string displayOriginalCaption = "";
        private string displayTranslatedCaption = "";
        private string overlayOriginalCaption = "";
        private string overlayTranslatedCaption = "";

        public string OriginalCaption { get; set; } = "";
        public string TranslatedCaption { get; set; } = "";
        public string DisplayOriginalCaption
        {
            get => displayOriginalCaption;
            set
            {
                displayOriginalCaption = value;
                OnPropertyChanged("DisplayOriginalCaption");
            }
        }
        public string DisplayTranslatedCaption
        {
            get => displayTranslatedCaption;
            set
            {
                displayTranslatedCaption = value;
                OnPropertyChanged("DisplayTranslatedCaption");
            }
        }
        public string OverlayOriginalCaption
        {
            get => overlayOriginalCaption;
            set
            {
                overlayOriginalCaption = value;
                OnPropertyChanged("OverlayOriginalCaption");
            }
        }
        public string OverlayTranslatedCaption
        {
            get => overlayTranslatedCaption;
            set
            {
                overlayTranslatedCaption = value;
                OnPropertyChanged("OverlayTranslatedCaption");
            }
        }

        public Queue<TranslationHistoryEntry> Contexts { get; } = new(6);
        public IEnumerable<TranslationHistoryEntry> DisplayContexts => Contexts.Reverse();

        public string ContextPreviousCaption => GetPreviousCaption(
            Math.Min(Translator.Setting.MainWindow.CaptionLogMax, Contexts.Count));
        public string OverlayPreviousTranslation => GetPreviousTranslation(
            Math.Min(Translator.Setting.OverlayWindow.HistoryMax, Contexts.Count));

        private Caption()
        {
        }

        public static Caption GetInstance()
        {
            if (instance != null)
                return instance;
            instance = new Caption();
            return instance;
        }

        public string GetPreviousCaption(int count)
        {
            if (count <= 0)
                return string.Empty;

            var prefix = DisplayContexts
                .Take(count)
                .Reverse()
                .Select(entry => entry.SourceText)
                .Aggregate((accu, cur) =>
                {
                    if (!string.IsNullOrEmpty(accu) && Array.IndexOf(TextUtil.PUNC_EOS, accu[^1]) == -1)
                        accu += TextUtil.isCJChar(accu[^1]) ? "。" : ". ";
                    return accu + cur;
                });

            if (!string.IsNullOrEmpty(prefix) && Array.IndexOf(TextUtil.PUNC_EOS, prefix[^1]) == -1)
                prefix += TextUtil.isCJChar(prefix[^1]) ? "。" : ".";
            if (!string.IsNullOrEmpty(prefix) && Encoding.UTF8.GetByteCount(prefix[^1].ToString()) < 2)
                prefix += " ";
            return prefix;
        }

        public string GetPreviousTranslation(int count)
        {
            if (count <= 0)
                return string.Empty;

            var prefix = DisplayContexts
                .Take(count)
                .Reverse()
                .Select(entry =>
                    entry == null || entry.TranslatedText.Contains("[ERROR]") || entry.TranslatedText.Contains("[WARNING]")
                        ? "" : entry.TranslatedText)
                .Aggregate((accu, cur) =>
                {
                    if (!string.IsNullOrEmpty(accu) && Array.IndexOf(TextUtil.PUNC_EOS, accu[^1]) == -1)
                        accu += TextUtil.isCJChar(accu[^1]) ? "。" : ". ";
                    cur = RegexPatterns.NoticePrefix().Replace(cur, "");
                    return accu + cur;
                });
            prefix = RegexPatterns.NoticePrefix().Replace(prefix, "");

            if (!string.IsNullOrEmpty(prefix) && Array.IndexOf(TextUtil.PUNC_EOS, prefix[^1]) == -1)
                prefix += TextUtil.isCJChar(prefix[^1]) ? "。" : ".";
            if (!string.IsNullOrEmpty(prefix) && Encoding.UTF8.GetByteCount(prefix[^1].ToString()) < 2)
                prefix += " ";
            return prefix;
        }

        public void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}