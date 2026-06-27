using System.Net.Http.Json;
using Frontend.Models.Wishlist;
using Frontend.ViewModels;

namespace Frontend.Services
{
    public class WishlistService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AuthViewModel _authVM;

        public WishlistService(IHttpClientFactory httpClientFactory, AuthViewModel authVM)
        {
            _httpClientFactory = httpClientFactory;
            _authVM = authVM;
        }

        private async Task<HttpClient> CreateClientAsync()
        {
            var client = _httpClientFactory.CreateClient("API");
            var token = await _authVM.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        public async Task<List<WishlistItem>> GetWishlistAsync()
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync("/api/wishlist");
                if (response.IsSuccessStatusCode)
                {
                    var items = await response.Content.ReadFromJsonAsync<List<WishlistItem>>();
                    return items ?? new List<WishlistItem>();
                }
            }
            catch { }
            return new List<WishlistItem>();
        }

        public async Task<bool> AddToWishlistAsync(int productId)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.PostAsync($"/api/wishlist/{productId}", new StringContent(""));
                return response.IsSuccessStatusCode;
            }
            catch { }
            return false;
        }

        public async Task<bool> RemoveFromWishlistAsync(int productId)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.DeleteAsync($"/api/wishlist/{productId}");
                return response.IsSuccessStatusCode;
            }
            catch { }
            return false;
        }

        public async Task<bool> CheckWishlistAsync(int productId)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync($"/api/wishlist/check/{productId}");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CheckWishlistResponse>();
                    return result?.IsWishlisted ?? false;
                }
            }
            catch { }
            return false;
        }
    }
}
