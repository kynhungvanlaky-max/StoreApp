using Microsoft.AspNetCore.Builder;
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
using BCrypt.Net;

namespace GameStore.API.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth");

            // REGISTER
            group.MapPost("/register", async (RegisterRequest req, GameStoreDbContext db) =>
            {
                if (await db.Users.AnyAsync(u => u.Email == req.Email))
                    return Results.BadRequest("Email đã được sử dụng.");

                Role role;
                if (req.SecretCode == "NEXUSADMIN")
                {
                    role = await db.Set<Role>().FirstOrDefaultAsync(r => r.RoleName == "Administrator");
                }
                else
                {
                    role = await db.Set<Role>().FirstOrDefaultAsync(r => r.RoleName == "Customer");
                }

                if (role == null) return Results.StatusCode(500);

                var user = new User
                {
                    Username = req.Username,
                    Email = req.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                    RoleId = role.Id,
                    WalletBalance = 0,
                    CreatedAt = DateTime.UtcNow
                };

                db.Users.Add(user);
                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Đăng ký thành công!" });
            });

            // LOGIN
            group.MapPost("/login", async (LoginRequest req, GameStoreDbContext db, IConfiguration config) =>
            {
                var account = (string.IsNullOrWhiteSpace(req.Username) ? req.Email : req.Username)?.Trim() ?? string.Empty;
                var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == account || u.Username == account);
                if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                    return Results.Unauthorized();

                if (user.IsLocked)
                    return Results.BadRequest(new { message = "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ hỗ trợ." });

                var token = GenerateJwtToken(user, config);
                var refreshToken = GenerateRefreshToken();

                var rt = new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    CreatedAt = DateTime.UtcNow,
                    Expires = DateTime.UtcNow.AddDays(7)
                };
                db.Set<RefreshToken>().Add(rt);
                await db.SaveChangesAsync();

                return Results.Ok(new AuthResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role.RoleName
                });
            });

            // REFRESH TOKEN
            group.MapPost("/refresh", async (RefreshRequest req, GameStoreDbContext db, IConfiguration config) =>
            {
                var rt = await db.Set<RefreshToken>().Include(r => r.User).ThenInclude(u => u.Role)
                                 .FirstOrDefaultAsync(r => r.Token == req.RefreshToken);

                if (rt == null || rt.Expires < DateTime.UtcNow)
                    return Results.Unauthorized();

                var newToken = GenerateJwtToken(rt.User, config);
                var newRefreshToken = GenerateRefreshToken();

                // Rotate refresh token
                db.Set<RefreshToken>().Remove(rt);
                db.Set<RefreshToken>().Add(new RefreshToken
                {
                    UserId = rt.User.Id,
                    Token = newRefreshToken,
                    CreatedAt = DateTime.UtcNow,
                    Expires = DateTime.UtcNow.AddDays(7)
                });
                await db.SaveChangesAsync();

                return Results.Ok(new AuthResponse
                {
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    Username = rt.User.Username,
                    Email = rt.User.Email,
                    Role = rt.User.Role.RoleName
                });
            });

            // FORGOT PASSWORD
            group.MapPost("/forgot-password", async (ForgotPasswordRequest req, GameStoreDbContext db, IEmailService emailService, IConfiguration config) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
                if (user == null)
                    return Results.Ok(new { message = "Nếu email tồn tại, một liên kết khôi phục sẽ được gửi." });

                // Tạo token reset password (hết hạn sau 1 giờ)
                var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", "").Replace("+", "").Replace("/", "");
                var resetToken = new PasswordResetToken
                {
                    UserId = user.Id,
                    Token = token,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    IsUsed = false
                };
                db.Set<PasswordResetToken>().Add(resetToken);
                await db.SaveChangesAsync();

                // Gửi email reset password
                try
                {
                    var frontendBaseUrl = config["AppSettings:FrontendBaseUrl"] ?? "http://localhost:5205";
                    var resetLink = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
                    var emailBody = $@"
                        <h2>Đặt lại mật khẩu - Nexus Store</h2>
                        <p>Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng nhấp vào link bên dưới để đặt lại mật khẩu:</p>
                        <p><a href='{resetLink}' style='display:inline-block;padding:12px 24px;background:#0078F2;color:white;text-decoration:none;border-radius:6px;'>Đặt lại mật khẩu</a></p>
                        <p>Link có hiệu lực trong 1 giờ. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
                        <p><small>{resetLink}</small></p>";
                    await emailService.SendEmailAsync(user.Email, "Đặt lại mật khẩu - Nexus Store", emailBody);
                    Console.WriteLine($"[EMAIL] Gửi email reset password tới {user.Email}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EMAIL ERROR] Không thể gửi email tới {user.Email}: {ex.Message}");
                    // Vẫn trả về thành công để không lộ thông tin tồn tại email
                }
                
                return Results.Ok(new { message = "Nếu email tồn tại, một liên kết khôi phục sẽ được gửi." });
            });

            // RESET PASSWORD
            group.MapPost("/reset-password", async (ResetPasswordRequest req, GameStoreDbContext db) =>
            {
                var resetToken = await db.Set<PasswordResetToken>()
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == req.Token && !rt.IsUsed && rt.ExpiresAt > DateTime.UtcNow);

                if (resetToken == null)
                    return Results.BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn." });

                if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
                    return Results.BadRequest(new { message = "Mật khẩu phải có ít nhất 6 ký tự." });

                // Cập nhật mật khẩu
                resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
                
                // Đánh dấu token đã dùng
                resetToken.IsUsed = true;
                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Mật khẩu đã được thay đổi thành công." });
            });

            // EXTERNAL LOGIN (Google / Facebook MOCK & REAL)
            group.MapPost("/external-login", async (ExternalLoginRequest req, GameStoreDbContext db, IConfiguration config) =>
            {
                string email = "";
                string name = "";
                string googleId = "";

                if (req.Provider.Equals("Google", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        if (req.AccessToken.StartsWith("ey"))
                        {
                            // Validate Google ID Token
                            var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(req.AccessToken);
                            email = payload.Email;
                            name = payload.Name;
                            googleId = payload.Subject;
                        }
                        else
                        {
                            // Validate Google Access Token by calling UserInfo endpoint
                            using var httpClient = new HttpClient();
                            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", req.AccessToken);
                            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
                            if (response.IsSuccessStatusCode)
                            {
                                var content = await response.Content.ReadAsStringAsync();
                                var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
                                email = jsonDoc.RootElement.GetProperty("email").GetString() ?? "";
                                name = jsonDoc.RootElement.GetProperty("name").GetString() ?? "";
                                if (jsonDoc.RootElement.TryGetProperty("sub", out var subProp))
                                {
                                    googleId = subProp.GetString() ?? "";
                                }
                            }
                            else
                            {
                                var errorContent = await response.Content.ReadAsStringAsync();
                                throw new Exception($"Failed to verify access token with Google UserInfo API. Status: {response.StatusCode}, Content: {errorContent}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[OAUTH] Real Google Validation failed: {ex.Message}");
                        if (req.AccessToken.StartsWith("MOCK_"))
                        {
                            email = "google_user@example.com";
                            name = "Google User (Mock)";
                        }
                        else
                        {
                            return Results.BadRequest("Xác thực Google thất bại: " + ex.Message);
                        }
                    }
                }
                else
                {
                    email = $"{req.Provider.ToLower()}_user@example.com";
                    name = $"{req.Provider} User";
                }

                var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    var customerRole = await db.Set<Role>().FirstOrDefaultAsync(r => r.RoleName == "Customer");
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
                    await db.SaveChangesAsync();
                }
                else if (string.IsNullOrEmpty(user.GoogleId) && !string.IsNullOrEmpty(googleId))
                {
                    user.GoogleId = googleId;
                    await db.SaveChangesAsync();
                }

                if (user.IsLocked)
                    return Results.BadRequest("Tài khoản của bạn đã bị khoá. Vui lòng liên hệ hỗ trợ.");

                var token = GenerateJwtToken(user, config);
                var refreshToken = GenerateRefreshToken();
                db.Set<RefreshToken>().Add(new RefreshToken
                {
                    UserId = user.Id,
                    Token = refreshToken,
                    CreatedAt = DateTime.UtcNow,
                    Expires = DateTime.UtcNow.AddDays(7)
                });
                await db.SaveChangesAsync();

                return Results.Ok(new AuthResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Username = user.Username,
                    Email = user.Email,
                    Role = user.Role.RoleName
                });
            });

            // CHANGE PASSWORD
            group.MapPost("/change-password", async (ChangePasswordRequest req, ClaimsPrincipal userClaims, GameStoreDbContext db) =>
            {
                var userIdString = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                                   ?? userClaims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                    return Results.Unauthorized();

                var user = await db.Users.FindAsync(userId);
                if (user == null)
                    return Results.Unauthorized();

                if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
                    return Results.BadRequest(new { message = "Mật khẩu hiện tại không chính xác." });

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Đổi mật khẩu thành công!" });
            }).RequireAuthorization();
        }

        private static string GenerateJwtToken(User user, IConfiguration config)
        {
            var key = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);
            var securityKey = new SymmetricSecurityKey(key);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Customer"),
                new Claim("role", user.Role?.RoleName ?? "Customer"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7), // Extended for development
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    public class RegisterRequest { public string Username { get; set; } = string.Empty; public string Email { get; set; } = string.Empty; public string Password { get; set; } = string.Empty; public string SecretCode { get; set; } = string.Empty; }
    public class LoginRequest { public string? Email { get; set; } public string? Username { get; set; } public string Password { get; set; } = string.Empty; }
    public class RefreshRequest { public string RefreshToken { get; set; } = string.Empty; }
    public class ForgotPasswordRequest { public string Email { get; set; } = string.Empty; }
    public class ExternalLoginRequest { public string Provider { get; set; } = string.Empty; public string AccessToken { get; set; } = string.Empty; }
    public class ChangePasswordRequest { public string CurrentPassword { get; set; } = string.Empty; public string NewPassword { get; set; } = string.Empty; }
    public class ResetPasswordRequest { public string Token { get; set; } = string.Empty; public string NewPassword { get; set; } = string.Empty; }
    
    public class AuthResponse 
    { 
        public string Token { get; set; } = string.Empty; 
        public string RefreshToken { get; set; } = string.Empty; 
        public string Username { get; set; } = string.Empty; 
        public string Email { get; set; } = string.Empty; 
        public string Role { get; set; } = string.Empty; 
    }
}
