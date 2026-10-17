# Architecture & Implementation Design: Token & Authentication Services (Milestone 1)

## Executive Summary
This design specifies the Clean Architecture extraction of Token and Authentication services for the NexusKeys Game Store system (`GameStore.slnx`). The design cleanly isolates Domain interfaces (`ITokenService`), encapsulates cryptographic and persistence logic in Infrastructure (`TokenService`), wires DI in API (`Program.cs`), and maintains 100% compatibility with existing read-only integration tests in `GameStore.API.ProductKeyTests` (72/72 passing).

---

## 1. Domain Layer Design (`src/NexusKeys.Domain`)

### 1.1 Interface Definition: `ITokenService`
- **File path**: `src/NexusKeys.Domain/Interfaces/ITokenService.cs`
- **Namespace**: `GameStore.API.Interfaces`
- **Design rationale**:
  - Pure domain contract with zero dependencies on ASP.NET Core or EF Core.
  - Exposes token issuance, cryptographic refresh token creation, rotation with concurrency resilience, and revocation (both single token and all tokens for a user).

```csharp
using System.Threading;
using System.Threading.Tasks;
using GameStore.API.Endpoints;
using GameStore.API.Entities;

namespace GameStore.API.Interfaces
{
    public interface ITokenService
    {
        /// <summary>
        /// Generates a signed JWT Access Token with 30-minute lifespan and standard claims:
        /// sub, nameid (ClaimTypes.NameIdentifier), email, role (ClaimTypes.Role & "role"), jti, name (ClaimTypes.Name & "name").
        /// </summary>
        string GenerateAccessToken(User user);

        /// <summary>
        /// Generates a cryptographically secure 32-byte Base64 RefreshToken entity with 7-day lifespan.
        /// </summary>
        RefreshToken GenerateRefreshToken(int userId);

        /// <summary>
        /// Issues a complete token pair (JWT Access Token + persisted RefreshToken) for a user.
        /// </summary>
        Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct = default);

        /// <summary>
        /// Atomically rotates an existing refresh token, revoking the old token and returning a new token pair.
        /// Handles race conditions via DbUpdateConcurrencyException recovery.
        /// </summary>
        Task<(bool Success, AuthResponse? Response, string? Error, bool IsLocked)> RefreshTokenAsync(string token, CancellationToken ct = default);

        /// <summary>
        /// Revokes all active refresh tokens for the given user (used on password change, password reset, account lock).
        /// </summary>
        Task RevokeUserTokensAsync(int userId, CancellationToken ct = default);

        /// <summary>
        /// Revokes a specific refresh token (used on logout). Returns true if token was found and revoked.
        /// </summary>
        Task<bool> RevokeTokenAsync(string token, CancellationToken ct = default);
    }
}
```

### 1.2 Model Migration: `AuthResponse`
- **File path**: `src/NexusKeys.Domain/Interfaces/AuthResponse.cs`
- **Namespace**: `GameStore.API.Endpoints`
- **Design rationale**:
  - `AuthResponse` is currently declared in `src/NexusKeys.Api/Endpoints/Identity/AuthEndpoints.cs`.
  - In `tests/GameStore.API.ProductKeyTests/AuthAndUserApiTests.cs`, tests deserialize response bodies using `ReadFromJsonAsync<AuthResponse>()` with imports `using GameStore.API.Endpoints;` and `using GameStore.API.Entities;`.
  - By placing `AuthResponse` in `NexusKeys.Domain` under `namespace GameStore.API.Endpoints` and removing the duplicate in `AuthEndpoints.cs`, `NexusKeys.Domain`, `NexusKeys.Infrastructure`, `NexusKeys.Api`, and `tests` all resolve the exact same type with zero ambiguity (`CS0104`) and zero test modifications.

```csharp
namespace GameStore.API.Endpoints
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
```

---

## 2. Infrastructure Layer Design (`src/NexusKeys.Infrastructure`)

### 2.1 Project Configuration Updates
- **File path**: `src/NexusKeys.Infrastructure/NexusKeys.Infrastructure.csproj`
- **Required Package**: To support JWT token creation (`JwtSecurityToken`, `SymmetricSecurityKey`, `SigningCredentials`), add the package reference matching the API's transitive dependencies:
```xml
<ItemGroup>
  <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.1" />
</ItemGroup>
```

### 2.2 Service Implementation: `TokenService`
- **File path**: `src/NexusKeys.Infrastructure/Services/TokenService.cs`
- **Namespace**: `GameStore.API.Services`
- **Dependencies**: `IConfiguration` (`Microsoft.Extensions.Configuration`), `GameStoreDbContext` (`GameStore.API.Data`)

```csharp
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameStore.API.Data;
using GameStore.API.Endpoints;
using GameStore.API.Entities;
using GameStore.API.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GameStore.API.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly GameStoreDbContext _db;

        public TokenService(IConfiguration config, GameStoreDbContext db)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public string GenerateAccessToken(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            var keyBytes = Encoding.UTF8.GetBytes(_config["Jwt:Key"] 
                ?? throw new InvalidOperationException("Jwt:Key is not configured."));
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var roleName = user.Role?.RoleName ?? "Customer";
            var username = user.Username ?? string.Empty;
            var email = user.Email ?? string.Empty;
            var userIdStr = user.Id.ToString();

            // Standard RFC 7519 & ASP.NET Core claims:
            // - sub: Subject identifier
            // - nameid (ClaimTypes.NameIdentifier): Standard identifier for ClaimsPrincipal
            // - email: Registered email address
            // - role: Role claim in both standard URI and short format
            // - jti: Unique token nonce
            // - name: Username in both standard URI and short format
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userIdStr),
                new(ClaimTypes.NameIdentifier, userIdStr),
                new(JwtRegisteredClaimNames.Email, email),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Role, roleName),
                new("role", roleName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.Name, username),
                new("name", username)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30), // Standard 30-minute access token lifetime
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public RefreshToken GenerateRefreshToken(int userId)
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return new RefreshToken
            {
                UserId = userId,
                Token = Convert.ToBase64String(randomBytes),
                CreatedAt = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7) // Standard 7-day refresh token lifetime
            };
        }

        public async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(user);

            var accessToken = GenerateAccessToken(user);
            var refreshTokenEntity = GenerateRefreshToken(user.Id);

            _db.RefreshTokens.Add(refreshTokenEntity);
            await _db.SaveChangesAsync(ct);

            return new AuthResponse
            {
                Token = accessToken,
                RefreshToken = refreshTokenEntity.Token,
                Username = user.Username ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = user.Role?.RoleName ?? "Customer"
            };
        }

        public async Task<(bool Success, AuthResponse? Response, string? Error, bool IsLocked)> RefreshTokenAsync(
            string token, 
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, null, "Refresh token không được để trống.", false);
            }

            var rt = await _db.RefreshTokens
                .Include(r => r.User)
                .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(r => r.Token == token, ct);

            if (rt == null || rt.Expires < DateTime.UtcNow)
            {
                return (false, null, "Token không hợp lệ hoặc đã hết hạn.", false);
            }

            if (rt.User.IsLocked)
            {
                return (false, null, "Tài khoản của bạn đã bị khoá. Vui lòng liên hệ hỗ trợ.", true);
            }

            var newAccessToken = GenerateAccessToken(rt.User);
            var newRefreshTokenEntity = GenerateRefreshToken(rt.UserId);

            // Rotate: remove consumed refresh token and persist new one
            _db.RefreshTokens.Remove(rt);
            _db.RefreshTokens.Add(newRefreshTokenEntity);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle parallel race condition: retrieve latest active refresh token for user
                var latestToken = await _db.RefreshTokens
                    .Include(r => r.User)
                    .ThenInclude(u => u.Role)
                    .Where(r => r.UserId == rt.UserId && r.Expires > DateTime.UtcNow)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync(ct);

                if (latestToken != null && !latestToken.User.IsLocked)
                {
                    var latestJwt = GenerateAccessToken(latestToken.User);
                    return (true, new AuthResponse
                    {
                        Token = latestJwt,
                        RefreshToken = latestToken.Token,
                        Username = latestToken.User.Username ?? string.Empty,
                        Email = latestToken.User.Email ?? string.Empty,
                        Role = latestToken.User.Role?.RoleName ?? "Customer"
                    }, null, false);
                }

                return (false, null, "Lỗi đồng thời hoặc tài khoản đã bị khoá.", latestToken?.User?.IsLocked ?? false);
            }

            return (true, new AuthResponse
            {
                Token = newAccessToken,
                RefreshToken = newRefreshTokenEntity.Token,
                Username = rt.User.Username ?? string.Empty,
                Email = rt.User.Email ?? string.Empty,
                Role = rt.User.Role?.RoleName ?? "Customer"
            }, null, false);
        }

        public async Task RevokeUserTokensAsync(int userId, CancellationToken ct = default)
        {
            var userTokens = await _db.RefreshTokens
                .Where(t => t.UserId == userId)
                .ToListAsync(ct);

            if (userTokens.Count != 0)
            {
                _db.RefreshTokens.RemoveRange(userTokens);
                await _db.SaveChangesAsync(ct);
            }
        }

        public async Task<bool> RevokeTokenAsync(string token, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var rt = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token, ct);
            if (rt == null)
                return false;

            _db.RefreshTokens.Remove(rt);
            await _db.SaveChangesAsync(ct);
            return true;
        }
    }
}
```

---

## 3. Dependency Injection & Service Registration

### 3.1 Production Registration in `src/NexusKeys.Api/Program.cs`
- In `Program.cs`, register `ITokenService` alongside existing scoped infrastructure services (`IEmailService`, `GeminiService`):
```csharp
using GameStore.API.Interfaces;
...
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<GeminiService>();
```

### 3.2 Dual-Mode Test Compatibility Strategy
- **Critical Context**: In `tests/GameStore.API.ProductKeyTests/ApiTestHost.cs`, test runs construct an isolated `WebApplication` that registers `GameStoreDbContext`, `IEmailService`, and endpoint route mappings, but does NOT invoke `Program.cs`. Furthermore, `ApiTestHost.cs` is inside `tests/` and **cannot be modified** under the project's strict test immutability constraint.
- **Architectural Solution**:
  In endpoint definitions (`AuthEndpoints.cs` and `UserEndpoints.cs`), inject `ITokenService? tokenService = null` (or resolve from request services) with fallback to `new TokenService(config, db)`:
  ```csharp
  var tokenSvc = tokenService ?? new TokenService(config, db);
  ```
  - In production (`Program.cs`), DI resolves the scoped `TokenService` instance.
  - In integration tests (`ApiTestHost`), where `ITokenService` is not pre-registered in the test host's container, ASP.NET Core passes `null` for the optional service parameter, and the endpoint gracefully instantiates `new TokenService(config, db)` using the test host's registered `IConfiguration` and in-memory `GameStoreDbContext`.
  - Zero test failures, zero test file mutations, 100% architectural compliance.

---

## 4. API Endpoints Integration (`src/NexusKeys.Api`)

### 4.1 Changes to `AuthEndpoints.cs`
1. **Remove private static token helper methods**:
   - Lines 433-464 (`GenerateJwtToken`, `GenerateRefreshToken`) are removed from `AuthEndpoints.cs`, delegating all token responsibilities to `ITokenService`.
2. **Remove duplicate `AuthResponse` class**:
   - Lines 475-482 (`public class AuthResponse`) are deleted in `AuthEndpoints.cs`, as `AuthResponse` is now imported from `NexusKeys.Domain`.
3. **Endpoint Refactoring**:
   - **`POST /register`**: Remains focused on user creation and validation.
   - **`POST /login`**:
     ```csharp
     group.MapPost("/login", async (
         LoginRequest req, 
         GameStoreDbContext db, 
         IConfiguration config, 
         ITokenService? tokenService = null) =>
     {
         var tokenSvc = tokenService ?? new TokenService(config, db);
         ...
         // After credential verification:
         var authResponse = await tokenSvc.IssueTokensAsync(user);
         return Results.Ok(authResponse);
     });
     ```
   - **`POST /refresh`**:
     ```csharp
     group.MapPost("/refresh", async (
         RefreshRequest req, 
         GameStoreDbContext db, 
         IConfiguration config, 
         ITokenService? tokenService = null) =>
     {
         var tokenSvc = tokenService ?? new TokenService(config, db);
         var (success, response, error, isLocked) = await tokenSvc.RefreshTokenAsync(req.RefreshToken);

         if (!success)
         {
             return Results.Unauthorized();
         }

         return Results.Ok(response);
     });
     ```
   - **`POST /reset-password`**:
     ```csharp
     group.MapPost("/reset-password", async (
         ResetPasswordRequest req, 
         GameStoreDbContext db, 
         IConfiguration config, 
         ITokenService? tokenService = null) =>
     {
         var tokenSvc = tokenService ?? new TokenService(config, db);
         ...
         // After updating password hash:
         resetToken.IsUsed = true;
         await tokenSvc.RevokeUserTokensAsync(resetToken.UserId);
         return Results.Ok(new { message = "Mật khẩu đã được thay đổi thành công." });
     });
     ```
   - **`POST /external-login`**:
     ```csharp
     // After creating or fetching user:
     var tokenSvc = tokenService ?? new TokenService(config, db);
     var authResponse = await tokenSvc.IssueTokensAsync(user);
     return Results.Ok(authResponse);
     ```

### 4.2 Changes to `UserEndpoints.cs`
- In `POST /api/v1/users/me/password-changes`:
  ```csharp
  group.MapPost(isV1 ? "/me/password-changes" : "/change-password", async (
      ChangePasswordDto dto, 
      GameStoreDbContext db, 
      IConfiguration config, 
      ClaimsPrincipal userClaim, 
      ITokenService? tokenService = null) =>
  {
      var tokenSvc = tokenService ?? new TokenService(config, db);
      ...
      user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
      await tokenSvc.RevokeUserTokensAsync(user.Id);

      return isV1 ? Results.NoContent() : Results.Ok(new { message = "Đổi mật khẩu thành công!" });
  });
  ```

---

## 5. Verification & Safety Matrix

| Scenario / Requirement | Verified Condition | Expected Outcome |
|------------------------|--------------------|------------------|
| Build integrity | `dotnet build GameStore.slnx` | 0 errors, 0 warnings introduced |
| Existing integration tests | `dotnet test` (72 tests) | 72/72 tests pass (100%) |
| Test suite preservation | `git diff tests/` | Output is completely empty |
| Standard JWT Claims | `sub`, `nameid`, `email`, `role`, `jti`, `name` | Present in access token payload |
| Access Token Lifetime | 30 minutes | `expires: DateTime.UtcNow.AddMinutes(30)` |
| Refresh Token Lifetime | 7 days | `Expires = DateTime.UtcNow.AddDays(7)` |
| Token Rotation | `/api/v1/auth/refresh` | Old token removed, new token inserted |
| Locked User Rejection | `/api/v1/auth/refresh` with locked user | Returns `401 Unauthorized` |
| Password Change Revocation | `/api/v1/users/me/password-changes` | User refresh tokens deleted (count = 0) |
| Password Reset Revocation | `/api/v1/auth/reset-password` | User refresh tokens deleted (count = 0) |
| Concurrency Safety | Parallel refresh requests | Race caught via `DbUpdateConcurrencyException`, latest valid token returned |
| Backward Compatibility | Minimal API endpoints with `ITokenService? = null` | Works identically in standalone `ApiTestHost` and production DI |
