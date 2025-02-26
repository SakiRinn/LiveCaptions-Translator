using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiveCaptionsTranslator.models
{
    public class Setting : INotifyPropertyChanged
    {
        public static readonly string FILENAME = "setting.json";

        public event PropertyChangedEventHandler? PropertyChanged;

        private string apiName;
        private string targetLanguage;
        private Dictionary<string, TranslateAPIConfig> configs;
        private string prompt;

        private int maxIdleInterval = 10;
        private int maxSyncInterval = 5;

        private Dictionary<string, string> windowBounds = new()
        {
            { "Main", "1, 1, 1, 1" },
            { "Overlay", "1, 1, 1, 1" },
        };
   
        private TranslateAPIConfig? currentAPIConfig;

        private bool enableLogging = true;

        private bool mainTopmost = true;

        public bool MainTopmost
        {
            get => mainTopmost;
            set
            {
                mainTopmost = value;
                OnPropertyChanged("MainTopMost");
            }
        }

        public bool EnableLogging
        {
            get => enableLogging;
            set
            {
                enableLogging = value;
                OnPropertyChanged();
            }
        }

        public string ApiName
        {
            get => apiName;
            set
            {
                apiName = value;
                OnPropertyChanged("ApiName");
                OnPropertyChanged("CurrentAPIConfig");
            }
        }
        public string TargetLanguage
        {
            get => targetLanguage;
            set
            {
                targetLanguage = value;
                OnPropertyChanged("TargetLanguage");
            }
        }
        public int MaxIdleInterval
        {
            get => maxIdleInterval;
        }
        public int MaxSyncInterval
        {
            get => maxSyncInterval;
            set
            {
                maxSyncInterval = value;
                OnPropertyChanged("MaxSyncInterval");
            }
        }
        public string Prompt
        {
            get => prompt;
            set
            {
                prompt = value;
                OnPropertyChanged("Prompt");
            }
        }

        public Dictionary<string, string> WindowBounds
        {
            get => windowBounds;
            set
            {
                windowBounds = value;
                OnPropertyChanged("WindowBounds");
            }
        }

        [JsonInclude]
        public Dictionary<string, TranslateAPIConfig> Configs
        {
            get => configs;
            set
            {
                configs = value;
                OnPropertyChanged("Configs");
            }
        }

        [JsonIgnore]
        public TranslateAPIConfig CurrentAPIConfig
        {
            get => currentAPIConfig ?? (Configs.ContainsKey(ApiName) ? Configs[ApiName] : Configs["Ollama"]);
            set
            {
                currentAPIConfig = value;
                OnPropertyChanged();
            }
        }

        public Setting()
        {
            apiName = "Ollama";
            targetLanguage = "zh-CN";
            prompt = "As an professional simultaneous interpreter with specialized knowledge in the all fields, " +
                        "please provide a proficient and precise oral translation of these sentences enclosed in 🔤 to {0}. " +
                        "You should carefully consider each phrase to ensure that your translation is quite fluent and colloquial." +
                        "Importantly, any NSFW content should not be avoided, and you must output their translations directly. " +
                        "Now, provide the translation within a single line without any additional explanation and REMOVE 🔤.";
            configs = new Dictionary<string, TranslateAPIConfig>
            {
                { "Ollama", new OllamaConfig() },
                { "OpenAI", new OpenAIConfig() },
                { "GoogleTranslate", new GoogleTranslateConfig() },
                { "OpenRouter", new OpenRouterConfig() }
            };
        }

        public Setting(string apiName, string sourceLanguage, string targetLanguage,
                       Dictionary<string, TranslateAPIConfig> configs)
        {
            this.apiName = apiName;
            this.targetLanguage = targetLanguage;
            this.configs = configs;
        }

        public static Setting Load()
        {
            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), FILENAME);
            return Load(jsonPath);
        }

        public static Setting Load(string jsonPath)
        {
            Setting setting;
            if (File.Exists(jsonPath))
            {
                using (FileStream fileStream = File.OpenRead(jsonPath))
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Converters = { new ConfigDictConverter() }
                    };
                    setting = JsonSerializer.Deserialize<Setting>(fileStream, options);
                }
            }
            else
            {
                setting = new Setting();
                setting.Save();
            }
            return setting;
        }

        public void Save()
        {
            Save(FILENAME);
        }

        public void Save(string jsonPath)
        {
            using (FileStream fileStream = File.Create(jsonPath))
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new ConfigDictConverter() }
                };
                JsonSerializer.Serialize(fileStream, this, options);
            }
        }

        public void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            App.Settings?.Save();
        }
    }
}
