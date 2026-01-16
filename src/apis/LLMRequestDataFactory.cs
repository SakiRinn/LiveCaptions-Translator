using System.Collections.Specialized;

using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.apis
{
    public static class LLMRequestDataFactory
    {
        private static readonly OrderedDictionary typeSequence = new()
        {
            ["integrated"] = typeof(IntegratedLLMRequestData),
            ["Aliyun"] = typeof(AliyunRequestData),
            ["Anthropic"] = typeof(AnthropicRequestData),
            ["Ollama"] = typeof(OllamaRequestData),
            ["OpenRouter"] = typeof(OpenRouterRequestData),
            ["OpenAI"] = typeof(OpenAIRequestData),
            ["XAI"] = typeof(XAIRequestData),
            ["base"] = typeof(BaseLLMRequestData)
        };

        public static int FallbackCount => typeSequence.Count;
        
        public static BaseLLMRequestData Create(string platform, string model, List<BaseLLMConfig.Message> messages, double temperature)
        {
            if (typeSequence[platform] == null)
                return null;
            return (BaseLLMRequestData)Activator.CreateInstance((Type)typeSequence[platform], model, messages, temperature);
        }
        
        public static BaseLLMRequestData Create(int index, string model, List<BaseLLMConfig.Message> messages, double temperature)
        {
            if (typeSequence[index] == null)
                return null;
            return (BaseLLMRequestData)Activator.CreateInstance((Type)typeSequence[index], model, messages, temperature);
        }
        
        public static BaseLLMRequestData Create(string model, List<BaseLLMConfig.Message> messages, double temperature)
        {
            return (BaseLLMRequestData)Activator.CreateInstance(typeof(BaseLLMRequestData), model, messages, temperature);
        }
    }
}