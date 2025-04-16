using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace LiveCaptionsTranslator.models
{
    public class TranslateAPIConfig : INotifyPropertyChanged
    {
        /*
         * The key of this property is used as the content for `targetLangBox` in the `SettingPage`.
         * Its purpose is to standardize the language selection interface.
         * Therefore, if your API doesn't follow the key format, please override this property.
         * (See the definition of `DeepLConfig` for an example)
         */
        [JsonIgnore]
        public virtual Dictionary<string, string> SupportedLanguages { get; } = new()
        {
            { "zh-CN", "zh-CN" },
            { "zh-TW", "zh-TW" },
            { "en-US", "en-US" },
            { "en-GB", "en-GB" },
            { "ja-JP", "ja-JP" },
            { "ko-KR", "ko-KR" },
            { "fr-FR", "fr-FR" },
            { "th-TH", "th-TH" },
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            Translator.Setting?.Save();
        }
    }

    public class BaseLLMConfig : TranslateAPIConfig
    {
        public class Message
        {
            public string role { get; set; }
            public string content { get; set; }
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

    public class OllamaConfig : BaseLLMConfig
    {
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

        private int port = 11434;

        public int Port
        {
            get => port;
            set
            {
                port = value;
                OnPropertyChanged("Port");
            }
        }
    }

    public class OpenAIConfig : BaseLLMConfig
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
        public class Response
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

    public class OpenRouterConfig : BaseLLMConfig
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
    
    public class DeepLConfig : TranslateAPIConfig
    {
        [JsonIgnore]
        public override Dictionary<string, string> SupportedLanguages { get; } = new()
        {
            { "zh-CN", "ZH-HANS" },
            { "zh-TW", "ZH-HANT" },
            { "en-US", "EN-US" },
            { "en-GB", "EN-GB" },
            { "ja-JP", "JA" },
            { "ko-KR", "KO" },
            { "fr-FR", "FR" },
        };

        private string apiKey = "";
        private string apiUrl = "https://api.deepl.com/v2/translate";

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
    public class YoudaoConfig : TranslateAPIConfig
    {
        public class TranslationResult
        {
            public string errorCode { get; set; }
            public string query { get; set; }
            public List<string> translation { get; set; }
            public string l { get; set; }
            public string tSpeakUrl { get; set; }
            public string speakUrl { get; set; }
        }

        [JsonIgnore]
        public override Dictionary<string, string> SupportedLanguages { get; } = new()
    {
        { "zh-CN", "zh-CHS" }, 
        { "zh-TW", "zh-CHT" }, 
        { "en-US", "en" },      
        { "ja-JP", "ja" },      
        { "ko-KR", "ko" },     
        { "fr-FR", "fr" },      
        { "th-TH", "th" },
    };

        private string appKey = "";
        private string appSecret = "";
        private string apiUrl = "https://openapi.youdao.com/api";

        public string AppKey
        {
            get => appKey;
            set
            {
                appKey = value;
                OnPropertyChanged("AppKey");
            }
        }

        public string AppSecret
        {
            get => appSecret;
            set
            {
                appSecret = value;
                OnPropertyChanged("AppSecret");
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

    public class MTranServerConfig : TranslateAPIConfig
    {
        [JsonIgnore]
        public override Dictionary<string, string> SupportedLanguages { get; } = new()
        {
            { "zh-CN", "zh" },
            { "zh-TW", "zh" },
            { "en-US", "en" },
            { "en-GB", "en" },
            { "ja-JP", "ja" },
            { "ko-KR", "ko" },
            { "fr-FR", "fr" },
        };

        private string apiKey = "";
        private string apiUrl = "http://localhost:8989/translate";
        private string sourceLanguage = "en";

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

        public string SourceLanguage 
        {
            get => sourceLanguage;
            set
            {
                sourceLanguage = value;
                OnPropertyChanged("SourceLanguage");
            }
        }

        public class Response
        {
            public string result { get; set; }
        }
    }

    public class MTranServerCoreConfig : TranslateAPIConfig
    {
        private string apiUrl = "http://localhost:3000/translate";
        private string apiKey = "";
        private string sourceLanguage = "en";

        public string ApiUrl
        {
            get => apiUrl;
            set
            {
                apiUrl = value;
                OnPropertyChanged("ApiUrl");
            }
        }

        public string ApiKey
        {
            get => apiKey;
            set
            {
                apiKey = value;
                OnPropertyChanged("ApiKey");
            }
        }

        public string SourceLanguage
        {
            get => sourceLanguage;
            set
            {
                sourceLanguage = value;
                OnPropertyChanged("SourceLanguage");
            }
        }

        [JsonIgnore]
        public override Dictionary<string, string> SupportedLanguages { get; } = new()
        {
            { "zh-CN", "zh" },
            { "en-US", "en" },
            { "ja-JP", "ja" },
            { "ko-KR", "ko" },
            { "ru-RU", "ru" },
            { "fr-FR", "fr" },
            { "es-ES", "es" },
            { "pt-PT", "pt" },
            { "it-IT", "it" },
            { "de-DE", "de" },
            { "vi-VN", "vi" },
            { "th-TH", "th" },
            { "id-ID", "id" },
            { "ms-MY", "ms" }
        };

        public class Response
        {
            public string text { get; set; }
        }
    }
}
