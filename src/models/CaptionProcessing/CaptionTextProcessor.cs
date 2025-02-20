using System;
using System.Text;

namespace LiveCaptionsTranslator.models.CaptionProcessing
{
    public static class CaptionTextProcessor
    {
        public static readonly char[] PUNC_EOS = ".?!。？！".ToCharArray();
        public static readonly char[] PUNC_COMMA = ",，、—\n".ToCharArray();

        public static string ProcessFullText(string fullText)
        {
            foreach (char eos in PUNC_EOS)
                fullText = fullText.Replace($"{eos}\n", $"{eos}");
            return fullText;
        }

        public static int GetLastEOSIndex(string fullText)
        {
            if (string.IsNullOrEmpty(fullText)) return -1;
            
            return Array.IndexOf(PUNC_EOS, fullText[^1]) != -1
                ? fullText[0..^1].LastIndexOfAny(PUNC_EOS)
                : fullText.LastIndexOfAny(PUNC_EOS);
        }

        public static string ExtractLatestCaption(string fullText, int lastEOSIndex)
        {
            if (lastEOSIndex < -1) return fullText;
            
            string latestCaption = fullText.Substring(lastEOSIndex + 1);

            // Ensure appropriate caption length
            while (lastEOSIndex > 0 && Encoding.UTF8.GetByteCount(latestCaption) < 15)
            {
                lastEOSIndex = fullText[0..lastEOSIndex].LastIndexOfAny(PUNC_EOS);
                latestCaption = fullText.Substring(lastEOSIndex + 1);
            }

            while (Encoding.UTF8.GetByteCount(latestCaption) > 170)
            {
                int commaIndex = latestCaption.IndexOfAny(PUNC_COMMA);
                if (commaIndex < 0 || commaIndex + 1 == latestCaption.Length)
                    break;
                latestCaption = latestCaption.Substring(commaIndex + 1);
            }

            return latestCaption;
        }

        public static bool ShouldTriggerTranslation(string caption, ref int syncCount, int maxSyncInterval)
        {
            bool shouldTranslate = false;

            if (Array.IndexOf(PUNC_EOS, caption[^1]) != -1 ||
                Array.IndexOf(PUNC_COMMA, caption[^1]) != -1)
            {
                syncCount = 0;
                shouldTranslate = true;
            }
            else if (syncCount > maxSyncInterval)
            {
                syncCount = 0;
                shouldTranslate = true;
            }

            return shouldTranslate;
        }
    }
}
