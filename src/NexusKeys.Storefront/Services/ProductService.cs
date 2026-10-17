using System.Net.Http.Json;
using Frontend.Models.Product;

namespace Frontend.Services
{
    public class ProductService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductService> _logger;
        public string? LastError { get; private set; }

        public ProductService(IHttpClientFactory httpClientFactory, ILogger<ProductService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("API");
            _logger = logger;
        }

        public async Task<PaginatedList<ProductDto>?> GetProductsAsync(int page = 1, int pageSize = 20, string? search = null, List<string>? categories = null, List<string>? priceRanges = null, string? sortBy = null, string? type = null)
        {
            var query = $"?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(search)) query += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrEmpty(type)) query += $"&type={Uri.EscapeDataString(type)}";
            
            if (categories != null && categories.Any())
            {
                foreach (var cat in categories)
                {
                    query += $"&categories={Uri.EscapeDataString(cat)}";
                }
            }

            if (priceRanges != null && priceRanges.Any())
            {
                foreach (var pr in priceRanges)
                {
                    query += $"&priceRanges={Uri.EscapeDataString(pr)}";
                }
            }

            if (!string.IsNullOrEmpty(sortBy))
            {
                query += $"&sortBy={Uri.EscapeDataString(sortBy)}";
            }

            try
            {
                LastError = null;
                return await _httpClient.GetFromJsonAsync<PaginatedList<ProductDto>>($"{ApiRoutes.V1}/products{query}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể tải danh sách sản phẩm từ API");
                LastError = "Không thể tải sản phẩm. Vui lòng kiểm tra kết nối API và thử lại.";
                return new PaginatedList<ProductDto>();
            }
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            try
            {
                LastError = null;
                return await _httpClient.GetFromJsonAsync<ProductDto>($"{ApiRoutes.V1}/products/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể tải sản phẩm {ProductId} từ API", id);
                LastError = "Không thể tải thông tin sản phẩm.";
                return null;
            }
        }
    }
}
