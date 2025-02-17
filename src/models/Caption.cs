using System.Windows.Automation;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

using LiveCaptionsTranslator.controllers;

namespace LiveCaptionsTranslator.models
{
    public class Caption : INotifyPropertyChanged, IDisposable
    {
        // 单例实现改为线程安全
        private static readonly Lazy<Caption> _instance = 
            new Lazy<Caption>(() => new Caption(), LazyThreadSafetyMode.ExecutionAndPublication);
        public static Caption Instance => _instance.Value;

        // 事件和属性变更通知
        public event PropertyChangedEventHandler? PropertyChanged;
        private readonly SynchronizationContext? _syncContext;

        // 性能优化：缓存正则表达式和字符数组
        private static readonly char[] PUNC_EOS = ".?!。？！".ToCharArray();
        private static readonly char[] PUNC_COMMA = ",，、—\n".ToCharArray();

        // 线程安全的字段
        private string _original = "";
        private string _translated = "";
        private volatile bool _pauseFlag = false;
        private volatile bool _translateFlag = false;
        private volatile bool _eosFlag = false;

        // 性能监控
        private readonly ConcurrentQueue<long> _syncLatencies = new ConcurrentQueue<long>();
        private readonly ConcurrentQueue<long> _translateLatencies = new ConcurrentQueue<long>();

        // 取消令牌支持
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public string Original
        {
            get => _original;
            private set
            {
                if (_original != value)
                {
                    _original = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Translated
        {
            get => _translated;
            private set
            {
                if (_translated != value)
                {
                    _translated = value;
                    OnPropertyChanged();
                }
            }
        }

        private Caption()
        {
            _syncContext = SynchronizationContext.Current;
        }

        public void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            if (_syncContext != null)
            {
                _syncContext.Post(_ => 
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName)), null);
            }
            else
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            }
        }

        public async Task StartSyncAsync()
        {
            int idleCount = 0;
            int syncCount = 0;
            var token = _cts.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var stopwatch = Stopwatch.StartNew();

                    if (_pauseFlag || App.Window == null)
                    {
                        await Task.Delay(50, token);
                        continue;
                    }

                    string fullText = await Task.Run(() => GetCaptions(App.Window).Trim(), token);
                    
                    if (string.IsNullOrEmpty(fullText))
                    {
                        await Task.Delay(50, token);
                        continue;
                    }

                    string processedText = ProcessCaptionText(fullText);

                    if (Original != processedText)
                    {
                        idleCount = 0;
                        syncCount++;
                        Original = processedText;

                        UpdateTranslationFlags(processedText, ref syncCount);
                    }
                    else
                    {
                        idleCount++;
                    }

                    stopwatch.Stop();
                    _syncLatencies.Enqueue(stopwatch.ElapsedMilliseconds);
                    
                    // 保持性能监控队列大小
                    while (_syncLatencies.Count > 100)
                    {
                        _syncLatencies.TryDequeue(out _);
                    }

                    await Task.Delay(50, token);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                // 记录异常
                Console.Error.WriteLine($"Sync error: {ex}");
            }
        }

        private string ProcessCaptionText(string fullText)
        {
            // 优化文本处理逻辑
            var processedText = new StringBuilder(fullText);
            foreach (char eos in PUNC_EOS)
            {
                processedText.Replace($"{eos}\n", $"{eos}");
            }

            return OptimizeCaptionLength(processedText.ToString());
        }

        private string OptimizeCaptionLength(string text)
        {
            int lastEOSIndex = text.LastIndexOfAny(PUNC_EOS);
            string latestCaption = text.Substring(lastEOSIndex + 1);

            // 使用更高效的长度控制
            while (Encoding.UTF8.GetByteCount(latestCaption) > 170)
            {
                int commaIndex = latestCaption.IndexOfAny(PUNC_COMMA);
                if (commaIndex < 0 || commaIndex + 1 == latestCaption.Length)
                    break;
                latestCaption = latestCaption.Substring(commaIndex + 1);
            }

            return latestCaption.Replace("\n", "——");
        }

        private void UpdateTranslationFlags(string caption, ref int syncCount)
        {
            if (Array.IndexOf(PUNC_EOS, caption[^1]) != -1 ||
                Array.IndexOf(PUNC_COMMA, caption[^1]) != -1)
            {
                syncCount = 0;
                _translateFlag = true;
                _eosFlag = true;
            }
            else
            {
                _eosFlag = false;
            }

            if (syncCount > App.Settings.MaxSyncInterval)
            {
                syncCount = 0;
                _translateFlag = true;
            }
        }

        public async Task StartTranslateAsync()
        {
            var controller = new TranslationController();
            var token = _cts.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var stopwatch = Stopwatch.StartNew();

                    await HandlePauseState();

                    if (_translateFlag)
                    {
                        Translated = await controller.TranslateAndLogAsync(Original);
                        _translateFlag = false;

                        stopwatch.Stop();
                        _translateLatencies.Enqueue(stopwatch.ElapsedMilliseconds);

                        // 保持性能监控队列大小
                        while (_translateLatencies.Count > 100)
                        {
                            _translateLatencies.TryDequeue(out _);
                        }

                        if (_eosFlag)
                            await Task.Delay(500, token);
                    }

                    await Task.Delay(50, token);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                // 记录异常
                Console.Error.WriteLine($"Translate error: {ex}");
            }
        }

        private async Task HandlePauseState()
        {
            for (int pauseCount = 0; _pauseFlag; pauseCount++)
            {
                if (pauseCount > 60 && App.Window != null)
                {
                    App.Window = null;
                    LiveCaptionsHandler.KillLiveCaptions();
                }
                await Task.Delay(1000);
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        // 性能分析方法
        public (double avgSyncLatency, double avgTranslateLatency) GetPerformanceMetrics()
        {
            double avgSyncLatency = _syncLatencies.Count > 0 
                ? _syncLatencies.Average() 
                : 0;
            
            double avgTranslateLatency = _translateLatencies.Count > 0 
                ? _translateLatencies.Average() 
                : 0;

            return (avgSyncLatency, avgTranslateLatency);
        }
    }
}
