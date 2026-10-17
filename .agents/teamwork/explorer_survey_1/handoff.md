# Handoff Report — Explorer Architecture Survey

**Agent**: `explorer_survey_1`  
**Working Directory**: `c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\explorer_survey_1`  
**Date**: 2026-10-16  
**Type**: Hard Handoff  

---

## 1. Observation

1. **Solution Structure**:
   - `GameStore.slnx` contains 5 projects in `src/` (`NexusKeys.Domain`, `NexusKeys.Infrastructure`, `NexusKeys.Api`, `NexusKeys.Admin`, `NexusKeys.Storefront`) and 1 test project in `tests/` (`GameStore.API.ProductKeyTests`).
   - All projects target `net10.0`.
   - `NexusKeys.Api` is configured with `<AssemblyName>GameStore.API</AssemblyName>` and `<RootNamespace>GameStore.API</RootNamespace>` (`src/NexusKeys.Api/NexusKeys.Api.csproj:7-8`).
   - `NexusKeys.Domain` entities use `namespace GameStore.API.Entities` (`src/NexusKeys.Domain/Entities/Identity/User.cs:6`).
   - `NexusKeys.Infrastructure` uses `namespace GameStore.API.Data` (`src/NexusKeys.Infrastructure/Data/GameStoreDbContext.cs:4`) and `namespace GameStore.API.Services` (`src/NexusKeys.Infrastructure/Services/EmailService.cs:5`).

2. **Build and Test Verification**:
   - Command `dotnet build GameStore.slnx` succeeded with code 0:
     ```
     Build succeeded.
         0 Warning(s)
         0 Error(s)
     ```
   - Command `dotnet test` succeeded with code 0:
     ```
     Passed!  - Failed:     0, Passed:    72, Skipped:     0, Total:    72, Duration: 23 s - GameStore.API.ProductKeyTests.dll (net10.0)
     ```

3. **Current Authentication & User Identity Implementation**:
   - Identity entities in `src/NexusKeys.Domain/Entities/Identity/`:
     - `User.cs`: Properties `Id`, `Username`, `Email`, `WalletBalance`, `RoleId`, `CreatedAt`, `PasswordHash`, `GoogleId`, `AvatarUrl`, `IsLocked`.
     - `Role.cs`: Roles include Administrator, Customer, Developer, Staff.
     - `RefreshToken.cs`: Properties `Id`, `UserId`, `Token`, `Expires`, `CreatedAt`. No `IsRevoked` or `RevokedAt` columns; tokens are hard-deleted upon rotation and password changes.
     - `PasswordResetToken.cs`: Properties `Id`, `UserId`, `Token`, `CreatedAt`, `ExpiresAt`, `IsUsed`.
   - `AuthEndpoints.cs` (`src/NexusKeys.Api/Endpoints/Identity/AuthEndpoints.cs`):
     - `POST /api/v1/auth/register` (lines 25-60): Validates username length >= 3, email contains '@' and '.', password length >= 6, checks email duplicate, forbids secretCode "NEXUSADMIN", hashes password with BCrypt, returns HTTP 201 Created with Location header. Does NOT check for duplicate username.
     - `POST /api/v1/auth/login` (lines 63-139): Checks user existence, verifies password via BCrypt, handles locked user by returning HTTP 400 ProblemDetails (required by `AuthAndUserApiTests:78-92`), generates JWT and RefreshToken, persists RefreshToken (expires in 7 days), returns HTTP 200 OK with `AuthResponse`.
     - `POST /api/v1/auth/refresh` (lines 142-199): Looks up token, checks expiry and lock state, rotates refresh token (deletes old token, adds new token), handles concurrency conflicts (`DbUpdateConcurrencyException`).
     - `POST /api/v1/auth/forgot-password` (lines 202-242): Generates 1-hour expiry reset token and sends email via `IEmailService`.
     - `POST /api/v1/auth/reset-password` (lines 245-273): Validates token not used and not expired, hashes new password, sets `IsUsed = true`, revokes existing refresh tokens via `db.RefreshTokens.RemoveRange(userTokens)`, returns HTTP 200 OK.
     - `POST /api/v1/auth/external-login` (lines 276-393): Google token validation and mock fallback.
     - Missing: No endpoint exists for Logout / Token Revocation (`/api/v1/auth/logout` or `/api/v1/auth/revoke`).
   - `UserEndpoints.cs` (`src/NexusKeys.Api/Endpoints/Identity/UserEndpoints.cs`):
     - `GET /api/v1/users/me` (lines 102-122): Authenticated profile lookup via claim `GetUserId()`.
     - `PATCH /api/v1/users/me` (lines 124-139): Updates username and avatar, returns 204 NoContent.
     - `POST /api/v1/users/me/password-changes` (lines 141-173): Validates old password with BCrypt, updates hash, revokes existing refresh tokens via `db.RefreshTokens.RemoveRange(userTokens)`, returns HTTP 204 NoContent.
     - `PATCH /api/v1/users/{id:int}` (lines 70-84): Admin updates user lock status (`IsLocked`), returns 204 NoContent.
     - `PUT /api/v1/users/{id:int}/role` (lines 86-98): Admin updates user role.

4. **JWT Claims Setup** (`src/NexusKeys.Api/Endpoints/Identity/AuthEndpoints.cs:433-456`):
   ```csharp
   var claims = new[]
   {
       new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
       new Claim(JwtRegisteredClaimNames.Email, user.Email),
       new Claim(ClaimTypes.Name, user.Username),
       new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Customer"),
       new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
   };
   ```
   Observation: `JwtRegisteredClaimNames.Sub` is included, but `ClaimTypes.NameIdentifier` (`nameid`) is not explicitly defined in the claims array.

5. **Error Handling & ProblemDetails**:
   - `V1ProblemDetailsFilter` (`src/NexusKeys.Api/Endpoints/Common/EndpointRouteConventions.cs:18-57`) intercepts all status codes >= 400 in the `/api/v1` group and converts them into `application/problem+json`.
   - `Program.cs` registers `builder.Services.AddProblemDetails` and `app.UseStatusCodePages`.

6. **Test Suite Constraints & Expectations**:
   - `tests/GameStore.API.ProductKeyTests/AuthAndUserApiTests.cs`:
     - `Login_LockedUser_ReturnsBadRequest`: Expects HTTP 400 Bad Request with ProblemDetails for locked user.
     - `ChangePassword_ValidOldPassword_ReturnsNoContentAndUpdatesHash`: Expects HTTP 204 NoContent for password change.
     - `Refresh_ValidToken_RotatesRefreshToken`: Expects old token removed, new token persisted.
   - `tests/GameStore.API.ProductKeyTests/RestRouteContractTests.cs`:
     - Asserts presence of canonical V1 routes (`/api/v1/auth/register`, `/api/v1/auth/login`, `/api/v1/users/me/password-changes`, `/api/v1/users/{id:int}`).
     - Asserts absence of `/api/v1/auth/change-password` and any `/api/v1` route containing `toggle-lock`.

---

## 2. Logic Chain

1. From Observation 1, the solution preserves the historical `GameStore.API.*` namespace structure across `NexusKeys.Domain`, `NexusKeys.Infrastructure`, and `NexusKeys.Api` to maintain compatibility with client apps and test suites. Any refactoring must preserve or alias these namespaces.
2. From Observation 2, the baseline solution is stable: 0 errors, 0 warnings, 72/72 tests passing.
3. From Observation 3 and Requirement R1:
   - Registration, login, token refresh, forgot password, reset password, and password changes are implemented and tested.
   - However, there is no API endpoint for Logout / Token Revocation. Both Storefront and Admin currently clear client cookies/storage without invalidating the active refresh token in the database.
   - In registration, only email uniqueness is checked; duplicate username is unvalidated.
4. From Observation 4 and Requirement R2:
   - `GenerateJwtToken` issues `sub`, `email`, `role`, `name`, and `jti`, but omits explicit `ClaimTypes.NameIdentifier` (`nameid`). Adding `ClaimTypes.NameIdentifier` ensures full RFC/JWT standard compliance.
   - Revocation on password change/reset is implemented via `RemoveRange`, but revocation on logout is missing.
5. From Observation 5 and Requirement R3:
   - The test suite strictly asserts certain behaviors (such as HTTP 400 for locked user login, HTTP 204 for password changes, and exact endpoint route conventions in `RestRouteContractTests`).
   - No test file in `tests/` can be modified under the critical project rules. All improvements must be fully compatible with existing assertions.

---

## 3. Caveats

- Supabase PostgreSQL database is configured in `appsettings.json`, but all automated tests run against an in-memory database (`UseInMemoryDatabase`) configured in `ApiTestHost.cs`.
- The `RefreshToken` entity model does not currently feature soft-revocation properties (`IsRevoked`, `RevokedAt`). Hard deletion is currently expected by the test `Refresh_ValidToken_RotatesRefreshToken` (`Assert.DoesNotContain("old-refresh-token", tokens)`). If soft revocation is introduced, querying active tokens must filter by revocation state, but existing test assertions checking database token lists must be preserved.
- No other caveats.

---

## 4. Conclusion

1. The existing solution is healthy and functional (0 build errors, 72/72 tests pass).
2. The core gap for **R1** is the lack of a server-side Logout / Revoke endpoint (`POST /api/v1/auth/logout`) and username duplicate validation during registration.
3. The core gap for **R2** is adding explicit `ClaimTypes.NameIdentifier` (`nameid`) to the JWT claim set and tying logout to refresh token revocation.
4. For **R3**, Clean Architecture separation can be enhanced by extracting token management and authentication services cleanly while preserving existing namespaces, route contracts, and 100% test pass rate.

---

## 5. Verification Method

To verify these observations independently:
1. Build verification:
   ```powershell
   dotnet build GameStore.slnx
   ```
   *Expected output*: `Build succeeded. 0 Warning(s) 0 Error(s)`.
2. Test verification:
   ```powershell
   dotnet test
   ```
   *Expected output*: `Passed! - Failed: 0, Passed: 72, Skipped: 0, Total: 72`.
3. Code inspection:
   - Review `src/NexusKeys.Api/Endpoints/Identity/AuthEndpoints.cs` lines 25-273 for register/login/refresh/reset flows and line 433-456 for JWT claims.
   - Review `src/NexusKeys.Api/Endpoints/Identity/UserEndpoints.cs` lines 141-173 for password change flow.
   - Review `tests/GameStore.API.ProductKeyTests/AuthAndUserApiTests.cs` and `RestRouteContractTests.cs`.
