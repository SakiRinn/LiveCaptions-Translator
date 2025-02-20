using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace LiveCaptionsTranslator.models.CaptionProviders
{
    public interface ICaptionProvider
    {
        /// <summary>
        /// Gets captions asynchronously from the specified window
        /// </summary>
        /// <param name="window">The automation element representing the window</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>The caption text</returns>
        Task<string> GetCaptionsAsync(AutomationElement window, CancellationToken cancellationToken);

        /// <summary>
        /// Indicates whether this provider supports adaptive synchronization
        /// </summary>
        bool SupportsAdaptiveSync { get; }

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        string ProviderName { get; }
    }
}
