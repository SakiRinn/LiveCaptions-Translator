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
        private string sourceLanguage;
        private string targetLanguage;
        private Dictionary<string, TranslateAPIConfig> configs;

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
        public string SourceLanguage
        {
            get => sourceLanguage;
            set
            {
                sourceLanguage = value;
                OnPropertyChanged("SourceLanguage");
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

        [JsonInclude]
        private Dictionary<string, TranslateAPIConfig> Configs
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
            get => Configs[apiName];
        }

        public Setting()
        {
            apiName = "OpenAI";
            sourceLanguage = "";
            targetLanguage = "";
            configs = new Dictionary<string, TranslateAPIConfig>
            {
                { "OpenAI", new OpenAIConfig() }
            };
        }

        public Setting(string apiName, string sourceLanguage, string targetLanguage, 
            Dictionary<string, TranslateAPIConfig> configs)
        {
            this.apiName = apiName;
            this.sourceLanguage = sourceLanguage;
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
                setting = new Setting();
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
