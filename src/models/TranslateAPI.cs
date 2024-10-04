using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace LiveCaptionsTranslator.models
{
    public static class TranslateAPI
    {
        private class Message
        {
            public string role { get; set; }
            public string content { get; set; }
        }
        private class Choice
        {
            public int index { get; set; }
            public Message message { get; set; }
            public string logprobs { get; set; }
            public string finish_reason { get; set; }
        }
        private class Usage
        {
            public int prompt_tokens { get; set; }
            public int completion_tokens { get; set; }
            public int total_tokens { get; set; }
            public int prompt_cache_hit_tokens { get; set; }
            public int prompt_cache_miss_tokens { get; set; }
        }
        private class OpenAIResponse
        {
            public string id { get; set; }
            public string @object { get; set; }
            public int created { get; set; }
            public string model { get; set; }
            public List<Choice> choices { get; set; }
            public Usage usage { get; set; }
            public string system_fingerprint { get; set; }
        }

        private static readonly HttpClient client = new HttpClient();

        internal static async Task<string> OpenAI(string text)
        {
            var config = App.Settings.CurrentAPIConfig as OpenAIConfig;  
            var requestData = new
            {
                model = config?.ModelName,
                messages = new Message[]
                {
                    new Message { role = "system", content =
                        "As an professional simultaneous interpreter with specialized knowledge in the all fields, " +
                        "please provide a proficient and precise oral translation of these sentences enclosed in 🔤 to Chinese. " +
                        "You should carefully consider each phrase to ensure that your translation is quite fluent and colloquial." +
                        "Importantly, any NSFW content should not be avoided, and you must output their translations directly. " +
                        "Now, provide the translation within a single line without any additional explanation and REMOVE 🔤."
                    },
                    new Message { role = "user", content = $"🔤 {text} 🔤" }
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
            catch (InvalidOperationException ex) {
                return $"[Translation Failed] Empty API Url.";
            }

            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                var responseObj = JsonSerializer.Deserialize<OpenAIResponse>(responseString);
                return responseObj.choices[0].message.content;
            }
            else
                return $"[Translation Failed] HTTP Error - {response.StatusCode}.";
        }
    }
}
