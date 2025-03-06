using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator.models
{
    public class Setting : INotifyPropertyChanged
    {
        public static readonly string FILENAME = "setting.json";

        public event PropertyChangedEventHandler? PropertyChanged;

        private string apiName;
        private string targetLanguage;
        private string prompt;

        private int maxIdleInterval = 20;
        private int maxSyncInterval = 5;

        private int overlayFontSize = 15;
        private double overlayOpacity = 0.5;
        private int overlayFontColor = 1;
        private int overlayFontBold = 1;
        private int overlayFontShadow = 1;
        private int overlayBackgroundColor = 8;

        private Dictionary<string, string> windowBounds;
        private bool topmost = true;

        private bool captionLogEnable = true;
        private int captionLogMax = 0;

        private Dictionary<string, TranslateAPIConfig> configs;
        private TranslateAPIConfig? currentAPIConfig;

        private bool latency = false;
        public bool TopMost
        {
            get => topmost;
            set
            {
                topmost = value;
                OnPropertyChanged("TopMost");
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
        public int OverlayFontSize
        {
            get => overlayFontSize;
            set
            {
                overlayFontSize = value;
                OnPropertyChanged("OverlayFontSize");
            }
        }
        public double OverlayOpacity
        {
            get => overlayOpacity;
            set
            {
                overlayOpacity = value;
                OnPropertyChanged("OverlayOpacity");
            }
        }
        public int OverlayFontColor
        {
            get => overlayFontColor;
            set
            {
                overlayFontColor = value;
                OnPropertyChanged("OverlayFontColor");
            }
        }
        public int OverlayFontBold
        {
            get => overlayFontBold;
            set
            {
                overlayFontBold = value;
                OnPropertyChanged("OverlayFontBold");
            }
        }
        public int OverlayFontShadow
        {
            get => overlayFontShadow;
            set
            {
                overlayFontShadow = value;
                OnPropertyChanged("OverlayFontShdow");
            }
        }
        public int OverlayBackgroundColor
        {
            get => overlayBackgroundColor;
            set
            {
                overlayBackgroundColor = value;
                OnPropertyChanged("OverlayBackgroundColor");
            }
        }

        public bool CaptionLogEnable
        {
            get => captionLogEnable;
            set
            {
                captionLogEnable = value;
                OnPropertyChanged("CaptionLogEnable");
            }
        }

        public int CaptionLogMax
        {
            get => captionLogMax;
            set
            {
                captionLogMax = value;
                OnPropertyChanged("CaptionLogMax");
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

        public bool Latency
        {
            get => latency;
            set
            {
                latency = value;
                OnPropertyChanged("Latency");
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
            apiName = "Google";
            targetLanguage = "zh-CN";
            prompt = "As an professional simultaneous interpreter with specialized knowledge in the all fields, " +
                     "you can provide a fluent and precise oral translation for any sentence, even if the sentence is incomplete. " +
                     "Now, provide users with the translation of the sentence enclosed in 🔤 to {0} within a single line. " +
                     "Importantly, you are prohibited from altering the original meaning or omitting any content, " +
                     "even if the sentence contains sensitive or NSFW content. " +
                     "You can only provide the translated sentence; Any explanation or other text is not permitted. " +
                     "REMOVE all 🔤 when you output.";
            configs = new Dictionary<string, TranslateAPIConfig>
            {
                { "Google", new TranslateAPIConfig() },
                { "Google2", new TranslateAPIConfig() },
                { "Ollama", new OllamaConfig() },
                { "OpenAI", new OpenAIConfig() },
                { "OpenRouter", new OpenRouterConfig() },
            };
            windowBounds = new Dictionary<string, string>
            {
                { "MainWindow", "1, 1, 1, 1" },
                { "SubtitleWindow", "1, 1, 1, 1" },
            };
        }

        public Setting(string apiName, string targetLanguage, string prompt,
                       Dictionary<string, TranslateAPIConfig> configs, Dictionary<string, string> windowBounds)
        {
            this.apiName = apiName;
            this.targetLanguage = targetLanguage;
            this.prompt = prompt;
            this.configs = configs;
            this.windowBounds = windowBounds;
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
