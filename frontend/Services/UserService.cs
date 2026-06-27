using System.Net.Http.Json;
using Frontend.Models.User;
using Frontend.ViewModels;

namespace Frontend.Services
{
    public class UserService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthViewModel _authViewModel;

        public UserService(IHttpClientFactory httpClientFactory, AuthViewModel authViewModel)
        {
            _httpClient = httpClientFactory.CreateClient("API");
            _authViewModel = authViewModel;
        }

        private async Task SetAuthorizationHeaderAsync()
        {
            var token = await _authViewModel.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<UserProfileDto?> GetProfileAsync()
        {
            await SetAuthorizationHeaderAsync();
            var response = await _httpClient.GetAsync("/api/users/profile");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserProfileDto>();
            }
            return null;
        }

        public async Task<bool> UpdateProfileAsync(UpdateProfileRequest request)
        {
            await SetAuthorizationHeaderAsync();
            var response = await _httpClient.PutAsJsonAsync("/api/users/profile", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<LibraryItemDto>?> GetLibraryAsync()
        {
            await SetAuthorizationHeaderAsync();
            var response = await _httpClient.GetAsync("/api/orders/library");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<LibraryItemDto>>();
            }
            return null;
        }
    }
}
