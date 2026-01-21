namespace LiveCaptionsTranslator.models
{
    // Reasoning/thinking for all LLMs is disabled or minimal to reduce response time.

    public class BaseLLMRequestData(string model, List<BaseLLMConfig.Message> messages, double temperature)
    {
        public string model { get; set; } = model;
        public List<BaseLLMConfig.Message> messages { get; set; } = messages;
        public double temperature { get; set; } = temperature;

        public int max_tokens { get; set; } = 128;
        public bool stream { get; set; } = false;
    }

    public class IntegratedLLMRequestData(string model, List<BaseLLMConfig.Message> messages, double temperature)
        : BaseLLMRequestData(model, messages, temperature)
    {
        // Some platforms do not return 400/422 errors; instead, they automatically ignore incorrect parameters.
        // This request data is used for these platforms to ensure that model thinking is disabled.

        public class Reasoning
        {
            public bool exclude { get; set; } = true;
            public bool enabled { get; set; } = false;
            public string effort { get; set; } = "low";
        }
        public class Thinking
        {
            public string type { get; set; } = "disabled";
        }

        public bool think { get; set; } = false;
        public bool enable_thinking { get; set; } = false;
        public string reasoning_effort { get; set; } = "low";
        public Reasoning reasoning { get; set; } = new();
        public Thinking thinking { get; set; } = new();
    }

    public class OllamaRequestData(string model, List<BaseLLMConfig.Message> messages, double temperature)
        : BaseLLMRequestData(model, messages, temperature)
    {
        public bool think { get; set; } = false;
    }

    public class OpenRouterRequestData(string model, List<BaseLLMConfig.Message> messages, double temperature)
        : BaseLLMRequestData(model, messages, temperature)
    {
        public class Reasoning
        {
            public bool exclude { get; set; } = true;
            public bool enabled { get; set; } = false;
        }
        public Reasoning reasoning { get; set; } = new();
    }

    public class AnthropicRequestData(string model, List<BaseLLMConfig.Message> messages, double temperature)
        : BaseLLMRequestData(model, messages, temperature)
    {
        // Supported Platform: Anthropic, Zhipu (BigModel)
        public class Thinking
        {
            public string type { get; set; } = "disabled";
        }
        public Thinking thinking { get; set; } = new();
    }

    public class AliyunRequestData(string model, List<BaseLLMConfig.Message> messages, double temperature)
        : BaseLLMRequestData(model, messages, temperature)
    {
        // Supported Platform: Aliyun (Bailian), Silicon Flow
        public bool enable_thinking { get; set; } = false;
    }

    public class OpenAIRequestData(string model, List<BaseLLMConfig.Message> messages, double temperature)
        : BaseLLMRequestData(model, messages, temperature)
    {
        // Supported Platform: OpenAI, Silicon Flow (For reasoning models)
        public class Reasoning
        {
            public string effort { get; set; } = "low";
        }
        public Reasoning reasoning { get; set; } = new();
    }

    public class XAIRequestData(string model, List<BaseLLMConfig.Message> messages, double temperature)
        : BaseLLMRequestData(model, messages, temperature)
    {
        // Supported Platform: xAI (Grok)
        public string reasoning_effort { get; set; } = "low";
    }
}