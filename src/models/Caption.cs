using System.Windows.Automation;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using LiveCaptionsTranslator.controllers;
using LiveCaptionsTranslator.models.CaptionProviders;
using LiveCaptionsTranslator.models.CaptionProcessing;


namespace LiveCaptionsTranslator.models
{
    public class Caption : INotifyPropertyChanged, IDisposable
    {
        // 单例模式
        private static Caption? instance = null;
        private static readonly object _lock = new object();

        public event PropertyChangedEventHandler? PropertyChanged;

        private string original = "";
        private string translated = "";
        private readonly Queue<CaptionHistoryItem> captionHistory = new(5);
        private ICaptionProvider _captionProvider;
        private CancellationTokenSource? _syncCts;

        public class CaptionHistoryItem
        {
            public string Original { get; set; }
            public string Translated { get; set; }
        }

        // 保留原有的公共属性
        public IEnumerable<CaptionHistoryItem> CaptionHistory => captionHistory.Reverse();
        public bool PauseFlag { get; set; } = false;
        public bool TranslateFlag { get; set; } = false;
        private bool EOSFlag { get; set; } = false;

        public string Original
        {
            get => original;
            set
            {
                original = value;
                OnPropertyChanged(nameof(Original));
            }
        }

        public string Translated
        {
            get => translated;
            set
            {
                translated = value;
                OnPropertyChanged(nameof(Translated));
            }
        }

        // 单例获取方法
        public static Caption GetInstance()
        {
            if (instance == null)
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = new Caption();
                    }
                }
            }
            return instance;
        }

        private Caption() { }

        public void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public void InitializeProvider(string providerName)
        {
            _captionProvider = CaptionProviderFactory.GetProvider(providerName);
            _syncCts?.Cancel();
            _syncCts = new CancellationTokenSource();
        }

        public async Task SyncAsync()
        {
            if (_captionProvider == null)
            {
                throw new InvalidOperationException("Caption provider not initialized");
            }

            var token = _syncCts?.Token ?? CancellationToken.None;
            int syncCount = 0;

            while (!token.IsCancellationRequested)
            {
                if (PauseFlag || App.Window == null)
                {
                    await Task.Delay(50, token);
                    continue;
                }

                try
                {
                    string fullText = (await _captionProvider.GetCaptionsAsync(App.Window, token)).Trim();
                    if (string.IsNullOrEmpty(fullText))
                    {
                        await Task.Delay(50, token);
                        continue;
                    }

                    fullText = CaptionTextProcessor.ProcessFullText(fullText);
                    int lastEOSIndex = CaptionTextProcessor.GetLastEOSIndex(fullText);
                    string latestCaption = CaptionTextProcessor.ExtractLatestCaption(fullText, lastEOSIndex);

                    if (Original.CompareTo(latestCaption) != 0)
                    {
                        syncCount++;
                        Original = latestCaption;
                        TranslateFlag = CaptionTextProcessor.ShouldTriggerTranslation(latestCaption, ref syncCount, App.Settings.MaxSyncInterval);
                        EOSFlag = Array.IndexOf(CaptionTextProcessor.PUNC_EOS, latestCaption[^1]) != -1;
                    }

                    await Task.Delay(_captionProvider.SupportsAdaptiveSync ? 30 : 50, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Sync error: {ex.Message}");
                    await Task.Delay(50, token);
                }
            }
        }


        public async Task TranslateAsync(CancellationToken cancellationToken = default)
        {
            var controller = new TranslationController();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (PauseFlag)
                {
                    int pauseCount = 0;
                    while (PauseFlag && !cancellationToken.IsCancellationRequested)
                    {
                        if (pauseCount > 60 && App.Window != null)
                        {
                            App.Window = null;
                            LiveCaptionsHandler.KillLiveCaptions();
                        }
                        await Task.Delay(1000, cancellationToken);
                        pauseCount++;
                    }
                    continue;
                }

                try
                {
                    if (TranslateFlag)
                    {
                        Translated = await controller.TranslateAndLogAsync(Original);
                        TranslateFlag = false;

                        // Add to history
                        if (!string.IsNullOrEmpty(Original) && !string.IsNullOrEmpty(Translated))
                        {
                            var lastHistory = captionHistory.LastOrDefault();
                            if (lastHistory == null || 
                                lastHistory.Original != Original || 
                                lastHistory.Translated != Translated)
                            {
                                if (captionHistory.Count >= 5)
                                    captionHistory.Dequeue();
                                captionHistory.Enqueue(new CaptionHistoryItem 
                                { 
                                    Original = Original, 
                                    Translated = Translated 
                                });
                                OnPropertyChanged(nameof(CaptionHistory));
                            }
                        }

                        if (EOSFlag)
                            await Task.Delay(1000, cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Translate error: {ex.Message}");
                }

                await Task.Delay(50, cancellationToken);
            }
        }

        public void ClearHistory()
        {
            captionHistory.Clear();
            OnPropertyChanged(nameof(CaptionHistory));
        }

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _syncCts?.Cancel();
                _syncCts?.Dispose();
                _syncCts = null;
                instance = null;
            }
        }
    }
}
