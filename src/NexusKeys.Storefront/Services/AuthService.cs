using System.Net.Http.Json;
using System.Text.Json;
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
            var result = await LoginWithResultAsync(request);
            return result.Success ? result.Data : null;
        }

        public async Task<AuthResult> LoginWithResultAsync(LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{ApiRoutes.V1}/auth/login", request);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    return new AuthResult { Success = true, Data = data };
                }

                return await ExtractErrorAsync(response, "Email hoặc mật khẩu không chính xác.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTH ERROR] LoginAsync error: {ex.Message}");
                return new AuthResult { Success = false, ErrorMessage = "Không thể kết nối đến máy chủ. Vui lòng thử lại sau." };
            }
        }

        public async Task<AuthResponse?> ExternalLoginAsync(ExternalLoginRequest request)
        {
            var result = await ExternalLoginWithResultAsync(request);
            return result.Success ? result.Data : null;
        }

        public async Task<AuthResult> ExternalLoginWithResultAsync(ExternalLoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{ApiRoutes.V1}/auth/external-login", request);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    return new AuthResult { Success = true, Data = data };
                }

                return await ExtractErrorAsync(response, "Đăng nhập với tài khoản bên ngoài thất bại.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTH ERROR] ExternalLoginAsync error: {ex.Message}");
                return new AuthResult { Success = false, ErrorMessage = "Không thể kết nối đến máy chủ. Vui lòng thử lại sau." };
            }
        }

        private static async Task<AuthResult> ExtractErrorAsync(HttpResponseMessage response, string defaultMessage)
        {
            try
            {
                var jsonDoc = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (jsonDoc.ValueKind == JsonValueKind.Object)
                {
                    string? msg = null;
                    if (jsonDoc.TryGetProperty("detail", out var detailProp) && !string.IsNullOrWhiteSpace(detailProp.GetString()))
                    {
                        msg = detailProp.GetString();
                    }
                    else if (jsonDoc.TryGetProperty("message", out var msgProp) && !string.IsNullOrWhiteSpace(msgProp.GetString()))
                    {
                        msg = msgProp.GetString();
                    }
                    else if (jsonDoc.TryGetProperty("title", out var titleProp) && !string.IsNullOrWhiteSpace(titleProp.GetString()))
                    {
                        msg = titleProp.GetString();
                    }

                    bool isLocked = false;
                    if (jsonDoc.TryGetProperty("isLocked", out var lockedProp) && lockedProp.ValueKind == JsonValueKind.True)
                    {
                        isLocked = true;
                    }
                    else if (!string.IsNullOrEmpty(msg) && (msg.Contains("khoá", StringComparison.OrdinalIgnoreCase) || msg.Contains("khóa", StringComparison.OrdinalIgnoreCase)))
                    {
                        isLocked = true;
                    }

                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        return new AuthResult { Success = false, ErrorMessage = msg, IsLocked = isLocked };
                    }
                }
                else if (jsonDoc.ValueKind == JsonValueKind.String)
                {
                    var str = jsonDoc.GetString();
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        bool isLocked = str.Contains("khoá", StringComparison.OrdinalIgnoreCase) || str.Contains("khóa", StringComparison.OrdinalIgnoreCase);
                        return new AuthResult { Success = false, ErrorMessage = str, IsLocked = isLocked };
                    }
                }
            }
            catch
            {
                try
                {
                    var raw = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        var clean = raw.Trim('"', ' ', '\r', '\n');
                        bool isLocked = clean.Contains("khoá", StringComparison.OrdinalIgnoreCase) || clean.Contains("khóa", StringComparison.OrdinalIgnoreCase);
                        return new AuthResult { Success = false, ErrorMessage = clean, IsLocked = isLocked };
                    }
                }
                catch { }
            }

            return new AuthResult { Success = false, ErrorMessage = defaultMessage };
        }

        public async Task<AuthResponse?> RefreshAsync(string refreshToken)
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiRoutes.V1}/auth/refresh", new RefreshRequest
            {
                RefreshToken = refreshToken
            });

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AuthResponse>();
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync($"{ApiRoutes.V1}/auth/register", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<HttpResponseMessage> ChangePasswordAsync(ChangePasswordRequest request)
        {
            return await _httpClient.PostAsJsonAsync($"{ApiRoutes.V1}/users/me/password-changes", new
            {
                oldPassword = request.CurrentPassword,
                newPassword = request.NewPassword
            });
        }

        public async Task<HttpResponseMessage> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            return await _httpClient.PostAsJsonAsync($"{ApiRoutes.V1}/auth/forgot-password", request);
        }

        public async Task<HttpResponseMessage> ResetPasswordAsync(ResetPasswordRequest request)
        {
            return await _httpClient.PostAsJsonAsync($"{ApiRoutes.V1}/auth/reset-password", request);
        }
    }
}
