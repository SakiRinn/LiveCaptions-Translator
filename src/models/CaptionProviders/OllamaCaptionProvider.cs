using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace LiveCaptionsTranslator.models.CaptionProviders
{
    public class OllamaCaptionProvider : ICaptionProvider
    {
        private const int DEFAULT_TIMEOUT_MS = 100;
        private const int MAX_RETRY_ATTEMPTS = 3;
        private readonly Stopwatch _performanceWatch;
        private double _averageProcessingTime;
        private int _processedCount;

        public OllamaCaptionProvider()
        {
            _performanceWatch = new Stopwatch();
            _averageProcessingTime = DEFAULT_TIMEOUT_MS;
            _processedCount = 0;
        }

        public bool SupportsAdaptiveSync => true;
        public string ProviderName => "Ollama";

        public async Task<string> GetCaptionsAsync(AutomationElement window, CancellationToken cancellationToken)
        {
            _performanceWatch.Restart();
            
            try
            {
                for (int attempt = 0; attempt < MAX_RETRY_ATTEMPTS; attempt++)
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(CalculateTimeout()));

                    try
                    {
                        var captionsTextBlock = await Task.Run(() => 
                            LiveCaptionsHandler.FindElementByAId(window, "CaptionsTextBlock"), 
                            timeoutCts.Token);

                        if (captionsTextBlock != null)
                        {
                            var caption = captionsTextBlock.Current.Name;
                            UpdatePerformanceMetrics();
                            return caption;
                        }
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        // Only timeout occurred, not external cancellation
                        if (attempt < MAX_RETRY_ATTEMPTS - 1)
                        {
                            await Task.Delay(10, cancellationToken); // Short delay before retry
                            continue;
                        }
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting captions in OllamaCaptionProvider: {ex.Message}");
                return string.Empty;
            }
            finally
            {
                _performanceWatch.Stop();
            }
        }

        private int CalculateTimeout()
        {
            // Dynamic timeout based on moving average of processing time
            // Add 50% buffer to average processing time
            return (int)Math.Max(DEFAULT_TIMEOUT_MS, _averageProcessingTime * 1.5);
        }

        private void UpdatePerformanceMetrics()
        {
            var currentTime = _performanceWatch.ElapsedMilliseconds;
            _processedCount++;

            // Exponential moving average with 0.2 weight for new values
            _averageProcessingTime = (_averageProcessingTime * 0.8) + (currentTime * 0.2);
        }
    }
}
