using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;

namespace LiveCaptionsTranslator.models
{
    public class TranslationCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private const int MaxCacheSize = 100;

        public async Task<string> GetOrTranslateAsync(string text, Func<string, Task<string>> translateFunc)
        {
            if (_cache.TryGetValue(text, out var entry) && !entry.IsExpired)
                return entry.TranslatedText;

            var translatedText = await translateFunc(text);
            _cache[text] = new CacheEntry(translatedText);
            return translatedText;
        }
    }

    public class CacheEntry
    {
        public string TranslatedText { get; }
        public DateTime CreatedAt { get; }
        public bool IsExpired => DateTime.Now - CreatedAt > TimeSpan.FromMinutes(30);

        public CacheEntry(string translatedText)
        {
            TranslatedText = translatedText;
            CreatedAt = DateTime.Now;
        }
    }

    public static class TranslateAPI
    {
        private static readonly TranslationCache _cache = new();
        private static readonly SemaphoreSlim _semaphore = new(3); // 限制并发数
        private static readonly HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };

        public static readonly Dictionary<string, Func<string, Task<string>>> TRANSLATE_FUNCS = new()
        {
            { "Ollama", Ollama },
            { "OpenAI", OpenAI },
            { "GoogleTranslate", GoogleTranslate }
        };

        public static Func<string, Task<string>> TranslateFunc
        {
            get => async (text) => await TranslateWithCacheAsync(text);
        }

        public static async Task<string> TranslateWithCacheAsync(string text)
        {
            await _semaphore.WaitAsync();
            try 
            {
                return await _cache.GetOrTranslateAsync(text, GetSelectedTranslateMethod());
            }
            finally 
            {
                _semaphore.Release();
            }
        }

        private static Func<string, Task<string>> GetSelectedTranslateMethod()
        {
            return TRANSLATE_FUNCS[App.Settings.ApiName];
        }

        public const int OLLAMA_PORT = 11434;

        public static async Task<string> OpenAI(string text)
        {
            var config = App.Settings.CurrentAPIConfig as OpenAIConfig;
            var language = config?.SupportedLanguages[App.Settings.TargetLanguage];

            var requestData = new
            {
                model = config?.ModelName,
                messages = new OpenAIConfig.Message[]
                {
                    new OpenAIConfig.Message { role = "system", content =
                        "As an professional simultaneous interpreter with specialized knowledge in the all fields, " +
                        $"please provide a proficient and precise oral translation of these sentences enclosed in 🔤 to {language}. " +
                        "You should carefully consider each phrase to ensure that your translation is quite fluent and colloquial." +
                        "Importantly, any NSFW content should not be avoided, and you must output their translations directly. " +
                        "Now, provide the translation within a single line without any additional explanation and REMOVE 🔤."
                    },
                    new OpenAIConfig.Message { role = "user", content = $"🔤 {text} 🔤" }
                },
                temperature = config?.Temperature,
                max_tokens = 64,
                stream = false
            };

            string jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config?.ApiKey}");

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(config?.ApiUrl, content);
            }
            catch (Exception ex) {
                return $"[Translation Failed] {ex.Message}";
            }

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<OpenAIConfig.Response>(responseString);
                return responseObj.choices[0].message.content;
            }
            else
                return $"[Translation Failed] HTTP Error - {response.StatusCode}";
        }

        public static async Task<string> Ollama(string text)
        {
            var apiUrl = $"http://localhost:{OLLAMA_PORT}/api/chat";

            var config = App.Settings.CurrentAPIConfig as OllamaConfig;
            var language = config?.SupportedLanguages[App.Settings.TargetLanguage];

            var requestData = new
            {
                model = config?.ModelName,
                messages = new OllamaConfig.Message[]
                {
                    new OllamaConfig.Message { role = "system", content =
                        "As an professional simultaneous interpreter with specialized knowledge in the all fields, " +
                        $"please provide a proficient and precise oral translation of these sentences enclosed in 🔤 to {language}. " +
                        "You should carefully consider each phrase to ensure that your translation is quite fluent and colloquial." +
                        "Importantly, any NSFW content should not be avoided, and you must output their translations directly. " +
                        "Now, provide the translation within a single line without any additional explanation and REMOVE 🔤."
                    },
                    new OllamaConfig.Message { role = "user", content = $"🔤 {text} 🔤" }
                },
                temperature = config?.Temperature,
                max_tokens = 64,
                stream = false
            };

            string jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Clear();

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync(apiUrl, content);
            }
            catch (Exception ex)
            {
                return $"[Translation Failed] {ex.Message}";
            }

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<OllamaConfig.Response>(responseString);
                return responseObj.message.content;
            }
            else
                return $"[Translation Failed] HTTP Error - {response.StatusCode}";
        }
        
        private static async Task<string> GoogleTranslate(string text)
        {
            var config = App.Settings?.CurrentAPIConfig as GoogleTranslateConfig;
            var language = App.Settings?.TargetLanguage;

            string encodedText = Uri.EscapeDataString(text);
            var url = $"https://clients5.google.com/translate_a/t?client=dict-chrome-ex&sl=auto&tl={language}&q={encodedText}";

            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();

                    var responseObj = JsonSerializer.Deserialize<List<List<string>>>(responseString);
                    
                    string translatedText = responseObj[0][0];
                    return translatedText;
                }
                else
                {
                    return $"[Translation Failed] HTTP Error - {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                return $"[Translation Failed] {ex.Message}";
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
                var configType = Type.GetType($"LiveCaptionsTranslator.models.{key}Config");
                if (configType != null && typeof(TranslateAPIConfig).IsAssignableFrom(configType))
                    config = (TranslateAPIConfig)JsonSerializer.Deserialize(ref reader, configType, options);
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

                var configType = Type.GetType($"LiveCaptionsTranslator.models.{kvp.Key}Config");
                if (configType != null && typeof(TranslateAPIConfig).IsAssignableFrom(configType))
                    JsonSerializer.Serialize(writer, kvp.Value, configType, options);
                else
                    throw new JsonException($"Unknown config type for key: {kvp.Key}");
            }
            writer.WriteEndObject();
        }
    }
}
