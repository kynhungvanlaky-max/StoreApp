using Microsoft.AspNetCore.Mvc;
using Backend.Models;
using Backend.Extensions;
using Backend.Attributes;

namespace Backend.Controllers
{
    [RequireAdminRole]
    public class AiAssistantConfigsController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AiAssistantConfigsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("API");
            var model = new AiAssistantConfigViewModel();

            try
            {
                var response = await client.GetAsync("/api/v1/assistant/configurations");
                if (response.IsSuccessStatusCode)
                {
                    var dict = await response.Content.ReadJsonAsync<Dictionary<string, string>>();
                    if (dict != null)
                    {
                        model.IsEnabled = !dict.TryGetValue("AiAssistant:IsEnabled", out var isEn) || !isEn.Equals("false", StringComparison.OrdinalIgnoreCase);
                        model.Provider = dict.GetValueOrDefault("AiAssistant:Provider", "Auto");
                        model.GeminiApiKey = dict.GetValueOrDefault("AiAssistant:Gemini:ApiKey", "");
                        model.GeminiModel = dict.GetValueOrDefault("AiAssistant:Gemini:Model", "gemini-3.6-flash");
                        
                        if (double.TryParse(dict.GetValueOrDefault("AiAssistant:Gemini:Temperature", "0.7"), out var temp))
                            model.GeminiTemperature = temp;

                        if (int.TryParse(dict.GetValueOrDefault("AiAssistant:Gemini:MaxTokens", "2048"), out var tokens))
                            model.GeminiMaxTokens = tokens;

                        model.OllamaBaseUrl = dict.GetValueOrDefault("AiAssistant:Ollama:BaseUrl", "http://localhost:11434");
                        model.OllamaModel = dict.GetValueOrDefault("AiAssistant:Ollama:Model", "qwen2.5:0.5b");
                        model.SystemPrompt = dict.GetValueOrDefault("AiAssistant:SystemPrompt", "");
                        model.IncludeUserInfo = !dict.TryGetValue("AiAssistant:IncludeUserInfo", out var incUser) || !incUser.Equals("false", StringComparison.OrdinalIgnoreCase);
                        model.IncludeHotGames = !dict.TryGetValue("AiAssistant:IncludeHotGames", out var incHot) || !incHot.Equals("false", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("API AiAssistantConfigs Error: " + ex.Message);
            }

            return View(model);
        }
    }
}
