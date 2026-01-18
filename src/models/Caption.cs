using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator.models
{
    public class Caption : INotifyPropertyChanged
    {
        public const int TAKE_MARGIN = 4;
        
        private static Caption? instance = null;
        public event PropertyChangedEventHandler? PropertyChanged;

        private string displayOriginalCaption = string.Empty;
        private string displayTranslatedCaption = string.Empty;
        private string overlayOriginalCaption = " ";
        private string overlayCurrentTranslation = " ";
        private string overlayNoticePrefix = " ";

        public string OriginalCaption { get; set; } = string.Empty;
        public string TranslatedCaption { get; set; } = string.Empty;

        public Queue<TranslationHistoryEntry> Contexts { get; } = new(6);
        public IEnumerable<TranslationHistoryEntry> DisplayContexts => Contexts.Reverse();
        
        public string ContextPreviousCaption => GetPreviousCaption(
            Math.Min(Translator.Setting.MainWindow.CaptionLogMax, Contexts.Count));
        
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
        public string OverlayNoticePrefix
        {
            get => overlayNoticePrefix;
            set
            {
                overlayNoticePrefix = value;
                OnPropertyChanged("OverlayNoticePrefix");
            }
        }
        public string OverlayCurrentTranslation
        {
            get => overlayCurrentTranslation;
            set
            {
                overlayCurrentTranslation = value;
                OnPropertyChanged("OverlayCurrentTranslation");
            }
        }
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
            var entries = FilterDedupEntries(DisplayContexts.Take(count + TAKE_MARGIN).Reverse());
            entries = entries[..Math.Min(count, entries.Count)];

            var prev = entries
                .Select(entry => entry.SourceText)
                .Aggregate((accu, cur) =>
                {
                    if (!string.IsNullOrEmpty(accu))
                    {
                        if (Array.IndexOf(TextUtil.PUNC_EOS, accu[^1]) == -1)
                            accu += TextUtil.isCJChar(accu[^1]) ? "。" : ". ";
                        else
                            accu += TextUtil.isCJChar(accu[^1]) ? "" : " ";
                    }
                    cur = RegexPatterns.NoticePrefix().Replace(cur, "");
                    return accu + cur;
                });

            if (!string.IsNullOrEmpty(prev) && Array.IndexOf(TextUtil.PUNC_EOS, prev[^1]) == -1)
                prev += TextUtil.isCJChar(prev[^1]) ? "。" : ".";
            if (!string.IsNullOrEmpty(prev) && Encoding.UTF8.GetByteCount(prev[^1].ToString()) < 2)
                prev += " ";
            return prev;
        }

        public string GetPreviousTranslation(int count)
        {
            if (count <= 0)
                return string.Empty;
            var entries = FilterDedupEntries(DisplayContexts.Take(count + TAKE_MARGIN).Reverse());
            entries = entries[..Math.Min(count, entries.Count)];

            var prev = entries
                .Select(entry => entry.TranslatedText.Contains("[ERROR]") || entry.TranslatedText.Contains("[WARNING]") ?
                    "" : entry.TranslatedText)
                .Aggregate((accu, cur) =>
                {
                    if (!string.IsNullOrEmpty(accu))
                    {
                        if (Array.IndexOf(TextUtil.PUNC_EOS, accu[^1]) == -1)
                            accu += TextUtil.isCJChar(accu[^1]) ? "。" : ". ";
                        else
                            accu += TextUtil.isCJChar(accu[^1]) ? "" : " ";
                    }
                    cur = RegexPatterns.NoticePrefix().Replace(cur, "");
                    return accu + cur;
                });
            
            prev = RegexPatterns.NoticePrefix().Replace(prev, "");
            if (!string.IsNullOrEmpty(prev) && Array.IndexOf(TextUtil.PUNC_EOS, prev[^1]) == -1)
                prev += TextUtil.isCJChar(prev[^1]) ? "。" : ".";
            if (!string.IsNullOrEmpty(prev) && Encoding.UTF8.GetByteCount(prev[^1].ToString()) < 2)
                prev += " ";
            return prev;
        }

        public void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private List<TranslationHistoryEntry> FilterDedupEntries(IEnumerable<TranslationHistoryEntry> entries)
        {
            var entryList = entries.ToList();
            var filtered = new List<TranslationHistoryEntry>();

            int i = 0;
            while (i < entryList.Count)
            {
                var longest = entryList[i];
                int j = i + 1;

                while (j < entryList.Count)
                {
                    if (entryList[j].SourceText.StartsWith(longest.SourceText))
                    {
                        longest = entryList[j];
                        j++;
                    }
                    else if (longest.SourceText.StartsWith(entryList[j].SourceText))
                        j++;
                    else
                        break;
                }

                filtered.Add(longest);
                i = j;
            }

            return filtered;
        }
    }
}