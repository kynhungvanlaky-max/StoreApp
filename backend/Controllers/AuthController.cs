using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backend.Extensions;

namespace Backend.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration? _configuration;

        public AuthController(IHttpClientFactory httpClientFactory) : this(httpClientFactory, null)
        {
        }

        [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
        public AuthController(IHttpClientFactory httpClientFactory, IConfiguration? configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet("/Auth/Login")]
        public IActionResult Login([FromQuery] bool expired = false, [FromQuery] bool apiUnavailable = false)
        {
            var token = Request.Cookies["access_token"];
            if (!expired && !apiUnavailable && !string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index", "Home");
            }

            if (expired)
            {
                ViewBag.Error = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.";
            }
            else if (apiUnavailable)
            {
                ViewBag.Error = "Không thể kết nối đến máy chủ API. Vui lòng thử lại sau.";
            }

            return View();
        }

        [HttpPost("/Auth/Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginPost([FromForm] string username, [FromForm] string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tên đăng nhập/email và mật khẩu.";
                return View("Login");
            }

            try
            {
                var client = _httpClientFactory.CreateClient("API");
                var loginPayload = new { username = username.Trim(), email = username.Trim(), password = password };
                var jsonContent = new StringContent(JsonSerializer.Serialize(loginPayload), System.Text.Encoding.UTF8, "application/json");

                using var response = await client.PostAsync("api/auth/login", jsonContent, HttpContext.RequestAborted);
                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Tài khoản hoặc mật khẩu không chính xác.";
                    return View("Login");
                }

                var loginResult = await response.Content.ReadJsonAsync<LoginResponseDto>();
                if (string.IsNullOrWhiteSpace(loginResult?.Token))
                {
                    ViewBag.Error = "Đăng nhập không thành công. Dữ liệu xác thực không hợp lệ.";
                    return View("Login");
                }

                var token = loginResult.Token;

                // Validate user role and status
                using var profileReq = new HttpRequestMessage(HttpMethod.Get, "api/users/profile");
                profileReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var profileRes = await client.SendAsync(profileReq, HttpContext.RequestAborted);

                if (!profileRes.IsSuccessStatusCode)
                {
                    ViewBag.Error = "Không thể lấy thông tin người dùng.";
                    return View("Login");
                }

                var profile = await profileRes.Content.ReadJsonAsync<AdminProfileDto>();
                if (profile?.IsLocked == true)
                {
                    ViewBag.Error = "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ quản trị viên.";
                    return View("Login");
                }

                var isAllowedRole = profile != null && (
                    string.Equals(profile.RoleName, "Administrator", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(profile.RoleName, "Developer", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(profile.RoleName, "Staff", StringComparison.OrdinalIgnoreCase));

                if (!isAllowedRole)
                {
                    ViewBag.Error = "Tài khoản không có quyền truy cập vào Cổng Quản Trị.";
                    return View("Login");
                }

                AppendAdminCookie("access_token", token);
                AppendAdminCookie("admin_name", profile!.Username ?? loginResult.Username ?? "Quản trị viên");
                AppendAdminCookie("admin_email", profile.Email ?? loginResult.Email ?? string.Empty);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi kết nối: {ex.Message}";
                return View("Login");
            }
        }

        [HttpGet("/Auth/AutoLogin")]
        public async Task<IActionResult> AutoLogin([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction(nameof(Login));
            }

            try
            {
                var client = _httpClientFactory.CreateClient("API");
                using var request = new HttpRequestMessage(HttpMethod.Get, "api/users/profile");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var response = await client.SendAsync(request, HttpContext.RequestAborted);

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ.";
                    return RedirectToAction(nameof(Login));
                }

                var profile = await response.Content.ReadJsonAsync<AdminProfileDto>();
                if (profile?.IsLocked == true)
                {
                    TempData["ErrorMessage"] = "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ hỗ trợ.";
                    return RedirectToAction(nameof(Login));
                }

                var isAllowedRole = profile != null && (
                    string.Equals(profile.RoleName, "Administrator", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(profile.RoleName, "Developer", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(profile.RoleName, "Staff", StringComparison.OrdinalIgnoreCase));

                if (!isAllowedRole)
                {
                    TempData["ErrorMessage"] = "Tài khoản không có quyền truy cập cổng quản trị.";
                    return RedirectToAction(nameof(Login));
                }

                AppendAdminCookie("access_token", token);
                AppendAdminCookie("admin_name", profile!.Username ?? "Quản trị viên");
                AppendAdminCookie("admin_email", profile.Email ?? string.Empty);

                return RedirectToAction("Index", "Home");
            }
            catch
            {
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpPost("/Auth/Bridge")]
        [IgnoreAntiforgeryToken]
        [RequestSizeLimit(32 * 1024)]
        public async Task<IActionResult> Bridge([FromForm] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiên đăng nhập từ ứng dụng.";
                return RedirectToAction(nameof(Login));
            }

            try
            {
                var client = _httpClientFactory.CreateClient("API");
                using var request = new HttpRequestMessage(HttpMethod.Get, "api/users/profile");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var response = await client.SendAsync(request, HttpContext.RequestAborted);

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ.";
                    return RedirectToAction(nameof(Login));
                }

                var profile = await response.Content.ReadJsonAsync<AdminProfileDto>();
                if (profile?.IsLocked == true)
                {
                    TempData["ErrorMessage"] = "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ hỗ trợ.";
                    return RedirectToAction(nameof(Login));
                }

                var isAllowedRole = profile != null && (
                    string.Equals(profile.RoleName, "Administrator", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(profile.RoleName, "Developer", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(profile.RoleName, "Staff", StringComparison.OrdinalIgnoreCase));

                if (!isAllowedRole)
                {
                    TempData["ErrorMessage"] = "Tài khoản không có quyền truy cập cổng quản trị.";
                    return RedirectToAction(nameof(Login));
                }

                AppendAdminCookie("access_token", token);
                AppendAdminCookie("admin_name", profile!.Username ?? "Quản trị viên");
                AppendAdminCookie("admin_email", profile.Email ?? string.Empty);

                TempData["SuccessMessage"] = $"Đăng nhập thành công! Xin chào {profile.Username ?? profile.Email}.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                TempData["ErrorMessage"] = "Không thể xác thực phiên đăng nhập. Vui lòng thử lại.";
                return RedirectToAction(nameof(Login));
            }
        }

        private void AppendAdminCookie(string name, string value)
        {
            Response.Cookies.Append(name, value, new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.AddHours(12),
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Path = "/"
            });
        }

        [HttpGet("/Auth/Logout")]
        public IActionResult LogoutGet()
        {
            return PerformLogout();
        }

        [HttpPost("/Auth/Logout")]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            return PerformLogout();
        }

        private IActionResult PerformLogout()
        {
            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("admin_name");
            Response.Cookies.Delete("admin_email");
            return RedirectToAction(nameof(Login));
        }

        private class AdminProfileDto
        {
            [JsonPropertyName("username")]
            public string? Username { get; set; }

            [JsonPropertyName("email")]
            public string? Email { get; set; }

            [JsonPropertyName("roleName")]
            public string? RoleName { get; set; }

            [JsonPropertyName("isLocked")]
            public bool IsLocked { get; set; }
        }

        private class LoginResponseDto
        {
            [JsonPropertyName("token")]
            public string? Token { get; set; }

            [JsonPropertyName("username")]
            public string? Username { get; set; }

            [JsonPropertyName("email")]
            public string? Email { get; set; }

            [JsonPropertyName("role")]
            public string? Role { get; set; }
        }
    }
}
