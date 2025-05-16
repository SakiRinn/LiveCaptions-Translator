using System.Net.Http;
using System.Text.Json;

namespace LiveCaptionsTranslator.utils
{
    public static class UpdateUtil
    {
        public static async Task<string> GetLatestVersion()
        {
            string apiUrl = Constants.GitHubLatestReleaseApi;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("request");
            try
            {
                var response = await client.GetStringAsync(apiUrl);
                using var doc = JsonDocument.Parse(response);
                var latestVersionRaw = doc.RootElement.GetProperty("tag_name").GetString();
                var latestVersion = string.IsNullOrEmpty(latestVersionRaw)
                    ? ""
                    : System.Text.RegularExpressions.Regex.Replace(latestVersionRaw, "[^0-9.]", "");
                return latestVersion;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Network error: {ex.Message}");
                return string.Empty;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON parsing error: {ex.Message}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return string.Empty;

            }
        }
    }
}
