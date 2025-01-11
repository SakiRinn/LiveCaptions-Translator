using System.Windows.Automation;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using LiveCaptionsTranslator.controllers;

namespace LiveCaptionsTranslator.models
{
    public class Caption : INotifyPropertyChanged
    {
        private static Caption? instance = null;
        public event PropertyChangedEventHandler? PropertyChanged;

        private static readonly char[] PUNC_EOS = ".?!。？！".ToCharArray();
        private static readonly char[] PUNC_COMMA = ",，、—\n".ToCharArray();
        private static readonly char[] PUNC_ALL = ".?!。？！,，、—\n".ToCharArray();
        
        private readonly object syncLock = new object();
        private string original = "";
        private string translated = "";
        private string pendingOriginal = "";
        private string lastTranslatedOriginal = "";
        private bool isProcessingTranslation = false;
        private int idleCount = 0;
        private int syncCount = 0;

        // 用于存储最近的句子历史
        private Queue<string> recentSentences = new Queue<string>();
        private const int MAX_RECENT_SENTENCES = 5;
        private const double SIMILARITY_THRESHOLD = 0.8;

        private const int MAX_IDLE_INTERVAL = 10;
        private const int MAX_SYNC_INTERVAL = 5;
        private const int MAX_SENTENCE_LENGTH = 100;
        private const int MIN_SENTENCE_LENGTH = 15;
        private const int MIN_CHAR_LENGTH = 10;
        private const int TRANSLATION_DELAY = 100;
        private DateTime lastUpdateTime = DateTime.Now;

        public bool PauseFlag { get; set; } = false;
        public bool TranslateFlag { get; set; } = false;
        private bool EOSFlag { get; set; } = false;

        public string Original
        {
            get => original;
            set
            {
                bool isLongEnough = !string.IsNullOrEmpty(value) && value.Length >= MIN_CHAR_LENGTH;

                if (!string.IsNullOrEmpty(value) && 
                    (EOSFlag || value.Length > 100 || idleCount >= MAX_IDLE_INTERVAL) &&
                    isLongEnough)
                {
                    lock (syncLock)
                    {
                        original = value;
                        pendingOriginal = "";
                        lastUpdateTime = DateTime.Now;
                        OnPropertyChanged("Original");
                        
                        if (!PauseFlag && value != lastTranslatedOriginal)
                        {
                            TranslateFlag = true;
                        }
                    }
                }
                else
                {
                    pendingOriginal = value;
                }
            }
        }
        public string Translated
        {
            get => translated;
            set
            {
                lock (syncLock)
                {
                    translated = value;
                    OnPropertyChanged("Translated");
                    isProcessingTranslation = false;
                }
            }
        }

        private Caption() { }

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

        private string FormatText(string text)
        {
            text = text.Replace("\n", "，");
            foreach (char eos in PUNC_EOS)
            {
                text = text.Replace($"{eos} ", $"{eos}");
                text = text.Replace($" {eos}", $"{eos}");
            }
            
            if (!string.IsNullOrEmpty(text) && Array.IndexOf(PUNC_ALL, text[^1]) == -1)
            {
                text += "...";
            }
            
            return text;
        }

        private bool IsSentenceComplete(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            
            bool hasEndPunctuation = Array.IndexOf(PUNC_EOS, text[^1]) != -1;
            bool hasMinLength = text.Length >= MIN_SENTENCE_LENGTH;
            bool hasValidStructure = text.Contains(" ") && !text.EndsWith(" ");
            
            return hasEndPunctuation && hasMinLength && hasValidStructure;
        }

        public async Task Sync()
        {
            idleCount = 0;
            syncCount = 0;

            while (true)
            {
                try
                {
                    if (PauseFlag || App.Window == null)
                    {
                        pendingOriginal = "";
                        Original = "";
                        Translated = "";
                        TranslateFlag = false;
                        isProcessingTranslation = false;
                        idleCount = 0;
                        syncCount = 0;
                        EOSFlag = false;
                        lastTranslatedOriginal = "";
                        await Task.Delay(1000);
                        continue;
                    }

                    string fullText = (await GetCaptions(App.Window)).Trim();

                    if (string.IsNullOrEmpty(fullText))
                    {
                        await Task.Delay(50);
                        continue;
                    }

                    fullText = FormatText(fullText);

                    if (fullText.Length > MAX_SENTENCE_LENGTH && !fullText.EndsWith("..."))
                    {
                        int splitIndex = fullText.LastIndexOfAny(PUNC_ALL, Math.Min(fullText.Length - 1, MAX_SENTENCE_LENGTH));
                        if (splitIndex > MIN_SENTENCE_LENGTH)
                        {
                            fullText = fullText.Substring(0, splitIndex + 1);
                        }
                    }

                    int lastEOSIndex;
                    if (Array.IndexOf(PUNC_EOS, fullText[^1]) != -1)
                        lastEOSIndex = fullText[0..^1].LastIndexOfAny(PUNC_EOS);
                    else
                        lastEOSIndex = fullText.LastIndexOfAny(PUNC_EOS);
                    string latestCaption = fullText.Substring(lastEOSIndex + 1);

                    while (lastEOSIndex > 0 && (
                        Encoding.UTF8.GetByteCount(latestCaption) < MIN_CHAR_LENGTH * 3 ||
                        !ContainsValidSentence(latestCaption)))
                    {
                        lastEOSIndex = fullText[0..lastEOSIndex].LastIndexOfAny(PUNC_EOS);
                        latestCaption = fullText.Substring(lastEOSIndex + 1);
                    }

                    while (Encoding.UTF8.GetByteCount(latestCaption) > 150)
                    {
                        int commaIndex = latestCaption.IndexOfAny(PUNC_COMMA);
                        if (commaIndex < 0 || commaIndex + 1 == latestCaption.Length)
                            break;
                        latestCaption = latestCaption.Substring(commaIndex + 1);
                    }
                    latestCaption = latestCaption.Replace("\n", "——");

                    if (pendingOriginal.CompareTo(latestCaption) != 0)
                    {
                        idleCount = 0;
                        syncCount++;
                        pendingOriginal = latestCaption;

                        if (IsSentenceComplete(latestCaption))
                        {
                            syncCount = 0;
                            Original = pendingOriginal;
                            EOSFlag = true;
                        }
                        else
                        {
                            EOSFlag = false;
                            if (latestCaption.Length >= MIN_SENTENCE_LENGTH * 2)
                            {
                                Original = pendingOriginal;
                            }
                        }
                    }
                    else
                        idleCount++;

                    if (syncCount > MAX_SYNC_INTERVAL || 
                        idleCount == MAX_IDLE_INTERVAL)
                    {
                        syncCount = 0;
                        Original = pendingOriginal;
                    }
                    await Task.Delay(50);
                }
                catch (Exception)
                {
                    await Task.Delay(1000);
                    continue;
                }
            }
        }

        public async Task Translate()
        {
            var controller = new TranslationController();

            while (true)
            {
                try
                {
                    if (PauseFlag || App.Window == null)
                    {
                        lock (syncLock)
                        {
                            TranslateFlag = false;
                            isProcessingTranslation = false;
                            lastTranslatedOriginal = "";
                        }

                        for (int pauseCount = 0; PauseFlag || App.Window == null; pauseCount++)
                        {
                            await Task.Delay(50);
                        }
                        continue;
                    }

                    if (TranslateFlag && !isProcessingTranslation)
                    {
                        isProcessingTranslation = true;
                        string textToTranslate;
                        
                        lock (syncLock)
                        {
                            textToTranslate = Original;
                        }

                        if (!string.IsNullOrEmpty(textToTranslate) && !PauseFlag)
                        {
                            // 检查是否需要记录到历史
                            bool shouldLog = false;
                            if (textToTranslate.IndexOfAny(PUNC_EOS) >= 0)
                            {
                                // 检查与最近句子的相似度
                                var longestSimilar = FindLongestSimilarSentence(textToTranslate);
                                if (longestSimilar == null || textToTranslate.Length > longestSimilar.Length)
                                {
                                    shouldLog = true;
                                    UpdateRecentSentences(textToTranslate);
                                }
                            }

                            string translatedText = await controller.TranslateAsync(textToTranslate);
                            if (shouldLog)
                            {
                                await controller.LogTranslationAsync(textToTranslate, translatedText);
                            }
                            
                            lock (syncLock)
                            {
                                if (textToTranslate == Original)
                                {
                                    Translated = translatedText;
                                    lastTranslatedOriginal = textToTranslate;
                                }
                            }
                        }

                        TranslateFlag = false;
                        isProcessingTranslation = false;
                    }

                    await Task.Delay(50);
                }
                catch (Exception)
                {
                    isProcessingTranslation = false;
                    TranslateFlag = false;
                    await Task.Delay(1000);
                    continue;
                }
            }
        }

        public static async Task<string> GetCaptions(AutomationElement window)
        {
            var captionsTextBlock = await Task.Run(() => LiveCaptionsHandler.FindElementByAId(window, "CaptionsTextBlock"));
            if (captionsTextBlock == null)
                return string.Empty;
            return captionsTextBlock.Current.Name;
        }

        private bool ContainsValidSentence(string text)
        {
            if (text.Length < MIN_CHAR_LENGTH) return false;
            
            return text.IndexOfAny(PUNC_EOS) >= 0 || text.IndexOfAny(PUNC_COMMA) >= 0;
        }

        // 计算两个字符串的相似度
        private double CalculateSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0;
            
            // 使用较短的字符串作为基准
            if (s1.Length > s2.Length)
            {
                var temp = s1;
                s1 = s2;
                s2 = temp;
            }

            // 检查s2是否包含s1的开头部分
            return s2.StartsWith(s1) ? (double)s1.Length / s2.Length : 0;
        }

        // 查找最近句子中最长的相似句子
        private string FindLongestSimilarSentence(string current)
        {
            string longest = null;
            foreach (var sentence in recentSentences)
            {
                if (CalculateSimilarity(sentence, current) > SIMILARITY_THRESHOLD)
                {
                    if (longest == null || sentence.Length > longest.Length)
                    {
                        longest = sentence;
                    }
                }
            }
            return longest;
        }

        // 更新最近句子队列
        private void UpdateRecentSentences(string sentence)
        {
            recentSentences.Enqueue(sentence);
            if (recentSentences.Count > MAX_RECENT_SENTENCES)
            {
                recentSentences.Dequeue();
            }
        }
    }
}
