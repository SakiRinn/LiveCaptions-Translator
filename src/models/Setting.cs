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

        private int maxIdleInterval = 20;
        private int maxSyncInterval = 5;

        private string apiName;
        private string targetLanguage;
        private string prompt;

        private MainWindowState mainWindowState;
        private SubtitleWindowState subtitleWindowState;

        private Dictionary<string, string> windowBounds;

        private Dictionary<string, TranslateAPIConfig> configs;
        private TranslateAPIConfig? currentAPIConfig;

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

        public MainWindowState MainWindow
        {
            get => mainWindowState;
            set
            {
                mainWindowState = value;
                OnPropertyChanged("MainWindow");
            }
        }
        public SubtitleWindowState SubtitleWindow
        {
            get => subtitleWindowState;
            set
            {
                subtitleWindowState = value;
                OnPropertyChanged("SubtitleWindow");
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
            set => currentAPIConfig = value;
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
            mainWindowState = new MainWindowState
            {
                Topmost = true,
                CaptionLogEnabled = false,
                CaptionLogMax = 2,
                LatencyShow = false
            };
            subtitleWindowState = new SubtitleWindowState
            {
                FontSize = 15,
                FontColor = 1,
                FontBold = 1,
                FontShadow = 1,
                BackgroundColor = 8,
                Opacity = 0.5
            };
            windowBounds = new Dictionary<string, string>
            {
                { "MainWindow", "1, 1, 1, 1" },
                { "SubtitleWindow", "1, 1, 1, 1" },
            };
            configs = new Dictionary<string, TranslateAPIConfig>
            {
                { "Google", new TranslateAPIConfig() },
                { "Google2", new TranslateAPIConfig() },
                { "Ollama", new OllamaConfig() },
                { "OpenAI", new OpenAIConfig() },
                { "OpenRouter", new OpenRouterConfig() },
            };
        }

        public Setting(string apiName, string targetLanguage, string prompt,
                       MainWindowState mainWindowState, SubtitleWindowState subtitleWindowState,
                       Dictionary<string, TranslateAPIConfig> configs, Dictionary<string, string> windowBounds)
        {
            this.apiName = apiName;
            this.targetLanguage = targetLanguage;
            this.prompt = prompt;
            this.mainWindowState = mainWindowState;
            this.subtitleWindowState = subtitleWindowState;
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
            App.Setting?.Save();
        }
    }

    public class MainWindowState
    {
        public bool Topmost { get; set; }
        public bool CaptionLogEnabled { get; set; }
        public int CaptionLogMax { get; set; }
        public bool LatencyShow { get; set; }
    }

    public class SubtitleWindowState
    {
        public int FontSize { get; set; }
        public int FontColor { get; set; }
        public int FontBold { get; set; }
        public int FontShadow { get; set; }
        public int BackgroundColor { get; set; }
        public double Opacity { get; set; }
    }
}
