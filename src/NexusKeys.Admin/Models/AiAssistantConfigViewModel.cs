namespace Backend.Models
{
    public class AiAssistantConfigViewModel
    {
        public bool IsEnabled { get; set; } = true;
        public string Provider { get; set; } = "Auto"; // "Auto", "Gemini", "Ollama", "Disabled"
        public string GeminiApiKey { get; set; } = string.Empty;
        public string GeminiModel { get; set; } = "gemini-3.6-flash";
        public double GeminiTemperature { get; set; } = 0.7;
        public int GeminiMaxTokens { get; set; } = 2048;
        public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
        public string OllamaModel { get; set; } = "qwen2.5:0.5b";
        public string SystemPrompt { get; set; } = string.Empty;
        public bool IncludeUserInfo { get; set; } = true;
        public bool IncludeHotGames { get; set; } = true;
    }

    public class AiAssistantTestResponseViewModel
    {
        public bool Success { get; set; }
        public string Reply { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public long LatencyMs { get; set; }
    }
}
