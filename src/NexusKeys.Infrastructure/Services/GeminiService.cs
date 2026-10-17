using System.Text;
using System.Text.Json;

namespace GameStore.API.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(HttpClient httpClient, IConfiguration config, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<string?> GenerateReplyAsync(
            string systemPrompt, 
            string userMessage, 
            string? customApiKey = null, 
            string? customModel = null,
            double? temperature = null,
            int? maxTokens = null)
        {
            var apiKey = !string.IsNullOrWhiteSpace(customApiKey) 
                ? customApiKey 
                : _config["AiAssistant:Gemini:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GEMINI_API_KEY_HERE")
            {
                _logger.LogWarning("Gemini API Key is not configured or empty.");
                return null;
            }

            var configuredModel = !string.IsNullOrWhiteSpace(customModel)
                ? customModel
                : (_config["AiAssistant:Gemini:Model"] ?? "gemini-3.6-flash");

            var model = NormalizeModelName(configuredModel);
            var temp = temperature ?? 0.7;
            var tokens = maxTokens ?? 2048;

            // Attempt primary model, fallback to gemini-3.6-flash if needed
            var reply = await CallGeminiApiAsync(model, apiKey, systemPrompt, userMessage, temp, tokens);
            if (reply == null && model != "gemini-3.6-flash")
            {
                _logger.LogInformation("Retrying Gemini with default model gemini-3.6-flash...");
                reply = await CallGeminiApiAsync("gemini-3.6-flash", apiKey, systemPrompt, userMessage, temp, tokens);
            }

            return reply;
        }

        private async Task<string?> CallGeminiApiAsync(
            string model, 
            string apiKey, 
            string systemPrompt, 
            string userMessage,
            double temperature = 0.7,
            int maxTokens = 2048)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var requestBody = new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                        new { text = systemPrompt }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = userMessage }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = temperature,
                    maxOutputTokens = maxTokens
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                using var response = await _httpClient.PostAsync(url, jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API ({Model}) failed with status {StatusCode}: {Error}", model, response.StatusCode, responseContent);
                    return null;
                }

                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        // Look for the last non-empty part text
                        for (int i = parts.GetArrayLength() - 1; i >= 0; i--)
                        {
                            if (parts[i].TryGetProperty("text", out var textProp))
                            {
                                var text = textProp.GetString();
                                if (!string.IsNullOrWhiteSpace(text))
                                    return text;
                            }
                        }
                    }
                }

                _logger.LogWarning("Gemini API returned unexpected response structure: {Response}", responseContent);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while calling Google Gemini API.");
                return null;
            }
        }

        private static string NormalizeModelName(string model)
        {
            var trimmed = model.Trim().ToLowerInvariant();
            // Handle common variants like "Gemini 2 Flash", "gemini 2.0 flash", "gemini-1.5-flash", "gemini-2.5-flash"
            if (trimmed.Contains("flash"))
            {
                if (trimmed.Contains("3.7")) return "gemini-3.7-flash";
                if (trimmed.Contains("3.6")) return "gemini-3.6-flash";
                if (trimmed.Contains("3.5")) return "gemini-3.5-flash";
                if (trimmed.Contains("3.1")) return "gemini-3.1-flash-lite";
                return "gemini-3.6-flash";
            }
            if (trimmed.Contains("pro"))
            {
                if (trimmed.Contains("3.1")) return "gemini-3.1-pro-preview";
                return "gemini-2.5-pro";
            }
            return trimmed.Replace(" ", "-");
        }
    }
}
