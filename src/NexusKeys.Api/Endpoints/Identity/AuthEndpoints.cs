using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameStore.API.Data;
using GameStore.API.Entities;
using GameStore.API.Services;
using GameStore.API.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using BCrypt.Net;

namespace GameStore.API.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var group = app.MapGroup($"{apiPrefix}/auth");

            // REGISTER
            group.MapPost("/register", async (RegisterRequest req, GameStoreDbContext db, CancellationToken ct) =>
            {
                var username = req.Username?.Trim() ?? string.Empty;
                var email = req.Email?.Trim() ?? string.Empty;
                var password = req.Password ?? string.Empty;
                var secretCode = req.SecretCode?.Trim() ?? string.Empty;

                // Input validation
                if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                    return Results.BadRequest(new { message = "Tên người dùng phải có ít nhất 3 ký tự." });
                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || !email.Contains("."))
                    return Results.BadRequest(new { message = "Email không hợp lệ." });
                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                    return Results.BadRequest(new { message = "Mật khẩu phải có ít nhất 6 ký tự." });

                // Duplicate check: username
                if (await db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower(), ct))
                    return Results.BadRequest(new { message = "Tên người dùng đã được sử dụng." });

                // Duplicate check: email
                if (await db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower(), ct))
                    return Results.BadRequest(new { message = "Email đã được sử dụng." });

                if (secretCode == "NEXUSADMIN")
                {
                    return Results.Forbid();
                }

                var role = await db.Set<Role>().FirstOrDefaultAsync(r => r.RoleName == "Customer", ct);
                if (role == null) return Results.StatusCode(StatusCodes.Status500InternalServerError);

                var user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    RoleId = role.Id,
                    WalletBalance = 0,
                    CreatedAt = DateTime.UtcNow
                };

                db.Users.Add(user);
                await db.SaveChangesAsync(ct);

                return Results.Created($"{EndpointRouteConventions.PublicPrefix(apiPrefix)}/users/{user.Id}", new { user.Id, message = "Đăng ký thành công!" });
            }).RequireRateLimiting("auth");

            // LOGIN
            group.MapPost("/login", async (
                LoginRequest req,
                GameStoreDbContext db,
                IConfiguration config,
                IServiceProvider sp,
                CancellationToken ct) =>
            {
                var email = req.Email?.Trim() ?? string.Empty;
                var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);
                if (user == null)
                {
                    return Results.Problem(
                        detail: "Email hoặc mật khẩu không chính xác.",
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Đăng nhập thất bại",
                        extensions: new Dictionary<string, object?>
                        {
                            ["message"] = "Email hoặc mật khẩu không chính xác."
                        });
                }

                if (user.IsLocked)
                {
                    return Results.Problem(
                        detail: "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ hỗ trợ.",
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Tài khoản bị khoá",
                        extensions: new Dictionary<string, object?>
                        {
                            ["message"] = "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ hỗ trợ.",
                            ["isLocked"] = true
                        });
                }

                try
                {
                    if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                    {
                        return Results.Problem(
                            detail: "Email hoặc mật khẩu không chính xác.",
                            statusCode: StatusCodes.Status401Unauthorized,
                            title: "Đăng nhập thất bại",
                            extensions: new Dictionary<string, object?>
                            {
                                ["message"] = "Email hoặc mật khẩu không chính xác."
                            });
                    }
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    return Results.Problem(
                        detail: "Email hoặc mật khẩu không chính xác.",
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Đăng nhập thất bại",
                        extensions: new Dictionary<string, object?>
                        {
                            ["message"] = "Email hoặc mật khẩu không chính xác."
                        });
                }

                var tokenService = ResolveTokenService(sp, db, config);
                var authResponse = await tokenService.IssueTokensAsync(user, ct);
                return Results.Ok(authResponse);
            }).RequireRateLimiting("auth");

            // REFRESH TOKEN
            group.MapPost("/refresh", async (
                RefreshRequest req,
                GameStoreDbContext db,
                IConfiguration config,
                IServiceProvider sp,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(req.RefreshToken))
                    return Results.Unauthorized();

                var tokenService = ResolveTokenService(sp, db, config);
                var (success, response, error, isLocked) = await tokenService.RefreshTokenAsync(req.RefreshToken, ct);

                if (!success || response == null)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(response);
            });

            // LOGOUT
            group.MapPost("/logout", async (
                LogoutRequest? req,
                ClaimsPrincipal user,
                GameStoreDbContext db,
                IConfiguration config,
                IServiceProvider sp,
                CancellationToken ct) =>
            {
                var tokenService = ResolveTokenService(sp, db, config);
                var tokenRevoked = false;

                // 1. If explicit refresh token provided in body, revoke it
                if (!string.IsNullOrWhiteSpace(req?.RefreshToken))
                {
                    await tokenService.RevokeTokenAsync(req.RefreshToken, ct);
                    tokenRevoked = true;
                }

                // 2. If caller is authenticated and no refresh token was revoked, revoke all tokens for user
                var userId = user.GetUserId();
                if (userId.HasValue && !tokenRevoked)
                {
                    await tokenService.RevokeUserTokensAsync(userId.Value, ct);
                    tokenRevoked = true;
                }

                // 3. If neither refresh token nor authenticated user provided
                if (!tokenRevoked)
                {
                    return Results.BadRequest(new { message = "Cần cung cấp refresh token hoặc xác thực người dùng để đăng xuất." });
                }

                return EndpointRouteConventions.IsV1(apiPrefix)
                    ? Results.NoContent()
                    : Results.Ok(new { message = "Đăng xuất thành công." });
            });

            // FORGOT PASSWORD
            group.MapPost("/forgot-password", async (ForgotPasswordRequest req, GameStoreDbContext db, IEmailService emailService, IConfiguration config) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
                if (user == null)
                    return Results.Ok(new { message = "Nếu email tồn tại, một liên kết khôi phục sẽ được gửi." });

                // Vô hiệu hóa các token chưa dùng trước đó của user
                var oldTokens = await db.PasswordResetTokens
                    .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();
                foreach (var old in oldTokens)
                {
                    old.IsUsed = true;
                }

                // Tạo token reset password CSPRNG (hết hạn sau 1 giờ)
                var token = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
                var resetToken = new PasswordResetToken
                {
                    UserId = user.Id,
                    Token = token,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    IsUsed = false
                };
                db.PasswordResetTokens.Add(resetToken);
                await db.SaveChangesAsync();

                // Gửi email reset password
                try
                {
                    var frontendBaseUrl = config["AppSettings:FrontendBaseUrl"] ?? "http://localhost:5205";
                    var resetLink = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
                    var emailBody = $@"
                        <h2>Đặt lại mật khẩu - NexusKeys</h2>
                        <p>Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng nhấp vào link bên dưới để đặt lại mật khẩu:</p>
                        <p><a href='{resetLink}' style='display:inline-block;padding:12px 24px;background:#0078F2;color:white;text-decoration:none;border-radius:6px;'>Đặt lại mật khẩu</a></p>
                        <p>Link có hiệu lực trong 1 giờ. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
                        <p><small>{resetLink}</small></p>";
                    await emailService.SendEmailAsync(user.Email, "Đặt lại mật khẩu - NexusKeys", emailBody);
                    Console.WriteLine($"[EMAIL] Gửi email reset password tới {user.Email}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EMAIL ERROR] Không thể gửi email tới {user.Email}: {ex.Message}");
                    // Vẫn trả về thành công để không lộ thông tin tồn tại email
                }
                
                return Results.Ok(new { message = "Nếu email tồn tại, một liên kết khôi phục sẽ được gửi." });
            }).RequireRateLimiting("auth");

            // RESET PASSWORD
            group.MapPost("/reset-password", async (
                ResetPasswordRequest req,
                GameStoreDbContext db,
                IConfiguration config,
                IServiceProvider sp,
                CancellationToken ct) =>
            {
                var resetToken = await db.PasswordResetTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == req.Token && !rt.IsUsed && rt.ExpiresAt > DateTime.UtcNow, ct);

                if (resetToken == null)
                    return Results.BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn." });

                if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
                    return Results.BadRequest(new { message = "Mật khẩu phải có ít nhất 6 ký tự." });

                // Cập nhật mật khẩu
                resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
                resetToken.IsUsed = true;
                await db.SaveChangesAsync(ct);

                // Thu hồi tất cả refresh token cũ qua ITokenService
                var tokenService = ResolveTokenService(sp, db, config);
                await tokenService.RevokeUserTokensAsync(resetToken.UserId, ct);

                return Results.Ok(new { message = "Mật khẩu đã được thay đổi thành công." });
            }).RequireRateLimiting("auth");

            // EXTERNAL LOGIN (Google / Facebook MOCK & REAL)
            group.MapPost("/external-login", async (
                ExternalLoginRequest req,
                GameStoreDbContext db,
                IConfiguration config,
                IWebHostEnvironment env,
                IServiceProvider sp,
                CancellationToken ct) =>
            {
                string email = "";
                string name = "";
                string googleId = "";

                if (!req.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
                {
                    if (!env.IsDevelopment())
                        return Results.BadRequest(new { message = "Nhà cung cấp xác thực chưa được hỗ trợ." });

                    email = $"{req.Provider.ToLowerInvariant()}_user@example.com";
                    name = $"{req.Provider} User";
                }
                else
                {
                    try
                    {
                        var googleClientId = config["Authentication:Google:ClientId"];
                        var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings();
                        if (!string.IsNullOrWhiteSpace(googleClientId))
                        {
                            settings.Audience = new[] { googleClientId };
                        }

                        if (req.AccessToken.StartsWith("ey"))
                        {
                            // Validate Google ID Token
                            var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(req.AccessToken, settings);
                            if (!payload.EmailVerified)
                                return Results.BadRequest(new { message = "Email Google chưa được xác minh." });

                            email = payload.Email;
                            name = payload.Name;
                            googleId = payload.Subject;
                        }
                        else
                        {
                            // Validate Google Access Token by calling UserInfo endpoint
                            using var httpClient = new HttpClient();
                            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", req.AccessToken);
                            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo", ct);
                            if (response.IsSuccessStatusCode)
                            {
                                var content = await response.Content.ReadAsStringAsync(ct);
                                var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
                                var emailVerified = jsonDoc.RootElement.TryGetProperty("email_verified", out var evProp) && evProp.GetBoolean();
                                if (!emailVerified)
                                    return Results.BadRequest(new { message = "Email Google chưa được xác minh." });

                                email = jsonDoc.RootElement.GetProperty("email").GetString() ?? "";
                                name = jsonDoc.RootElement.GetProperty("name").GetString() ?? "";
                                if (jsonDoc.RootElement.TryGetProperty("sub", out var subProp))
                                {
                                    googleId = subProp.GetString() ?? "";
                                }
                            }
                            else
                            {
                                var errorContent = await response.Content.ReadAsStringAsync(ct);
                                throw new Exception($"Failed to verify access token with Google UserInfo API. Status: {response.StatusCode}, Content: {errorContent}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[OAUTH] Real Google Validation failed: {ex.Message}");
                        if (env.IsDevelopment() && req.AccessToken.StartsWith("MOCK_", StringComparison.Ordinal))
                        {
                            email = "google_user@example.com";
                            name = "Google User (Mock)";
                        }
                        else
                        {
                            return Results.BadRequest(new { message = "Xác thực Google thất bại." });
                        }
                    }
                }

                var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email, ct);
                if (user == null)
                {
                    var customerRole = await db.Set<Role>().FirstOrDefaultAsync(r => r.RoleName == "Customer", ct);
                    user = new User
                    {
                        Username = name,
                        Email = email,
                        PasswordHash = "EXTERNAL_OAUTH", // Not used for auth
                        RoleId = customerRole!.Id,
                        WalletBalance = 0,
                        CreatedAt = DateTime.UtcNow,
                        GoogleId = string.IsNullOrEmpty(googleId) ? null : googleId
                    };
                    db.Users.Add(user);
                    await db.SaveChangesAsync(ct);
                }
                else if (string.IsNullOrEmpty(user.GoogleId) && !string.IsNullOrEmpty(googleId))
                {
                    user.GoogleId = googleId;
                    await db.SaveChangesAsync(ct);
                }

                if (user.IsLocked)
                {
                    return Results.Problem(
                        detail: "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ hỗ trợ.",
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Tài khoản bị khoá",
                        extensions: new Dictionary<string, object?>
                        {
                            ["message"] = "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ hỗ trợ.",
                            ["isLocked"] = true
                        });
                }

                var tokenService = ResolveTokenService(sp, db, config);
                var authResponse = await tokenService.IssueTokensAsync(user, ct);
                return Results.Ok(authResponse);
            });

            // Legacy alias. V1 exposes the operation as /users/me/password-changes.
            if (!EndpointRouteConventions.IsV1(apiPrefix))
            {
                group.MapPost("/change-password", async (
                    ChangePasswordRequest req,
                    ClaimsPrincipal userClaims,
                    GameStoreDbContext db,
                    IConfiguration config,
                    IServiceProvider sp,
                    CancellationToken ct) =>
                {
                    var userId = userClaims.GetUserId();
                    if (userId == null) return Results.Unauthorized();

                    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
                    if (user == null)
                        return Results.Unauthorized();

                    if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
                        return Results.BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự." });

                    try
                    {
                        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
                            return Results.BadRequest(new { message = "Mật khẩu hiện tại không chính xác." });
                    }
                    catch (BCrypt.Net.SaltParseException)
                    {
                        return Results.BadRequest(new { message = "Mật khẩu hiện tại không chính xác." });
                    }

                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
                    await db.SaveChangesAsync(ct);

                    var tokenService = ResolveTokenService(sp, db, config);
                    await tokenService.RevokeUserTokensAsync(user.Id, ct);

                    return Results.Ok(new { message = "Đổi mật khẩu thành công!" });
                }).RequireAuthorization();
            }
        }

        private static ITokenService ResolveTokenService(IServiceProvider sp, GameStoreDbContext db, IConfiguration config) =>
            sp.GetService<ITokenService>() ?? new TokenService(config, db);
    }

    public class RegisterRequest { public string Username { get; set; } = string.Empty; public string Email { get; set; } = string.Empty; public string Password { get; set; } = string.Empty; public string SecretCode { get; set; } = string.Empty; }
    public class LoginRequest { public string Email { get; set; } = string.Empty; public string Password { get; set; } = string.Empty; }
    public class RefreshRequest { public string RefreshToken { get; set; } = string.Empty; }
    public class LogoutRequest { public string? RefreshToken { get; set; } }
    public class ForgotPasswordRequest { public string Email { get; set; } = string.Empty; }
    public class ExternalLoginRequest { public string Provider { get; set; } = string.Empty; public string AccessToken { get; set; } = string.Empty; }
    public class ChangePasswordRequest { public string CurrentPassword { get; set; } = string.Empty; public string NewPassword { get; set; } = string.Empty; }
    public class ResetPasswordRequest { public string Token { get; set; } = string.Empty; public string NewPassword { get; set; } = string.Empty; }
}
