using System.Net.Http.Json;
using Frontend.Models.Assistant;
using Frontend.ViewModels;

namespace Frontend.Services
{
    public class AssistantService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthViewModel _authViewModel;

        public AssistantService(IHttpClientFactory httpClientFactory, AuthViewModel authViewModel)
        {
            _httpClient = httpClientFactory.CreateClient("API");
            _authViewModel = authViewModel;
        }

        public async Task<string> SendMessageAsync(string message)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/chat");
            request.Content = JsonContent.Create(new ChatRequest { Message = message });

            var token = await _authViewModel.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
                    return result?.Reply ?? "Xin lỗi, tôi không thể trả lời lúc này.";
                }
                return "Hệ thống AI hiện đang bận hoặc không phản hồi. Vui lòng thử lại sau.";
            }
            catch
            {
                return "Không thể kết nối đến máy chủ AI. Vui lòng kiểm tra lại kết nối.";
            }
        }
    }
}
