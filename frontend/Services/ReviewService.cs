using System.Net.Http.Json;
using Frontend.Models.Review;
using System.Net.Http.Headers;
using Frontend.ViewModels;

namespace Frontend.Services
{
    public class ReviewService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthViewModel _authViewModel;

        public ReviewService(IHttpClientFactory httpClientFactory, AuthViewModel authViewModel)
        {
            _httpClient = httpClientFactory.CreateClient("API");
            _authViewModel = authViewModel;
        }

        private async Task SetAuthorizationHeader()
        {
            var token = await _authViewModel.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<List<ReviewDto>> GetProductReviewsAsync(int productId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<ReviewDto>>($"/api/reviews/{productId}");
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
                await SetAuthorizationHeader();
                var response = await _httpClient.PostAsJsonAsync("/api/reviews", request);
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
                await SetAuthorizationHeader();
                var response = await _httpClient.PostAsJsonAsync($"/api/reviews/{reviewId}/comments", request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
