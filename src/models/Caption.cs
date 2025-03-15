using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
        
        public Queue<TranslationHistoryEntry> LogCards { get; } = new(6);
        public IEnumerable<TranslationHistoryEntry> DisplayLogCards => LogCards.Reverse();

        public string OverlayTranslatedPrefix
        {
            get
            {
                int historyCount = Math.Min(Translator.Setting.OverlayWindow.HistoryMax, LogCards.Count);
                if (historyCount <= 0)
                    return string.Empty;
                var prefix = DisplayLogCards.Take(historyCount)
                                            .Reverse()
                                            .Select(entry => entry.TranslatedText)
                                            .Aggregate((accu, cur) =>
                        {
                            accu = Regex.Replace(accu, @"^\[\d+ ms\] ", "");
                            if (Array.IndexOf(TextUtil.PUNC_EOS, accu[^1]) == -1)
                                accu += TextUtil.isCJChar(accu[^1]) ? "。" : ". ";
                            cur = Regex.Replace(cur, @"^\[\d+ ms\] ", "");
                            return accu + cur;
                        });
                if (Array.IndexOf(TextUtil.PUNC_EOS, prefix[^1]) == -1)
                    prefix += TextUtil.isCJChar(prefix[^1]) ? "。" : ". ";
                return prefix;
            }
        }

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

        public void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}