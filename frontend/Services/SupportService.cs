using System.Net.Http.Json;

namespace Frontend.Services
{
    public class SupportService
    {
        private readonly HttpClient _httpClient;

        public SupportService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("API");
        }

        public async Task<List<SupportTicketDto>> GetMyTicketsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/support/my-tickets");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<SupportTicketDto>>() ?? new List<SupportTicketDto>();
                }
            }
            catch { }
            return new List<SupportTicketDto>();
        }

        public async Task<bool> CreateTicketAsync(CreateSupportTicketDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/support", request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    public class SupportTicketDto
    {
        public int Id { get; set; }
        public string Topic { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? OrderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateSupportTicketDto
    {
        public string Topic { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? OrderId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
