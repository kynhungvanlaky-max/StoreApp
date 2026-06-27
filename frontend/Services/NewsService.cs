using Frontend.Models.News;

namespace Frontend.Services
{
    public class NewsService
    {
        private readonly HttpClient _httpClient;

        public NewsService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("API");
        }

        public async Task<Frontend.Models.Product.PaginatedList<NewsArticleDto>> GetNewsAsync(int page = 1, int pageSize = 10)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<Frontend.Models.Product.PaginatedList<NewsArticleDto>>($"/api/news?page={page}&pageSize={pageSize}") 
                       ?? new Frontend.Models.Product.PaginatedList<NewsArticleDto>();
            }
            catch
            {
                return new Frontend.Models.Product.PaginatedList<NewsArticleDto>();
            }
        }

        public async Task<NewsArticleDto?> GetNewsByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<NewsArticleDto>($"/api/news/{id}");
            }
            catch
            {
                return null;
            }
        }
    }
}
