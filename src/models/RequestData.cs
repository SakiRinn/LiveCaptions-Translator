using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LiveCaptionsTranslator.models
{
    // Reasoning/thinking for all LLMs is disabled or minimal to reduce response time.

    public class BaseLLMRequestData(string model, List<BaseLLMConfig.Message> messages, double temperature)
    {
        public string model { get; set; } = model;
        public List<BaseLLMConfig.Message> messages { get; set; } = messages;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? temperature { get; set; } = temperature;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? max_tokens { get; set; } = 128;
        public bool stream { get; set; } = false;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? keep_alive { get; set; } = 600;
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

    public partial class OpenAIReasoningRequestData : BaseLLMRequestData
    {
        // Supported Platform: OpenAI (GPT-5+, o-series) via Chat Completions.
        // These models reject max_tokens, keep_alive and (while reasoning) temperature,
        // and take a flat "reasoning_effort" instead of a nested "reasoning" object.
        // max_completion_tokens also covers hidden reasoning tokens, so the budget is larger than for other LLMs.

        [GeneratedRegex(@"(?:^|/)gpt-(\d+)(?:\.(\d+))?(.*)$", RegexOptions.IgnoreCase)]
        private static partial Regex GptModelPattern();
        [GeneratedRegex(@"(?:^|/)o\d", RegexOptions.IgnoreCase)]
        private static partial Regex OSeriesPattern();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string reasoning_effort { get; set; }
        public int max_completion_tokens { get; set; } = 1024;

        public OpenAIReasoningRequestData(string model, List<BaseLLMConfig.Message> messages,
            double temperature, string reasoningEffort)
            : base(model, messages, temperature)
        {
            max_tokens = null;
            keep_alive = null;
            reasoning_effort = string.IsNullOrEmpty(reasoningEffort) ? null : reasoningEffort;
            // Temperature is only accepted when the model is not reasoning (effort "none" or a chat model).
            if (reasoningEffort != null && reasoningEffort != "none")
                this.temperature = null;
        }

        public static bool IsReasoningModel(string model)
        {
            if (string.IsNullOrEmpty(model))
                return false;
            if (OSeriesPattern().IsMatch(model))
                return true;
            var match = GptModelPattern().Match(model);
            return match.Success && int.TryParse(match.Groups[1].Value, out int major) && major >= 5;
        }

        // Efforts to try in order; the accepted set differs between model generations
        // (gpt-5: minimal/low/medium/high, gpt-5.1+: none/low/medium/high, pro: high-only variants).
        // A null entry omits reasoning_effort (non-reasoning chat models); "" omits it together with temperature,
        // as a last resort that lets any future model run on its server-side defaults.
        public static string[] EffortCandidates(string model)
        {
            if (OSeriesPattern().IsMatch(model))
                return ["low", "medium", ""];

            var match = GptModelPattern().Match(model);
            string suffix = match.Groups[3].Value.ToLowerInvariant();
            if (suffix.Contains("chat"))
                return [null, ""];
            if (suffix.Contains("pro"))
                return ["high", "medium", ""];
            if (match.Groups[1].Value == "5" && !match.Groups[2].Success)
                return ["minimal", "low", "medium", ""];
            return ["none", "low", "minimal", "medium", ""];
        }
    }

    public class XAIRequestData(string model, List<BaseLLMConfig.Message> messages, double temperature)
        : BaseLLMRequestData(model, messages, temperature)
    {
        // Supported Platform: xAI (Grok)
        public string reasoning_effort { get; set; } = "low";
    }
}