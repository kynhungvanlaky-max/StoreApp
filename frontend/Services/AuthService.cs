using System.Net.Http.Json;
using Frontend.Models.Auth;

namespace Frontend.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        public AuthService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("API");
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AuthResponse>();
            }
            return null;
        }

        public async Task<AuthResponse?> ExternalLoginAsync(ExternalLoginRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/external-login", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AuthResponse>();
            }
            return null;
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<HttpResponseMessage> ChangePasswordAsync(ChangePasswordRequest request)
        {
            return await _httpClient.PostAsJsonAsync("/api/auth/change-password", request);
        }

        public async Task<HttpResponseMessage> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            return await _httpClient.PostAsJsonAsync("/api/auth/forgot-password", request);
        }

        public async Task<HttpResponseMessage> ResetPasswordAsync(ResetPasswordRequest request)
        {
            return await _httpClient.PostAsJsonAsync("/api/auth/reset-password", request);
        }
    }
}
