using System.Net.Http.Json;
using Frontend.Models.Assistant;
using Frontend.ViewModels;

namespace Frontend.Services
{
    public class AssistantService
    {
        private readonly AuthorizedApiClient _apiClient;

        public AssistantService(AuthorizedApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<string> SendMessageAsync(string message)
        {
            try
            {
                using var response = await _apiClient.SendAsync(() => new HttpRequestMessage(HttpMethod.Post, $"{ApiRoutes.V1}/assistant/chat")
                {
                    Content = JsonContent.Create(new ChatRequest { Message = message })
                });
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
