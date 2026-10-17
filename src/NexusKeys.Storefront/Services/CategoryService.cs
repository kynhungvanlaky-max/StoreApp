using System.Net.Http.Json;
using Frontend.Models.Product;

namespace Frontend.Services
{
    public class CategoryService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(IHttpClientFactory httpClientFactory, ILogger<CategoryService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("API");
            _logger = logger;
        }

        public async Task<List<CategoryItemDto>> GetCategoriesAsync()
        {
            try
            {
                var categories = await _httpClient.GetFromJsonAsync<List<CategoryItemDto>>($"{ApiRoutes.V1}/categories");
                if (categories != null && categories.Any())
                {
                    return categories;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể tải danh sách thể loại từ API /categories");
            }

            // Fallback categories
            return new List<CategoryItemDto>
            {
                new() { Name = "Hành động", ProductCount = 0 },
                new() { Name = "Nhập vai (RPG)", ProductCount = 0 },
                new() { Name = "Phiêu lưu", ProductCount = 0 },
                new() { Name = "Thế giới mở", ProductCount = 0 },
                new() { Name = "Chiến thuật", ProductCount = 0 },
                new() { Name = "Bắn súng", ProductCount = 0 },
                new() { Name = "Thể thao", ProductCount = 0 },
                new() { Name = "Mô phỏng", ProductCount = 0 },
                new() { Name = "Sinh tồn", ProductCount = 0 },
                new() { Name = "Kinh dị", ProductCount = 0 },
                new() { Name = "Đua xe", ProductCount = 0 },
                new() { Name = "Phần mềm", ProductCount = 0 }
            };
        }
    }
}
