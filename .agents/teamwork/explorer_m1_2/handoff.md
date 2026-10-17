# Handoff Report - Milestone 2 API Endpoint Refactoring and Hardening Design

**Agent:** `explorer_m1_2`  
**Date:** 2026-10-16  
**Type:** Hard Handoff (Task Complete)  
**Target:** Orchestrator (`87c86cfa-dbae-4bae-80f6-a7bb236db1d5`) / Workers

---

## 1. Observation

1. **`AuthEndpoints.cs` (`src/NexusKeys.Api/Endpoints/Identity/AuthEndpoints.cs`):**
   - Lines 25-60 (`POST /register`): Performs basic validation (`req.Username.Length < 3`, `!req.Email.Contains("@")`, `req.Password.Length < 6`, `db.Users.AnyAsync(u => u.Email == req.Email)`, `req.SecretCode == "NEXUSADMIN"`). Lacks username duplicate check (`u.Username == req.Username`) and lacks input string trimming (`.Trim()`).
   - Lines 63-139 (`POST /login`): Rejects locked user with HTTP 400 ProblemDetails (lines 81-90). Employs private static methods `GenerateJwtToken` (lines 433-456) and `GenerateRefreshToken` (lines 458-464), manually manipulating `RefreshToken` in DbContext.
   - Lines 142-199 (`POST /refresh`): Contains ~57 lines of low-level database operations for token rotation and concurrency handling (`DbUpdateConcurrencyException`).
   - Lines 245-273 (`POST /reset-password`): Executes direct entity removal:
     ```csharp
     var userTokens = await db.RefreshTokens.Where(t => t.UserId == resetToken.UserId).ToListAsync();
     if (userTokens.Any())
     {
         db.RefreshTokens.RemoveRange(userTokens);
     }
     await db.SaveChangesAsync();
     ```
   - Lines 276-393 (`POST /external-login`): Duplicates token generation logic and direct `RefreshToken` creation.
   - Lines 396-430: Legacy `/change-password` endpoint (mapped only when `!EndpointRouteConventions.IsV1(apiPrefix)`).
   - Entire file contains **no logout endpoint** (`POST /api/v1/auth/logout` is missing).

2. **`UserEndpoints.cs` (`src/NexusKeys.Api/Endpoints/Identity/UserEndpoints.cs`):**
   - Lines 141-173 (`POST /me/password-changes` on V1): Validates old password with BCrypt, updates `user.PasswordHash`, and directly queries and removes user tokens from `db.RefreshTokens`:
     ```csharp
     var userTokens = await db.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
     if (userTokens.Any())
     {
         db.RefreshTokens.RemoveRange(userTokens);
     }
     await db.SaveChangesAsync();
     ```
   - Line 170: Returns `isV1 ? Results.NoContent() : Results.Ok(new { message = "Đổi mật khẩu thành công!" });`.

3. **`RestRouteContractTests.cs` (`tests/GameStore.API.ProductKeyTests/RestRouteContractTests.cs`):**
   - Lines 89-91 & 100:
     ```csharp
     Assert.Contains(routes, route => route.Method == "PATCH" && route.Route == "/api/v1/users/{id:int}");
     Assert.Contains(routes, route => route.Method == "POST" && route.Route == "/api/v1/auth/register");
     Assert.Contains(routes, route => route.Method == "POST" && route.Route == "/api/v1/auth/login");
     Assert.Contains(routes, route => route.Method == "POST" && route.Route == "/api/v1/users/me/password-changes");
     ```
   - Lines 113-117:
     ```csharp
     Assert.DoesNotContain(routes, route => route.Route.StartsWith("/api/v1", StringComparison.Ordinal)
         && route.Route.Contains("toggle-lock", StringComparison.Ordinal));
     Assert.DoesNotContain(routes, route => route.Route.StartsWith("/api/v1", StringComparison.Ordinal)
         && route.Route.Contains("vnpay-return", StringComparison.Ordinal));
     Assert.DoesNotContain(routes, route => route.Route == "/api/v1/auth/change-password");
     ```
   - Adding `POST /api/v1/auth/logout` does not conflict with any `DoesNotContain` assertion.

4. **Integration Test Fixture (`tests/GameStore.API.ProductKeyTests/ApiTestHost.cs`):**
   - Lines 50-66: `ApiTestHost.StartAsync()` registers:
     `GameStoreDbContext` (In-Memory), `HttpClient`, `VnPayService`, `MoMoService`, `IEmailService`, `V1ProblemDetailsFilter`, `ProblemDetails`, `SignalR`, `Authentication`, `Authorization`.
   - `ITokenService` and `TokenService` are **not registered** in `ApiTestHost.cs`.
   - Modifying `ApiTestHost.cs` is strictly prohibited by project constraints.

5. **ProblemDetails Middleware & Filter:**
   - In `src/NexusKeys.Api/Endpoints/Common/EndpointRouteConventions.cs:18-57`:
     `V1ProblemDetailsFilter` automatically intercepts responses with HTTP status >= 400 and converts `{ message = "..." }` or text into `Results.Problem(statusCode, title, detail)` with `Content-Type: application/problem+json`.

---

## 2. Logic Chain

1. **Service Delegation:** From Observation 1 and 2, `AuthEndpoints.cs` and `UserEndpoints.cs` contain duplicated token generation, rotation, and revocation queries. By extracting these operations to `ITokenService`, endpoints become clean Minimal API adapters that delegate token lifecycle logic to the service layer.
2. **Resilient Resolution Pattern:** From Observation 4, `ApiTestHost.cs` does not register `ITokenService`. If an endpoint used mandatory parameter injection (`async (..., ITokenService tokenService)`), `AuthAndUserApiTests` would fail with `InvalidOperationException: No service for type 'ITokenService' has been registered.`. By implementing:
   ```csharp
   private static ITokenService ResolveTokenService(IServiceProvider sp, GameStoreDbContext db, IConfiguration config) =>
       sp.GetService<ITokenService>() ?? new TokenService(config, db);
   ```
   both production DI (registered in `Program.cs`) and integration test fixtures (`ApiTestHost.cs`) work seamlessly without modifying any test code.
3. **Register Validation Hardening:** From Observation 1 and 5, trimming `Username`, `Email`, and `SecretCode`, and adding duplicate username validation (`u.Username.ToLower() == username.ToLower()`) returning `Results.BadRequest(new { message = "Tên người dùng đã được sử dụng." })` ensures immediate interception by `V1ProblemDetailsFilter`, producing standard `application/problem+json` responses as required by R1 and R3.
4. **Logout Endpoint Design:** From Observation 1, 3, and R1/R2, `POST /api/v1/auth/logout` fills the missing session termination capability. Accepting either an explicit refresh token in the body (`LogoutRequest`) or an authenticated user principal (`user.GetUserId()`) ensures support for both single-device logout and token revocation upon session closure. Returning `204 NoContent` on V1 matches the REST convention established by `/api/v1/users/me/password-changes`.
5. **Route Contract Safety:** From Observation 3, the new `/api/v1/auth/logout` endpoint does not violate any `RestRouteContractTests.cs` exclusion filters (`toggle-lock`, `vnpay-return`, `auth/change-password`), and all required canonical routes remain intact.

---

## 3. Caveats

- **Scope Boundary:** This investigation and design was strictly read-only. No source or test files were modified.
- **Service Layer Dependency:** Milestone 2 endpoint implementation depends on `ITokenService` in `NexusKeys.Domain` and `TokenService` in `NexusKeys.Infrastructure` designed in Milestone 1 (`explorer_m1_1`). The DTO `TokenResult` in Domain is mapped to `AuthResponse` in `GameStore.API.Endpoints` to avoid inner-layer dependency on the API project.
- **RFC 7009 Token Revocation Idempotency:** The logout endpoint treats revoking an already-revoked or non-existent token as a successful operation (idempotent), returning 204 NoContent, rather than leaking token status via 404 or 400.

---

## 4. Conclusion

The API endpoint refactoring and hardening plan for Milestone 2 is complete, verified, and documented in detail in `endpoint_design.md`:
1. `AuthEndpoints.cs` and `UserEndpoints.cs` will delegate token issuance, refresh rotation, and revocation to `ITokenService` using the resilient resolution pattern.
2. `POST /api/v1/auth/register` is hardened with input trimming, case-insensitive duplicate username and email checking, privilege escalation prevention, and RFC 7807 ProblemDetails compliance.
3. `POST /api/v1/auth/logout` is designed to support refresh token revocation and authenticated user revocation, returning `204 NoContent` on V1.
4. `RestRouteContractTests.cs` contract compliance is fully preserved with 0 regressions.

---

## 5. Verification Method

To verify the design once implemented by the Worker:

1. **Build verification:**
   ```powershell
   dotnet build GameStore.slnx
   ```
   *Expected:* Build succeeded with 0 errors and 0 warnings.

2. **Route Contract verification:**
   ```powershell
   dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --filter "FullyQualifiedName~RestRouteContractTests"
   ```
   *Expected:* Passed! Total tests: 1, Passed: 1, Failed: 0.

3. **Auth & User integration tests verification:**
   ```powershell
   dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --filter "FullyQualifiedName~AuthAndUserApiTests"
   ```
   *Expected:* Passed! Total tests: 11, Passed: 11, Failed: 0.

4. **Security & Regression tests verification:**
   ```powershell
   dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --filter Category=Improvement
   ```
   *Expected:* Passed! Total tests: 11, Passed: 11, Failed: 0.

5. **Entire test suite verification:**
   ```powershell
   dotnet test GameStore.slnx
   ```
   *Expected:* Passed! Total tests: 72, Passed: 72, Failed: 0.
