using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace LiveCaptionsTranslator.models.CaptionProviders
{
    public class OpenAICaptionProvider : ICaptionProvider
    {
        public bool SupportsAdaptiveSync => false;
        public string ProviderName => "OpenAI";

        public Task<string> GetCaptionsAsync(AutomationElement window, CancellationToken cancellationToken)
        {
            try
            {
                var captionsTextBlock = LiveCaptionsHandler.FindElementByAId(window, "CaptionsTextBlock");
                return Task.FromResult(captionsTextBlock?.Current.Name ?? string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting captions in OpenAICaptionProvider: {ex.Message}");
                return Task.FromResult(string.Empty);
            }
        }
    }
}
