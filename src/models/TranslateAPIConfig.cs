using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace LiveCaptionsTranslator.models
{
    public abstract class TranslateAPIConfig : INotifyPropertyChanged
    {
        [JsonIgnore] public abstract Dictionary<string, string> SupportedLanguages { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            App.Settings?.Save();
        }
    }

    public class OllamaConfig : TranslateAPIConfig
    {
        public static readonly Dictionary<string, string> SUPPORTED_LANGUAGES = new()
        {
            { "zh-CN", "Simplified Chinese" },
            { "zh-TW", "Traditional Chinese" },
            { "en-US", "American English" },
            { "en-GB", "British English" },
            { "ja-JP", "Japanese" },
            { "ko-KR", "Korean" },
            { "fr-FR", "French" }
        };

        [JsonIgnore]
        public override Dictionary<string, string> SupportedLanguages
        {
            get => SUPPORTED_LANGUAGES;
        }

        public class Message
        {
            public string role { get; set; }
            public string content { get; set; }
        }

        public class Response
        {
            public string model { get; set; }
            public DateTime created_at { get; set; }
            public Message message { get; set; }
            public bool done { get; set; }
            public long total_duration { get; set; }
            public int load_duration { get; set; }
            public int prompt_eval_count { get; set; }
            public long prompt_eval_duration { get; set; }
            public int eval_count { get; set; }
            public long eval_duration { get; set; }
        }

        private string modelName = "";
        private double temperature = 1.0;

        public string ModelName
        {
            get => modelName;
            set
            {
                modelName = value;
                OnPropertyChanged("ModelName");
            }
        }

        public double Temperature
        {
            get => temperature;
            set
            {
                temperature = value;
                OnPropertyChanged("Temperature");
            }
        }
    }

    public class OpenAIConfig : OllamaConfig
    {
        public class Choice
        {
            public int index { get; set; }
            public Message message { get; set; }
            public string logprobs { get; set; }
            public string finish_reason { get; set; }
        }
        public class Usage
        {
            public int prompt_tokens { get; set; }
            public int completion_tokens { get; set; }
            public int total_tokens { get; set; }
            public int prompt_cache_hit_tokens { get; set; }
            public int prompt_cache_miss_tokens { get; set; }
        }
        public new class Response
        {
            public string id { get; set; }
            public string @object { get; set; }
            public int created { get; set; }
            public string model { get; set; }
            public List<Choice> choices { get; set; }
            public Usage usage { get; set; }
            public string system_fingerprint { get; set; }
        }

        private string apiKey = "";
        private string apiUrl = "";

        public string ApiKey
        {
            get => apiKey;
            set
            {
                apiKey = value;
                OnPropertyChanged("ApiKey");
            }
        }
        public string ApiUrl
        {
            get => apiUrl;
            set
            {
                apiUrl = value;
                OnPropertyChanged("ApiUrl");
            }
        }
    }

    public class OpenRouterConfig : OllamaConfig
    {
        private string apiKey = "";
        public string ApiKey
        {
            get => apiKey;
            set
            {
                apiKey = value;
                OnPropertyChanged();
            }
        }
    }

    public class GoogleTranslateConfig : TranslateAPIConfig
    {
        private static readonly Dictionary<string, string> SUPPORTED_LANGUAGES = new()
        {
            { "zh-CN", "Simplified Chinese" },
            { "zh-TW", "Traditional Chinese" },
            { "en-US", "American English" },
            { "en-GB", "British English" },
            { "ja-JP", "Japanese" },
            { "ko-KR", "Korean" },
            { "fr-FR", "French" }
        };

        [JsonIgnore]
        public override Dictionary<string, string> SupportedLanguages => SUPPORTED_LANGUAGES;
    }
}
