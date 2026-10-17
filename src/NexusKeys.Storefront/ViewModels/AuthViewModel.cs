using Frontend.Models.Auth;
using Frontend.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Frontend.ViewModels
{
    public class AuthViewModel
    {
        private readonly AuthService _authService;
        private readonly ProtectedLocalStorage _storage;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private Task? _initializationTask;
        
        public bool IsLoggedIn { get; private set; }
        public string? Username { get; private set; }
        public string? Role { get; private set; }
        public bool IsInitialized { get; private set; }
        
        public event Action? StateChanged;
        
        public AuthViewModel(AuthService authService, ProtectedLocalStorage storage)
        {
            _authService = authService;
            _storage = storage;
        }
        
        public Task InitializeAsync()
        {
            return _initializationTask ??= InitializeCoreAsync();
        }

        private async Task InitializeCoreAsync()
        {
            try
            {
                var usernameResult = await _storage.GetAsync<string>("username");
                var tokenResult = await _storage.GetAsync<string>("jwt_token");
                if (usernameResult.Success && !string.IsNullOrEmpty(usernameResult.Value)
                    && tokenResult.Success && !string.IsNullOrEmpty(tokenResult.Value))
                {
                    Username = usernameResult.Value;
                    IsLoggedIn = true;
                    var roleResult = await _storage.GetAsync<string>("role");
                    Role = roleResult.Success ? roleResult.Value : "Customer";
                }
            }
            catch
            {
                // Ignoring errors if local storage is unavailable or corrupted
            }
            finally
            {
                IsInitialized = true;
                StateChanged?.Invoke();
            }
        }
        
        public async Task<bool> LoginAsync(LoginRequest request)
        {
            var result = await LoginWithResultAsync(request);
            return result.Success;
        }

        public async Task<AuthResult> LoginWithResultAsync(LoginRequest request)
        {
            var result = await _authService.LoginWithResultAsync(request);
            if (result.Success && result.Data != null)
            {
                await StoreSessionAsync(result.Data);
            }
            return result;
        }

        public async Task<bool> HandleExternalLoginAsync(string provider, string accessToken)
        {
            var result = await HandleExternalLoginWithResultAsync(provider, accessToken);
            return result.Success;
        }

        public async Task<AuthResult> HandleExternalLoginWithResultAsync(string provider, string accessToken)
        {
            var request = new ExternalLoginRequest
            {
                Provider = provider,
                AccessToken = accessToken
            };

            var result = await _authService.ExternalLoginWithResultAsync(request);
            if (result.Success && result.Data != null)
            {
                await StoreSessionAsync(result.Data);
            }
            return result;
        }

        public async Task<bool> TryRefreshAsync()
        {
            await _refreshLock.WaitAsync();
            try
            {
                var refreshResult = await _storage.GetAsync<string>("refresh_token");
                if (!refreshResult.Success || string.IsNullOrWhiteSpace(refreshResult.Value))
                {
                    await ClearSessionAsync();
                    return false;
                }

                var response = await _authService.RefreshAsync(refreshResult.Value);
                if (response == null)
                {
                    await ClearSessionAsync();
                    return false;
                }

                await StoreSessionAsync(response);
                return true;
            }
            catch
            {
                await ClearSessionAsync();
                return false;
            }
            finally
            {
                _refreshLock.Release();
            }
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
            await ClearSessionAsync();
        }

        private async Task StoreSessionAsync(AuthResponse response)
        {
            await _storage.SetAsync("jwt_token", response.Token);
            await _storage.SetAsync("refresh_token", response.RefreshToken);
            await _storage.SetAsync("username", response.Username);
            await _storage.SetAsync("role", response.Role);

            IsLoggedIn = true;
            Username = response.Username;
            Role = response.Role;
            IsInitialized = true;
            StateChanged?.Invoke();
        }

        private async Task ClearSessionAsync()
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
