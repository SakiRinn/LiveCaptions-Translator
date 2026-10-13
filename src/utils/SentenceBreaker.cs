namespace LiveCaptionsTranslator.utils
{
    /// <summary>
    /// Ends the current sentence by hand.
    /// LiveCaptions can be slow to add a full stop when the speaker changes or a new sentence begins,
    /// so the words of both get translated together. A requested break is remembered by the text just
    /// before it, and a sentence end is inserted after that text on every later read, so the rest of
    /// the pipeline sees an ordinary end of sentence.
    /// </summary>
    public static class SentenceBreaker
    {
        // Characters before a break that locate it in later reads. LiveCaptions scrolls old text away
        // and may still rewrite its latest words; a break whose text is gone is dropped.
        private const int ANCHOR_LENGTH = 12;
        private const int MIN_ANCHOR_LENGTH = 4;
        private const int MAX_BREAKS = 8;

        // LiveCaptions lags behind the audio, so at the moment of the request it may not have written
        // the last words that were heard. The break is placed once its text pauses, or after a limit
        // if the next sentence follows without a pause.
        private const int QUIET_MS = 300;
        private const int MAX_WAIT_MS = 1500;

        internal static Func<long> Clock = () => Environment.TickCount64;

        private static readonly object breakLock = new object();
        private static readonly List<string> anchors = new List<string>();
        private static long requestedAt = -1;
        private static string lastText = string.Empty;
        private static long lastChange = 0;

        /// <summary>
        /// Requests a sentence end at the end of what is being said. Safe to call from the UI thread.
        /// </summary>
        public static void RequestBreak()
        {
            lock (breakLock)
            {
                if (requestedAt < 0)
                    requestedAt = Clock();
            }
        }

        /// <summary>
        /// Returns the captions with a sentence end after every remembered break that is still visible.
        /// Called by the sync loop with the preprocessed captions.
        /// </summary>
        public static string Apply(string text)
        {
            lock (breakLock)
            {
                long now = Clock();
                if (!string.Equals(text, lastText, StringComparison.Ordinal))
                {
                    lastText = text;
                    lastChange = now;
                }
                if (requestedAt >= 0 && (now - lastChange >= QUIET_MS || now - requestedAt >= MAX_WAIT_MS))
                {
                    requestedAt = -1;
                    AddAnchor(text);
                }
                if (anchors.Count == 0)
                    return text;

                var positions = new SortedSet<int>();
                for (int i = anchors.Count - 1; i >= 0; i--)
                {
                    int index = text.LastIndexOf(anchors[i], StringComparison.Ordinal);
                    if (index < 0)
                    {
                        anchors.RemoveAt(i);
                        continue;
                    }

                    // Nothing to do if LiveCaptions has ended the sentence there by itself.
                    int end = index + anchors[i].Length;
                    int next = end;
                    while (next < text.Length && char.IsWhiteSpace(text[next]))
                        next++;
                    if (next < text.Length && Array.IndexOf(TextUtil.PUNC_EOS, text[next]) != -1)
                        continue;
                    positions.Add(end);
                }

                foreach (int position in positions.Reverse())
                    text = text.Insert(position, BreakMark(text[position - 1]));
                return text;
            }
        }

        private static void AddAnchor(string text)
        {
            string trimmed = text.TrimEnd();
            if (trimmed.Length < MIN_ANCHOR_LENGTH || Array.IndexOf(TextUtil.PUNC_EOS, trimmed[^1]) != -1)
                return;

            anchors.Add(trimmed.Substring(Math.Max(0, trimmed.Length - ANCHOR_LENGTH)));
            if (anchors.Count > MAX_BREAKS)
                anchors.RemoveAt(0);
        }

        // A full-width full stop after CJK text, a period otherwise.
        private static string BreakMark(char previous)
        {
            return previous >= '\u2E80' ? "\u3002" : ".";
        }
    }
}
