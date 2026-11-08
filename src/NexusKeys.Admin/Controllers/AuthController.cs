using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

        private string GetSafeFrontendUrl()
        {
            return Backend.Extensions.PortalUrlHelper.GetFrontendUrl(_configuration, HttpContext);
        }

        [HttpGet("/login")]
        [HttpGet("/Auth/Login")]
        public IActionResult Login([FromQuery] bool expired = false, [FromQuery] bool apiUnavailable = false, [FromQuery] string? error = null)
        {
            var bridgeError = TempData["ErrorMessage"] as string;
            var token = Request.Cookies["access_token"];
            if (!expired && !apiUnavailable && string.IsNullOrEmpty(bridgeError) && string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index", "Home");
            }

            if (expired) ViewBag.ErrorMessage = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.";
            else if (apiUnavailable) ViewBag.ErrorMessage = "Máy chủ API hiện không khả dụng. Vui lòng kiểm tra lại dịch vụ Backend.";
            else if (!string.IsNullOrWhiteSpace(bridgeError)) ViewBag.ErrorMessage = bridgeError;
            else if (!string.IsNullOrWhiteSpace(error)) ViewBag.ErrorMessage = error;

            var frontendBase = GetSafeFrontendUrl();
            ViewBag.FrontendUrl = frontendBase;
            ViewBag.StorefrontLoginUrl = $"{frontendBase}/login?returnUrl=/admin";

            return View();
        }

        [HttpPost("/login")]
        [HttpPost("/Auth/Login")]
        public async Task<IActionResult> LoginPost([FromForm] string? email, [FromForm] string? password, [FromForm] string? action)
        {
            var frontendBase = GetSafeFrontendUrl();
            ViewBag.FrontendUrl = frontendBase;
            ViewBag.StorefrontLoginUrl = $"{frontendBase}/login?returnUrl=/admin";

            if (string.Equals(action, "demo", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(action, "demo-admin", StringComparison.OrdinalIgnoreCase))
            {
                email = "admin@nexuskeys.com";
                password = "Admin123!";
            }
            else if (string.Equals(action, "demo-staff", StringComparison.OrdinalIgnoreCase))
            {
                email = "staff@nexuskeys.com";
                password = "Staff123!";
            }
            else if (string.Equals(action, "demo-dev", StringComparison.OrdinalIgnoreCase))
            {
                email = "dev@cdprojekt.com";
                password = "Dev123!";
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ email và mật khẩu.";
                return View("Login");
            }

            try
            {
                var client = _httpClientFactory.CreateClient("API");
                var loginPayload = new { email = email.Trim(), password = password };
                using var response = await client.PostAsJsonAsync("api/v1/auth/login", loginPayload, HttpContext.RequestAborted);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.ErrorMessage = "Email hoặc mật khẩu không chính xác. Vui lòng thử lại.";
                    return View("Login");
                }

                var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                if (result == null || string.IsNullOrWhiteSpace(result.Token))
                {
                    ViewBag.ErrorMessage = "Phản hồi đăng nhập không hợp lệ từ máy chủ API.";
                    return View("Login");
                }

                var isAllowedRole = string.Equals(result.Role, "Administrator", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(result.Role, "Developer", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(result.Role, "Staff", StringComparison.OrdinalIgnoreCase);

                if (!isAllowedRole)
                {
                    ViewBag.ErrorMessage = "Tài khoản của bạn không có quyền truy cập Cổng Quản Trị.";
                    return View("Login");
                }

                AppendAdminCookie("access_token", result.Token);
                AppendAdminCookie("admin_name", result.Username ?? "Quản trị viên");
                AppendAdminCookie("admin_email", result.Email ?? email);

                TempData["SuccessMessage"] = $"Đăng nhập thành công! Xin chào {result.Username ?? result.Email}.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Không thể kết nối đến máy chủ API: " + ex.Message;
                return View("Login");
            }
        }

        [HttpPost("/Auth/Bridge")]
        [IgnoreAntiforgeryToken]
        [RequestSizeLimit(32 * 1024)]
        public async Task<IActionResult> Bridge([FromForm] string token)
        {
            var allowedOrigin = GetSafeFrontendUrl();
            var requestOrigin = Request.Headers.Origin.ToString().TrimEnd('/');
            if (string.IsNullOrEmpty(requestOrigin) && Request.Headers.TryGetValue("Referer", out var refererVal))
            {
                if (Uri.TryCreate(refererVal.ToString(), UriKind.Absolute, out var refererUri))
                {
                    requestOrigin = $"{refererUri.Scheme}://{refererUri.Authority}".TrimEnd('/');
                }
            }

            var isTrustedOrigin = string.IsNullOrEmpty(requestOrigin) ||
                string.Equals(requestOrigin, allowedOrigin, StringComparison.OrdinalIgnoreCase) ||
                requestOrigin.Contains("nexuskeys-storefront.onrender.com", StringComparison.OrdinalIgnoreCase) ||
                requestOrigin.Contains("localhost:5205", StringComparison.OrdinalIgnoreCase) ||
                requestOrigin.Contains("localhost:5000", StringComparison.OrdinalIgnoreCase) ||
                requestOrigin.Contains("localhost:5002", StringComparison.OrdinalIgnoreCase);

            if (!isTrustedOrigin)
            {
                TempData["ErrorMessage"] = "Yêu cầu đăng nhập không hợp lệ từ nguồn không được xác thực.";
                return RedirectToAction(nameof(Login));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiên đăng nhập từ ứng dụng.";
                return RedirectToAction(nameof(Login));
            }

            try
            {
                var client = _httpClientFactory.CreateClient("API");
                using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/users/me");
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

        [HttpGet("/Auth/AutoLogin")]
        public async Task<IActionResult> AutoLogin([FromQuery] string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["ErrorMessage"] = "Không tìm thấy phiên đăng nhập.";
                return RedirectToAction(nameof(Login));
            }

            try
            {
                var client = _httpClientFactory.CreateClient("API");
                using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/users/me");
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
                Expires = DateTimeOffset.UtcNow.AddHours(8),
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
        public IActionResult Logout()
        {
            return PerformLogout();
        }

        private IActionResult PerformLogout()
        {
            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("admin_name");
            Response.Cookies.Delete("admin_email");
            var frontendBase = GetSafeFrontendUrl();
            return Redirect($"{frontendBase}/login?loggedOut=true");
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
