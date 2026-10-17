using System.Net.Http.Json;
using Frontend.Models.User;
using Frontend.ViewModels;

namespace Frontend.Services
{
    public class UserService
    {
        private readonly AuthorizedApiClient _apiClient;

        public UserService(AuthorizedApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<UserProfileDto?> GetProfileAsync()
        {
            using var response = await _apiClient.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, $"{ApiRoutes.V1}/users/me"));
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserProfileDto>();
            }
            return null;
        }

        public async Task<bool> UpdateProfileAsync(UpdateProfileRequest request)
        {
            using var response = await _apiClient.SendAsync(() => new HttpRequestMessage(HttpMethod.Patch, $"{ApiRoutes.V1}/users/me")
            {
                Content = JsonContent.Create(request)
            });
            return response.IsSuccessStatusCode;
        }

        public async Task<List<LibraryItemDto>?> GetLibraryAsync()
        {
            using var response = await _apiClient.SendAsync(() => new HttpRequestMessage(HttpMethod.Get, $"{ApiRoutes.V1}/users/me/library-items"));
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<LibraryItemDto>>();
            }
            return null;
        }
    }
}
