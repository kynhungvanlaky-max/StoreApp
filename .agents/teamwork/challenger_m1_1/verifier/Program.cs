using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GameStore.API.Data;
using GameStore.API.Endpoints;
using GameStore.API.Entities;
using GameStore.API.Infrastructure;
using GameStore.API.Interfaces;
using GameStore.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ChallengerVerifier
{
    public class Program
    {
        private static int _failures = 0;
        private static int _passes = 0;

        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("   EMPIRICAL ADVERSARIAL CHALLENGER VERIFICATION SUITE    ");
            Console.WriteLine("==========================================================");

            await using var host = await TestAppHost.StartAsync();

            await TestRefreshTokenRotationAndReplay(host);
            await TestRevocationOnPasswordChangeAndReset(host);
            await TestLockedUserLoginVsRefresh(host);
            await TestRegisterValidationAndPrivilegeEscalation(host);
            await TestLogoutRevocationBehavior(host);
            await TestTokenClaimsAndCryptoIntegrity(host);

            Console.WriteLine("==========================================================");
            Console.WriteLine($"RESULTS: {_passes} PASSED, {_failures} FAILED");
            Console.WriteLine("==========================================================");

            return _failures == 0 ? 0 : 1;
        }

        private static void AssertTrue(bool condition, string testName, string details = "")
        {
            if (condition)
            {
                Console.WriteLine($"  [PASS] {testName}");
                _passes++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  [FAIL] {testName}: {details}");
                Console.ResetColor();
                _failures++;
            }
        }

        // =========================================================================
        // 1. REFRESH TOKEN ROTATION & REPLAY ATTACK MITIGATION
        // =========================================================================
        private static async Task TestRefreshTokenRotationAndReplay(TestAppHost host)
        {
            Console.WriteLine("\n--- SECTION 1: Refresh Token Rotation & Replay Mitigation ---");

            var userId = await host.CreateUserAsync("rotate_user", "rotate@test.com", "Password123!");

            // Login to obtain initial tokens
            using var client = host.CreateClient();
            var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
            {
                Email = "rotate@test.com",
                Password = "Password123!"
            });

            AssertTrue(loginRes.StatusCode == HttpStatusCode.OK, "Login returns HTTP 200");
            var loginData = await loginRes.Content.ReadFromJsonAsync<AuthResponse>();
            AssertTrue(!string.IsNullOrEmpty(loginData?.RefreshToken), "Refresh token received on login");
            var token1 = loginData!.RefreshToken;

            // 1st Refresh: should succeed and issue a new token
            var refresh1Res = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest
            {
                RefreshToken = token1
            });
            AssertTrue(refresh1Res.StatusCode == HttpStatusCode.OK, "First refresh succeeds with HTTP 200");
            var refreshData1 = await refresh1Res.Content.ReadFromJsonAsync<AuthResponse>();
            var token2 = refreshData1!.RefreshToken;

            AssertTrue(token1 != token2, "New refresh token is distinct from old refresh token (rotation verified)");

            // Check DB: token1 must be deleted, token2 must exist
            var token1InDb = await host.WithDbAsync(db => db.RefreshTokens.AnyAsync(t => t.Token == token1));
            var token2InDb = await host.WithDbAsync(db => db.RefreshTokens.AnyAsync(t => t.Token == token2));
            AssertTrue(!token1InDb, "Old refresh token was removed from DB");
            AssertTrue(token2InDb, "New refresh token is persisted in DB");

            // REPLAY ATTACK: Try to use token1 again -> MUST FAIL (HTTP 401)
            var replayRes = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest
            {
                RefreshToken = token1
            });
            AssertTrue(replayRes.StatusCode == HttpStatusCode.Unauthorized, "Replaying revoked refresh token returns HTTP 401 Unauthorized");

            // Expired token attempt
            await host.WithDbAsync(async db =>
            {
                db.RefreshTokens.Add(new RefreshToken
                {
                    UserId = userId,
                    Token = "expired_token_xyz",
                    CreatedAt = DateTime.UtcNow.AddDays(-8),
                    Expires = DateTime.UtcNow.AddMinutes(-10)
                });
                await db.SaveChangesAsync();
            });

            var expiredRes = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest
            {
                RefreshToken = "expired_token_xyz"
            });
            AssertTrue(expiredRes.StatusCode == HttpStatusCode.Unauthorized, "Expired refresh token returns HTTP 401 Unauthorized");

            // Blank token attempt
            var blankRes = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest
            {
                RefreshToken = ""
            });
            AssertTrue(blankRes.StatusCode == HttpStatusCode.Unauthorized, "Empty refresh token returns HTTP 401 Unauthorized");
        }

        // =========================================================================
        // 2. TOKEN REVOCATION ON PASSWORD CHANGE AND RESET
        // =========================================================================
        private static async Task TestRevocationOnPasswordChangeAndReset(TestAppHost host)
        {
            Console.WriteLine("\n--- SECTION 2: Token Revocation on Password Change & Reset ---");

            var userId = await host.CreateUserAsync("pwd_user", "pwd@test.com", "OldPassword123!");

            // Seed multiple active refresh tokens simulating multiple sessions
            await host.WithDbAsync(async db =>
            {
                db.RefreshTokens.AddRange(
                    new RefreshToken { UserId = userId, Token = "session_pc", CreatedAt = DateTime.UtcNow, Expires = DateTime.UtcNow.AddDays(7) },
                    new RefreshToken { UserId = userId, Token = "session_mobile", CreatedAt = DateTime.UtcNow, Expires = DateTime.UtcNow.AddDays(7) },
                    new RefreshToken { UserId = userId, Token = "session_tablet", CreatedAt = DateTime.UtcNow, Expires = DateTime.UtcNow.AddDays(7) }
                );
                await db.SaveChangesAsync();
            });

            var initialTokensCount = await host.WithDbAsync(db => db.RefreshTokens.CountAsync(t => t.UserId == userId));
            AssertTrue(initialTokensCount == 3, "User has 3 active sessions before password change");

            // Change password via authenticated endpoint
            using var authClient = host.CreateClient(role: "Customer", userId: userId);
            var changeRes = await authClient.PostAsJsonAsync("/api/v1/users/me/password-changes", new
            {
                oldPassword = "OldPassword123!",
                newPassword = "NewPassword456!"
            });

            AssertTrue(changeRes.StatusCode == HttpStatusCode.NoContent, "Password change returns HTTP 204 NoContent");
            var remainingTokensAfterChange = await host.WithDbAsync(db => db.RefreshTokens.CountAsync(t => t.UserId == userId));
            AssertTrue(remainingTokensAfterChange == 0, "ALL refresh tokens purged after password change (0 remaining)");

            // Verify the new password actually works
            using var unauthClient = host.CreateClient();
            var newLoginRes = await unauthClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
            {
                Email = "pwd@test.com",
                Password = "NewPassword456!"
            });
            AssertTrue(newLoginRes.StatusCode == HttpStatusCode.OK, "Login succeeds with new password");

            // Seed new tokens and test reset-password
            var loginData = await newLoginRes.Content.ReadFromJsonAsync<AuthResponse>();
            var activeToken = loginData!.RefreshToken;

            // Generate password reset token
            await host.WithDbAsync(async db =>
            {
                db.PasswordResetTokens.Add(new PasswordResetToken
                {
                    UserId = userId,
                    Token = "valid-prt-123",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    IsUsed = false
                });
                await db.SaveChangesAsync();
            });

            var resetRes = await unauthClient.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest
            {
                Token = "valid-prt-123",
                NewPassword = "ResetPassword789!"
            });

            AssertTrue(resetRes.StatusCode == HttpStatusCode.OK, "Password reset returns HTTP 200 OK");
            var remainingTokensAfterReset = await host.WithDbAsync(db => db.RefreshTokens.CountAsync(t => t.UserId == userId));
            AssertTrue(remainingTokensAfterReset == 0, "ALL refresh tokens purged after password reset (0 remaining)");

            var prtEntity = await host.WithDbAsync(db => db.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == "valid-prt-123"));
            AssertTrue(prtEntity != null && prtEntity.IsUsed, "Password reset token is marked as IsUsed = true");

            // Re-using reset token must fail
            var reusedResetRes = await unauthClient.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest
            {
                Token = "valid-prt-123",
                NewPassword = "AnotherPassword!"
            });
            AssertTrue(reusedResetRes.StatusCode == HttpStatusCode.BadRequest, "Re-using consumed password reset token returns HTTP 400 BadRequest");
        }

        // =========================================================================
        // 3. LOCKED USER REJECTION (LOGIN HTTP 400 ProblemDetails vs REFRESH HTTP 401)
        // =========================================================================
        private static async Task TestLockedUserLoginVsRefresh(TestAppHost host)
        {
            Console.WriteLine("\n--- SECTION 3: Locked User Rejection (Login 400 vs Refresh 401) ---");

            var userId = await host.CreateUserAsync("locked_user", "locked@test.com", "Password123!", isLocked: true);

            // Attempt login with locked account
            using var client = host.CreateClient();
            var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
            {
                Email = "locked@test.com",
                Password = "Password123!"
            });

            AssertTrue(loginRes.StatusCode == HttpStatusCode.BadRequest, "Locked user login returns HTTP 400 BadRequest");
            AssertTrue(loginRes.Content.Headers.ContentType?.MediaType == "application/problem+json", "Locked user login returns ProblemDetails content type");

            var problemJson = await loginRes.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(problemJson);
            var hasIsLocked = doc.RootElement.TryGetProperty("isLocked", out var isLockedProp) && isLockedProp.GetBoolean();
            AssertTrue(hasIsLocked, "Locked user login ProblemDetails includes 'isLocked': true");

            // Check that no refresh token was issued
            var tokenCount = await host.WithDbAsync(db => db.RefreshTokens.CountAsync(t => t.UserId == userId));
            AssertTrue(tokenCount == 0, "No refresh tokens created for locked user login attempt");

            // Now test Refresh when user was locked after token issuance
            // Add an existing refresh token directly to DB for this locked user
            await host.WithDbAsync(async db =>
            {
                db.RefreshTokens.Add(new RefreshToken
                {
                    UserId = userId,
                    Token = "token_before_lock",
                    CreatedAt = DateTime.UtcNow,
                    Expires = DateTime.UtcNow.AddDays(7)
                });
                await db.SaveChangesAsync();
            });

            var refreshRes = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest
            {
                RefreshToken = "token_before_lock"
            });

            AssertTrue(refreshRes.StatusCode == HttpStatusCode.Unauthorized, "Locked user refresh attempt returns HTTP 401 Unauthorized");
        }

        // =========================================================================
        // 4. REGISTER VALIDATION & PRIVILEGE ESCALATION BLOCKING
        // =========================================================================
        private static async Task TestRegisterValidationAndPrivilegeEscalation(TestAppHost host)
        {
            Console.WriteLine("\n--- SECTION 4: Register Validation & Privilege Escalation ---");

            using var client = host.CreateClient();

            // Short username
            var shortUserRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
            {
                Username = "ab",
                Email = "valid@test.com",
                Password = "password123"
            });
            AssertTrue(shortUserRes.StatusCode == HttpStatusCode.BadRequest, "Short username (<3) returns HTTP 400 BadRequest");

            // Invalid email
            var badEmailRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
            {
                Username = "validname",
                Email = "not-an-email",
                Password = "password123"
            });
            AssertTrue(badEmailRes.StatusCode == HttpStatusCode.BadRequest, "Invalid email format returns HTTP 400 BadRequest");

            // Short password
            var shortPwdRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
            {
                Username = "validname",
                Email = "valid@test.com",
                Password = "123"
            });
            AssertTrue(shortPwdRes.StatusCode == HttpStatusCode.BadRequest, "Short password (<6) returns HTTP 400 BadRequest");

            // Privilege escalation with secretCode NEXUSADMIN
            var adminAttemptRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
            {
                Username = "hacker",
                Email = "hacker@test.com",
                Password = "password123",
                SecretCode = "NEXUSADMIN"
            });
            AssertTrue(adminAttemptRes.StatusCode == HttpStatusCode.Forbidden, "Register with secretCode NEXUSADMIN returns HTTP 403 Forbidden");

            // Successful registration
            var validRegisterRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
            {
                Username = "gooduser",
                Email = "gooduser@test.com",
                Password = "password123"
            });
            AssertTrue(validRegisterRes.StatusCode == HttpStatusCode.Created, "Valid registration returns HTTP 201 Created");
            AssertTrue(validRegisterRes.Headers.Location != null, "Valid registration returns Location header");

            var createdUser = await host.WithDbAsync(db => db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == "gooduser@test.com"));
            AssertTrue(createdUser != null && createdUser.Role.RoleName == "Customer", "New registered user is assigned Customer role");
            AssertTrue(createdUser?.WalletBalance == 0, "New registered user has 0 wallet balance");

            // Duplicate username check (case insensitive)
            var dupUserRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
            {
                Username = "GoodUser",
                Email = "different@test.com",
                Password = "password123"
            });
            AssertTrue(dupUserRes.StatusCode == HttpStatusCode.BadRequest, "Duplicate username (case insensitive) returns HTTP 400 BadRequest");

            // Duplicate email check (case insensitive)
            var dupEmailRes = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
            {
                Username = "differentname",
                Email = "GOODUSER@TEST.COM",
                Password = "password123"
            });
            AssertTrue(dupEmailRes.StatusCode == HttpStatusCode.BadRequest, "Duplicate email (case insensitive) returns HTTP 400 BadRequest");
        }

        // =========================================================================
        // 5. LOGOUT ENDPOINT REVOCATION BEHAVIOR
        // =========================================================================
        private static async Task TestLogoutRevocationBehavior(TestAppHost host)
        {
            Console.WriteLine("\n--- SECTION 5: Logout Endpoint Revocation Behavior ---");

            var userId = await host.CreateUserAsync("logout_user", "logout@test.com", "Password123!");

            // Scenario A: Logout with explicit RefreshToken in request body
            await host.WithDbAsync(async db =>
            {
                db.RefreshTokens.AddRange(
                    new RefreshToken { UserId = userId, Token = "logout_token_1", CreatedAt = DateTime.UtcNow, Expires = DateTime.UtcNow.AddDays(7) },
                    new RefreshToken { UserId = userId, Token = "logout_token_2", CreatedAt = DateTime.UtcNow, Expires = DateTime.UtcNow.AddDays(7) }
                );
                await db.SaveChangesAsync();
            });

            using var unauthClient = host.CreateClient();
            var bodyLogoutRes = await unauthClient.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest
            {
                RefreshToken = "logout_token_1"
            });

            AssertTrue(bodyLogoutRes.StatusCode == HttpStatusCode.NoContent, "Logout with body refresh token returns HTTP 204 NoContent");
            var token1StillExists = await host.WithDbAsync(db => db.RefreshTokens.AnyAsync(t => t.Token == "logout_token_1"));
            var token2StillExists = await host.WithDbAsync(db => db.RefreshTokens.AnyAsync(t => t.Token == "logout_token_2"));
            AssertTrue(!token1StillExists, "Specified refresh token was revoked");
            AssertTrue(token2StillExists, "Other active refresh token for user was NOT affected");

            // Scenario B: Logout with authenticated user claims (no body) -> revokes all user tokens
            using var authClient = host.CreateClient(role: "Customer", userId: userId);
            var authLogoutRes = await authClient.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest());

            AssertTrue(authLogoutRes.StatusCode == HttpStatusCode.NoContent, "Authenticated logout returns HTTP 204 NoContent");
            var allRemaining = await host.WithDbAsync(db => db.RefreshTokens.CountAsync(t => t.UserId == userId));
            AssertTrue(allRemaining == 0, "All tokens for authenticated user revoked on logout");

            // Scenario C: Logout without token AND without authentication -> returns HTTP 400
            var emptyLogoutRes = await unauthClient.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest());
            AssertTrue(emptyLogoutRes.StatusCode == HttpStatusCode.BadRequest, "Anonymous logout with no token returns HTTP 400 BadRequest");
        }

        // =========================================================================
        // 6. TOKEN CLAIMS & CRYPTOGRAPHIC INTEGRITY
        // =========================================================================
        private static async Task TestTokenClaimsAndCryptoIntegrity(TestAppHost host)
        {
            Console.WriteLine("\n--- SECTION 6: Token Claims & Cryptographic Integrity ---");

            var userId = await host.CreateUserAsync("claims_user", "claims@test.com", "Password123!");
            var user = await host.WithDbAsync(db => db.Users.Include(u => u.Role).FirstAsync(u => u.Id == userId));

            using var scope = host.Services.CreateScope();
            var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

            var jwtString = tokenService.GenerateAccessToken(user);
            AssertTrue(!string.IsNullOrEmpty(jwtString), "AccessToken generated successfully");

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(jwtString);

            // Verify standard claims
            AssertTrue(jwt.Claims.Any(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString()), "Contains 'sub' claim with correct userId");
            AssertTrue(jwt.Claims.Any(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString()), "Contains 'nameid' claim with correct userId");
            AssertTrue(jwt.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Customer"), "Contains ClaimTypes.Role claim with 'Customer'");
            AssertTrue(jwt.Claims.Any(c => c.Type == "role" && c.Value == "Customer"), "Contains 'role' claim with 'Customer'");
            AssertTrue(jwt.Claims.Any(c => c.Type == JwtRegisteredClaimNames.Jti && Guid.TryParse(c.Value, out _)), "Contains unique 'jti' GUID nonce");

            // Verify lifetime is approximately 30 minutes
            var lifetime = jwt.ValidTo - DateTime.UtcNow;
            AssertTrue(lifetime.TotalMinutes >= 28 && lifetime.TotalMinutes <= 31, $"Access token lifetime is ~30 minutes ({lifetime.TotalMinutes:F1} mins)");

            // Verify refresh token cryptography
            var rt = tokenService.GenerateRefreshToken(userId);
            var bytes = Convert.FromBase64String(rt.Token);
            AssertTrue(bytes.Length == 32, "RefreshToken is cryptographically secure 32 random bytes (256 bits) in Base64");
            var rtLifetime = rt.Expires - rt.CreatedAt;
            AssertTrue(rtLifetime.TotalDays >= 6.9 && rtLifetime.TotalDays <= 7.1, $"RefreshToken lifetime is ~7 days ({rtLifetime.TotalDays:F1} days)");
        }
    }

    // =========================================================================
    // IN-MEMORY HOST FIXTURE
    // =========================================================================
    public sealed class TestAppHost : IAsyncDisposable
    {
        private readonly WebApplication _app;

        public Uri BaseAddress { get; }
        public IServiceProvider Services => _app.Services;

        private TestAppHost(WebApplication app, Uri baseAddress)
        {
            _app = app;
            BaseAddress = baseAddress;
        }

        public static async Task<TestAppHost> StartAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Development"
            });

            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));

            builder.Configuration["Jwt:Key"] = "integration-test-key-with-at-least-thirty-two-characters";
            builder.Configuration["Jwt:Issuer"] = "GameStore.Tests";
            builder.Configuration["Jwt:Audience"] = "GameStore.Tests";
            builder.Configuration["AppSettings:FrontendBaseUrl"] = "http://localhost:5205";

            var databaseName = $"challenger-tests-{Guid.NewGuid():N}";
            builder.Services.AddDbContext<GameStoreDbContext>(options =>
                options
                    .UseInMemoryDatabase(databaseName)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            builder.Services.AddHttpClient();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddSingleton<IEmailService, DummyEmailService>();
            builder.Services.AddScoped<V1ProblemDetailsFilter>();
            builder.Services.AddProblemDetails();
            builder.Services.AddSignalR();

            builder.Services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
            builder.Services.AddAuthorization();

            var app = builder.Build();
            app.UseExceptionHandler();
            app.UseAuthentication();
            app.UseAuthorization();

            var v1 = app.MapGroup("/api/v1")
                .AddEndpointFilter<V1ProblemDetailsFilter>();
            v1.MapAuthEndpoints(string.Empty);
            v1.MapUserEndpoints(string.Empty);

            await app.StartAsync();

            var addresses = app.Services
                .GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>()?
                .Addresses;
            var address = addresses?.FirstOrDefault() ?? throw new InvalidOperationException("No server address bound");

            var host = new TestAppHost(app, new Uri(address));
            await host.SeedRolesAsync();
            return host;
        }

        public HttpClient CreateClient(string? role = null, int userId = 1)
        {
            var client = new HttpClient { BaseAddress = BaseAddress };
            if (role != null)
            {
                client.DefaultRequestHeaders.Add("X-Test-Role", role);
                client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
            }
            return client;
        }

        public async Task WithDbAsync(Func<GameStoreDbContext, Task> action)
        {
            await using var scope = _app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
            await action(db);
        }

        public async Task<T> WithDbAsync<T>(Func<GameStoreDbContext, Task<T>> action)
        {
            await using var scope = _app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
            return await action(db);
        }

        public async Task SeedRolesAsync()
        {
            await WithDbAsync(async db =>
            {
                db.Roles.AddRange(
                    new Role { Id = 1, RoleName = "Customer" },
                    new Role { Id = 2, RoleName = "Administrator" }
                );
                await db.SaveChangesAsync();
            });
        }

        public async Task<int> CreateUserAsync(string username, string email, string password, bool isLocked = false)
        {
            return await WithDbAsync(async db =>
            {
                var user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    RoleId = 1,
                    IsLocked = isLocked,
                    CreatedAt = DateTime.UtcNow,
                    WalletBalance = 0
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();
                return user.Id;
            });
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    public class DummyEmailService : IEmailService
    {
        public Task SendEmailAsync(string to, string subject, string htmlBody) => Task.CompletedTask;
    }

    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "TestAuth";

        public TestAuthHandler(
            Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Role", out var role) ||
                !Request.Headers.TryGetValue("X-Test-UserId", out var userId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("sub", userId.ToString()),
                new Claim(ClaimTypes.Role, role.ToString()),
                new Claim("role", role.ToString())
            };
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
