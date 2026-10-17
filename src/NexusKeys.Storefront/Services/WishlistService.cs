using System.Net.Http.Json;
using Frontend.Models.Wishlist;
using Frontend.ViewModels;

namespace Frontend.Services
{
    public class WishlistService
    {
        private readonly AuthorizedApiClient _apiClient;

        public WishlistService(AuthorizedApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<WishlistItem>> GetWishlistAsync()
        {
            try
            {
                using var response = await _apiClient.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, $"{ApiRoutes.V1}/users/me/wishlist-items"));
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
                using var response = await _apiClient.SendAsync(() => new HttpRequestMessage(HttpMethod.Put, $"{ApiRoutes.V1}/users/me/wishlist-items/{productId}")
                {
                    Content = new StringContent(string.Empty)
                });
                return response.IsSuccessStatusCode;
            }
            catch { }
            return false;
        }

        public async Task<bool> RemoveFromWishlistAsync(int productId)
        {
            try
            {
                using var response = await _apiClient.SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, $"{ApiRoutes.V1}/users/me/wishlist-items/{productId}"));
                return response.IsSuccessStatusCode;
            }
            catch { }
            return false;
        }

        public async Task<bool> CheckWishlistAsync(int productId)
        {
            try
            {
                using var response = await _apiClient.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, $"{ApiRoutes.V1}/users/me/wishlist-items/{productId}"));
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
