using System.Net.Http.Json;
using Frontend.Models.Review;
using System.Net.Http.Headers;
using Frontend.ViewModels;

namespace Frontend.Services
{
    public class ReviewService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthorizedApiClient _apiClient;

        public ReviewService(IHttpClientFactory httpClientFactory, AuthorizedApiClient apiClient)
        {
            _httpClient = httpClientFactory.CreateClient("API");
            _apiClient = apiClient;
        }

        public async Task<List<ReviewDto>> GetProductReviewsAsync(int productId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ReviewDto>>($"{ApiRoutes.V1}/products/{productId}/reviews");
                return response ?? new List<ReviewDto>();
            }
            catch
            {
                return new List<ReviewDto>();
            }
        }

        public async Task<bool> CreateReviewAsync(CreateReviewDto request)
        {
            try
            {
                using var response = await _apiClient.SendAsync(() => new HttpRequestMessage(HttpMethod.Post, $"{ApiRoutes.V1}/products/{request.ProductId}/reviews")
                {
                    Content = JsonContent.Create(request)
                });
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ReplyToReviewAsync(int reviewId, ReplyReviewDto request)
        {
            try
            {
                using var response = await _apiClient.SendAsync(() => new HttpRequestMessage(HttpMethod.Post, $"{ApiRoutes.V1}/reviews/{reviewId}/comments")
                {
                    Content = JsonContent.Create(request)
                });
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
