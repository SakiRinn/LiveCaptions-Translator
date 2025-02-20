using System;
using System.Collections.Generic;

namespace LiveCaptionsTranslator.models.CaptionProviders
{
    public static class CaptionProviderFactory
    {
        private static readonly Dictionary<string, Func<ICaptionProvider>> _providers = new()
        {
            { "OpenAI", () => new OpenAICaptionProvider() },
            { "Ollama", () => new OllamaCaptionProvider() }
        };

        /// <summary>
        /// Gets a caption provider instance by name
        /// </summary>
        /// <param name="providerName">Name of the provider (e.g., "OpenAI", "Ollama")</param>
        /// <returns>An instance of the requested caption provider</returns>
        /// <exception cref="ArgumentException">Thrown when provider name is not recognized</exception>
        public static ICaptionProvider GetProvider(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                throw new ArgumentException("Provider name cannot be empty", nameof(providerName));
            }

            if (_providers.TryGetValue(providerName, out var factory))
            {
                return factory();
            }

            throw new ArgumentException($"Unsupported caption provider: {providerName}", nameof(providerName));
        }

        /// <summary>
        /// Gets a list of available provider names
        /// </summary>
        public static IEnumerable<string> AvailableProviders => _providers.Keys;
    }
}
