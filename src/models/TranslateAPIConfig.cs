using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace LiveCaptionsTranslator.models
{
    public class TranslateAPIConfig : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

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

        public void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            App.Settings?.Save();
        }
    }

    public class OpenAIConfig : TranslateAPIConfig
    {
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
                OnPropertyChanged("temperature");
            }
        }
    }

    public class ConfigDictConverter : JsonConverter<Dictionary<string, TranslateAPIConfig>>
    {
        public override Dictionary<string, TranslateAPIConfig> Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var configs = new Dictionary<string, TranslateAPIConfig>();
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected a StartObject token.");

            reader.Read();
            while (reader.TokenType == JsonTokenType.PropertyName)
            {
                string key = reader.GetString();
                reader.Read();

                TranslateAPIConfig config;
                if (key == "OpenAI")
                    config = JsonSerializer.Deserialize<OpenAIConfig>(ref reader, options);
                else
                    throw new JsonException($"Unknown config type for key: {key}");

                configs[key] = config;
                reader.Read();
            }

            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException("Expected an EndObject token.");
            return configs;
        }

        public override void Write(
            Utf8JsonWriter writer, Dictionary<string, TranslateAPIConfig> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);

                if (kvp.Value is OpenAIConfig openAIConfig)
                    JsonSerializer.Serialize(writer, openAIConfig, options);
                else
                    throw new JsonException($"Unknown config type for key: {kvp.Key}");
            }
            writer.WriteEndObject();
        }
    }
}
