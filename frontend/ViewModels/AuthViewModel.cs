using Frontend.Models.Auth;
using Frontend.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Frontend.ViewModels
{
    public class AuthViewModel
    {
        private readonly AuthService _authService;
        private readonly ProtectedLocalStorage _storage;
        
        public bool IsLoggedIn { get; private set; }
        public string? Username { get; private set; }
        public string? Role { get; private set; }
        
        public event Action? StateChanged;
        
        public AuthViewModel(AuthService authService, ProtectedLocalStorage storage)
        {
            _authService = authService;
            _storage = storage;
        }
        
        public async Task InitializeAsync()
        {
            try
            {
                var result = await _storage.GetAsync<string>("username");
                if (result.Success && !string.IsNullOrEmpty(result.Value))
                {
                    Username = result.Value;
                    IsLoggedIn = true;
                    var roleResult = await _storage.GetAsync<string>("role");
                    Role = roleResult.Success ? roleResult.Value : "Customer";
                    StateChanged?.Invoke();
                }
            }
            catch
            {
                // Ignoring errors if local storage is unavailable or corrupted
            }
        }
        
        public async Task<bool> LoginAsync(LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            if (response == null) return false;
            
            await _storage.SetAsync("jwt_token", response.Token);
            await _storage.SetAsync("refresh_token", response.RefreshToken);
            await _storage.SetAsync("username", response.Username);
            await _storage.SetAsync("role", response.Role);
            
            IsLoggedIn = true;
            Username = response.Username;
            Role = response.Role;
            StateChanged?.Invoke();
            return true;
        }

        public async Task<bool> HandleExternalLoginAsync(string provider, string accessToken)
        {
            var request = new ExternalLoginRequest
            {
                Provider = provider,
                AccessToken = accessToken
            };

            var response = await _authService.ExternalLoginAsync(request);
            if (response == null) return false;

            await _storage.SetAsync("jwt_token", response.Token);
            await _storage.SetAsync("refresh_token", response.RefreshToken);
            await _storage.SetAsync("username", response.Username);
            await _storage.SetAsync("role", response.Role);

            IsLoggedIn = true;
            Username = response.Username;
            Role = response.Role;
            StateChanged?.Invoke();
            return true;
        }
        
        public async Task<string?> GetTokenAsync()
        {
            try
            {
                var result = await _storage.GetAsync<string>("jwt_token");
                return result.Success ? result.Value : null;
            }
            catch
            {
                return null;
            }
        }
        
        public async Task LogoutAsync()
        {
            await _storage.DeleteAsync("jwt_token");
            await _storage.DeleteAsync("refresh_token");
            await _storage.DeleteAsync("username");
            await _storage.DeleteAsync("role");
            IsLoggedIn = false;
            Username = null;
            Role = null;
            StateChanged?.Invoke();
        }

        public async Task<HttpResponseMessage> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            return await _authService.ForgotPasswordAsync(request);
        }

        public async Task<HttpResponseMessage> ResetPasswordAsync(ResetPasswordRequest request)
        {
            return await _authService.ResetPasswordAsync(request);
        }
    }
}
