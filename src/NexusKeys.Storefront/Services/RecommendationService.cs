using System.Net.Http.Json;
using Frontend.Models.Product;

namespace Frontend.Services
{
    public class RecommendationService
    {
        private readonly HttpClient _httpClient;

        public RecommendationService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("API");
        }

        public async Task<List<ProductDto>> GetRecommendationsAsync(int? categoryId = null, int? excludeProductId = null, int count = 6)
        {
            try
            {
                var query = $"?count={count}";
                if (categoryId.HasValue) query += $"&categoryId={categoryId.Value}";
                if (excludeProductId.HasValue) query += $"&excludeProductId={excludeProductId.Value}";
                
                return await _httpClient.GetFromJsonAsync<List<ProductDto>>($"{ApiRoutes.V1}/recommendations{query}") ?? new();
            }
            catch
            {
                return new List<ProductDto>();
            }
        }

        public async Task<List<ProductDto>> GetTrendingAsync(int count = 6)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ProductDto>>($"{ApiRoutes.V1}/recommendations/trending?count={count}") ?? new();
            }
            catch
            {
                return new List<ProductDto>();
            }
        }
    }
}
