# Project: NexusKeys Game Store - Authentication & User Identity Standardization

## Architecture
Clean Architecture solution (.NET 10.0 `GameStore.slnx`):
- **NexusKeys.Domain** (`src/NexusKeys.Domain`):
  - Domain Entities in `namespace GameStore.API.Entities`: `User`, `Role`, `RefreshToken`, `PasswordResetToken`, etc.
  - Domain Interfaces in `namespace GameStore.API.Interfaces`: `ITokenService`.
  - Shared models in `namespace GameStore.API.Endpoints`: `AuthResponse`.
- **NexusKeys.Infrastructure** (`src/NexusKeys.Infrastructure`):
  - Data access in `namespace GameStore.API.Data`: `GameStoreDbContext`, seeders, migrations.
  - Infrastructure Services in `namespace GameStore.API.Services`: `TokenService`, `EmailService`.
  - Package dependencies: `Microsoft.AspNetCore.Authentication.JwtBearer` (10.0.9).
- **NexusKeys.Api** (`src/NexusKeys.Api`):
  - Presentation & API in `namespace GameStore.API`: Minimal API endpoints (`AuthEndpoints`, `UserEndpoints`), `V1ProblemDetailsFilter`, Middleware, DI configuration in `Program.cs`.
- **NexusKeys.Admin & NexusKeys.Storefront**:
  - MVC Admin bridge and Blazor Storefront client apps.
- **GameStore.API.ProductKeyTests** (`tests/GameStore.API.ProductKeyTests`):
  - 72 integration tests verifying API contracts, auth, security, and idempotency (READ-ONLY, 100% UNTOUCHED).

## Feature Inventory
| # | Feature | Description | Milestone | Source | Status |
|---|---------|-------------|-----------|--------|--------|
| 1 | Register Validation & Standardization | Validate username (length >= 3, duplicate check, trim), email, password (>= 6), reject secretCode NEXUSADMIN with 403 Forbidden, return 201 Created with Location header, RFC 7807 ProblemDetails for errors | M2 | Survey (spec_miner, explorer_1) | DONE |
| 2 | Login Authentication | Authenticate email/password (BCrypt), reject locked users with 400 ProblemDetails (with isLocked: true extension), issue JWT Access Token and 7-day RefreshToken | M1 | Survey (spec_miner, explorer_1) | DONE |
| 3 | Standard JWT Bearer Claims | Issue claims: `sub`, `ClaimTypes.NameIdentifier` (`nameid`), `email`, `role`, `jti`, `name`. Access token lifetime 30 minutes | M1 | Survey (explorer_1, R2) | DONE |
| 4 | Refresh Token Rotation | Rotate refresh token on `POST /api/v1/auth/refresh`, issue replacement 7-day token, reject locked users with 401 Unauthorized, handle concurrency safely | M1 | Survey (spec_miner, explorer_1) | DONE |
| 5 | Token Revocation on Logout | Add endpoint `POST /api/v1/auth/logout` to revoke server-side refresh tokens | M2 | Survey (explorer_1, R1, R2) | DONE |
| 6 | Token Revocation on Password Change | Revoke all active refresh tokens for user on `POST /api/v1/users/me/password-changes` | M1 | Survey (spec_miner, explorer_2) | DONE |
| 7 | Token Revocation on Password Reset | Revoke all active refresh tokens for user on `POST /api/v1/auth/reset-password` | M1 | Survey (spec_miner, explorer_2) | DONE |
| 8 | RFC 7807 ProblemDetails Compliance | All error responses (>= 400) return `application/problem+json` with standard title, status, detail, traceId | M2 | Survey (spec_miner, explorer_1) | DONE |
| 9 | Clean Architecture Service Separation | Encapsulate token and identity operations into dedicated services (`ITokenService` / `TokenService`) | M1 | Survey (explorer_1, R3) | DONE |
| 10 | Test Preservation & Build Verification | Maintain 0 compile errors and 100% pass on all 72 existing tests in `GameStore.API.ProductKeyTests` without modifying `tests/` | M3 | Survey (explorer_2, Critical Rule) | DONE |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| 1 | Clean Architecture Auth & Token Services | Extract `ITokenService` in Domain and `TokenService` in Infrastructure; standardize JWT claims (`sub`, `nameid`, `email`, `role`, `jti`, `name`); implement rotation & revocation methods | none | DONE |
| 2 | API Endpoints & Hardening | Connect endpoints to `ITokenService`; implement `POST /api/v1/auth/logout`; harden register validation (username duplicate, trim); ensure RFC 7807 compliance | M1 | DONE |
| 3 | Verification, Audit & Test Suite Preservation | Verify `dotnet build GameStore.slnx` (0 errors), `dotnet test` (72/72 pass 100%), execute Reviewer, Challenger, and Forensic Auditor verification | M2 | DONE |

## Interface Contracts
### `ITokenService`
- `string GenerateAccessToken(User user)`: Generates JWT token with standard claims (`sub`, `ClaimTypes.NameIdentifier`, `email`, `role`, `jti`, `name`), expires in 30 minutes.
- `RefreshToken GenerateRefreshToken(int userId)`: Generates cryptographically secure 32-byte Base64 token with 7-day expiration.
- `Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct = default)`: Issues and persists token pair.
- `Task<(bool Success, AuthResponse? Response, string? Error, bool IsLocked)> RefreshTokenAsync(string token, CancellationToken ct = default)`: Rotates token with concurrency safety.
- `Task RevokeUserTokensAsync(int userId, CancellationToken ct = default)`: Purges all active refresh tokens for a user (on password change/reset).
- `Task<bool> RevokeTokenAsync(string token, CancellationToken ct = default)`: Revokes a specific refresh token (on logout).

## Code Layout
- `src/NexusKeys.Domain/`
  - `Entities/Identity/`: `User.cs`, `Role.cs`, `RefreshToken.cs`, `PasswordResetToken.cs`
  - `Interfaces/`: `ITokenService.cs`, `AuthResponse.cs`
- `src/NexusKeys.Infrastructure/`
  - `Services/`: `TokenService.cs`
  - `Data/`: `GameStoreDbContext.cs`
- `src/NexusKeys.Api/`
  - `Endpoints/Identity/`: `AuthEndpoints.cs`, `UserEndpoints.cs`
  - `Endpoints/Common/`: `EndpointRouteConventions.cs`
  - `Program.cs`
- `tests/GameStore.API.ProductKeyTests/` (READ-ONLY, ABSOLUTELY UNTOUCHED, 72/72 PASS)
