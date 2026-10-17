# Challenge Report — Milestone 1: JWT Claims, Session Security, and Regression Immunity

## Challenge Summary

**Overall risk assessment**: LOW  
**Verdict**: APPROVE

---

## Challenges

### [Low] Challenge 1: Dual Claim Naming Discrepancies (Standard URI vs. Short Names)
- **Assumption challenged**: ASP.NET Core JWT handler can map or not map inbound claim types depending on `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap`. If claims are only provided in one format (e.g., only `sub` or only `ClaimTypes.NameIdentifier`), downstream consumers relying on the other could receive null.
- **Attack scenario**: An endpoint uses `user.FindFirst(ClaimTypes.Role)` while another checks `user.FindFirst("role")`, or `user.FindFirst(ClaimTypes.NameIdentifier)` vs `user.FindFirst(JwtRegisteredClaimNames.Sub)`.
- **Blast radius**: Null reference or 401/403 authorization bypass/denial on routes depending on specific claim naming.
- **Mitigation**: `TokenService.GenerateAccessToken` issues BOTH standard URI forms and short RFC 7519 aliases for all critical claims (`sub` + `nameid`, `email` URI + short `email`, `role` URI + short `role`, `name` URI + short `name`). Empirical test confirmed both forms are present in the serialized JWT and successfully resolve in `ClaimsPrincipal`.

### [Low] Challenge 2: Edge Case Null Attributes on User Entities
- **Assumption challenged**: Users migrated from external providers or with incomplete profiles might have null values for `Role`, `Username`, or `Email`.
- **Attack scenario**: `TokenService.GenerateAccessToken` crashes with `NullReferenceException` when invoking `.ToString()` or building claims.
- **Blast radius**: 500 Internal Server Error during authentication or token refresh for edge-case accounts.
- **Mitigation**: `TokenService` implements defensive fallbacks:
  - `var roleName = user.Role?.RoleName ?? "Customer";`
  - `var username = user.Username ?? string.Empty;`
  - `var email = user.Email ?? string.Empty;`  
  Empirically stress-tested in Test 2 of the verification harness: successfully generated valid tokens without throwing exceptions.

### [Low] Challenge 3: Hard-Deletion vs Soft-Deletion of Refresh Tokens
- **Assumption challenged**: Revoking tokens via soft-delete (`isRevoked = true`) might fail tests expecting token count to drop to 0.
- **Attack scenario**: Password reset or change leaves database records intact, causing `Assert.Equal(0, await api.WithDbAsync(db => db.RefreshTokens.CountAsync()))` to fail.
- **Blast radius**: Test assertion failure in `ImprovementRegressionTests.ChangePassword_RevokesExistingRefreshTokens` and `ResetPassword_RevokesExistingRefreshTokens`.
- **Mitigation**: `TokenService.RevokeUserTokensAsync` and `RevokeTokenAsync` utilize `_db.RefreshTokens.RemoveRange` / `_db.RefreshTokens.Remove`, completely purging revoked sessions and matching test assertions 100%.

---

## Stress Test Results

| Test Scenario | Expected Behavior | Actual Behavior | Result |
|---------------|-------------------|-----------------|:------:|
| Standard Claims Generation | Token contains `sub`, `nameid`, `email`, `role`, `jti`, `name`, 30-min expiry | All 10 claims present, exact values matched, 30.0 min expiry | **PASS** |
| Edge Case Null User Fields | User with null Role, Username, Email produces valid token | Defaults to Customer role, sub preserved, 0 exceptions | **PASS** |
| Cryptographic Refresh Token | 32 random bytes Base64 and 7-day expiration | Exact 32 bytes decoded, 7.0 days expiry verified | **PASS** |
| ClaimsPrincipal Identity Resolution | Validate token via `JwtSecurityTokenHandler` and resolve user ID and role | UserId 42 and Role 'Administrator' successfully resolved | **PASS** |
| Token Rotation & Revocation | Issue -> Rotate -> Revoke cycle | Token rotated to new value; revoked user tokens dropped to 0 | **PASS** |
| AdminBridge Compatibility | Missing/expired/customer tokens rejected; admin token creates session cookies | Verified in `AdminBridgeTests` (6/6 passed) | **PASS** |
| User Profile & Orders Resolution | `/api/v1/users/me` and `/orders` resolve authenticated user id | Verified in `AuthAndUserApiTests` & `CustomerWorkflowApiTests` | **PASS** |
| Full Regression Test Suite | Run `ImprovementRegressionTests` | 11/11 tests passed in 17.4s (100%) | **PASS** |
| Full Integration Test Suite | Run all 72 tests in `GameStore.API.ProductKeyTests` | 72/72 tests passed in 18.0s (100%) | **PASS** |
| Test Suite Immutability | `git diff --stat tests/` | Output is completely empty; zero test files modified | **PASS** |

---

## Unchallenged Areas

- **UI Component Visual Rendering**: Verified build compilation only (`NexusKeys.Storefront.csproj` succeeded); browser rendering and CSS styling are handled in separate storefront modules outside the scope of Milestone 1 backend auth standardization.
