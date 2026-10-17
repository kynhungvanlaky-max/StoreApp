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

        public TokenService(GameStoreDbContext db, IConfiguration config)
            : this(config, db)
        {
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
