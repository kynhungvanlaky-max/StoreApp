using System.Net.Http.Json;
using Frontend.Models.Product;

namespace Frontend.Services
{
    public class ProductService
    {
        private readonly HttpClient _httpClient;

        public ProductService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("API");
        }

        public async Task<PaginatedList<ProductDto>?> GetProductsAsync(int page = 1, int pageSize = 20, string? search = null, List<string>? categories = null, List<string>? priceRanges = null, string? sortBy = null)
        {
            var query = $"?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(search)) query += $"&search={Uri.EscapeDataString(search)}";
            
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
                return await _httpClient.GetFromJsonAsync<PaginatedList<ProductDto>>($"/api/products{query}");
            }
            catch
            {
                return new PaginatedList<ProductDto>();
            }
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ProductDto>($"/api/products/{id}");
            }
            catch
            {
                return null;
            }
        }
    }
}
