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
