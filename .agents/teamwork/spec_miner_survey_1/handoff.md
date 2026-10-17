# Handoff Report: Specification Mining for Authentication & User Identity API

- **Agent**: `spec_miner_survey_1` (Specification Miner)
- **Recipient**: Orchestrator / Parent Agent (`87c86cfa-dbae-4bae-80f6-a7bb236db1d5`)
- **Working Directory**: `c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\spec_miner_survey_1`
- **Specification Report Path**: `c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\spec_miner_survey_1\spec_report.md`
- **Handoff Type**: Hard (Task complete)

---

## 1. Observation

1. **Test Solution and Execution Baseline**:
   - Running `dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj` executed 72 tests across 8 test suites:
     `Passed! - Failed: 0, Passed: 72, Skipped: 0, Total: 72, Duration: 25 s`
   - Single test project in solution: `tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj` referencing `NexusKeys.Api`, `NexusKeys.Admin`, `NexusKeys.Domain`, `NexusKeys.Infrastructure`.
   - Zero test files were modified or deleted, preserving test suite integrity.

2. **Auth & Identity Test Coverage**:
   - `tests/GameStore.API.ProductKeyTests/AuthAndUserApiTests.cs`:
     - Line 12-34: `Register_InvalidInput_ReturnsProblemDetails` asserts 400 Bad Request with `Content-Type: application/problem+json` for usernames < 3 chars, invalid emails (missing `@` or `.`), and passwords < 6 chars.
     - Line 36-54: `Register_ValidCustomer_CreatesUserAndReturnsLocation` asserts 201 Created with `Location` header, creates user with Role `Customer`, and hashes password with BCrypt.
     - Line 56-75: `Login_ValidCredentials_ReturnsTokensAndPersistsRefreshToken` asserts 200 OK returning `AuthResponse` with JWT token, refresh token, role, and creates 1 `RefreshToken` in database.
     - Line 77-92: `Login_LockedUser_ReturnsBadRequest` asserts 400 Bad Request with `application/problem+json` and 0 refresh tokens persisted when `User.IsLocked == true`.
     - Line 94-124: `Refresh_ValidToken_RotatesRefreshToken` asserts 200 OK returning new rotated refresh token and removes the old token from database.
     - Line 126-158: `ResetPassword_ValidToken_ChangesPasswordAndConsumesToken` asserts 200 OK, sets `PasswordResetToken.IsUsed = true`, and updates user's `PasswordHash`.
     - Line 160-173: `CurrentUserProfile_UsesAuthenticatedUserId` asserts GET `/api/v1/users/me` with `X-Test-User-Id: 2` returns `UserDto` for user ID 2.
     - Line 175-188: `ChangePassword_ValidOldPassword_ReturnsNoContentAndUpdatesHash` asserts POST `/api/v1/users/me/password-changes` returns 204 NoContent and updates `PasswordHash`.
     - Line 190-202: `AdminPatchUser_SetsExplicitLockState` asserts PATCH `/api/v1/users/1` by `Administrator` returns 204 NoContent and mutates `user.IsLocked`.

3. **Improvement Regression Tests**:
   - `tests/GameStore.API.ProductKeyTests/ImprovementRegressionTests.cs`:
     - Line 14-30: `RefreshToken_LockedUser_IsRejected` asserts POST `/api/v1/auth/refresh` on locked user returns 401 Unauthorized.
     - Line 32-49: `ChangePassword_RevokesExistingRefreshTokens` asserts POST `/api/v1/users/me/password-changes` completely deletes all existing refresh tokens for that user (`db.RefreshTokens.Count == 0`).
     - Line 51-76: `ResetPassword_RevokesExistingRefreshTokens` asserts POST `/api/v1/auth/reset-password` completely deletes all existing refresh tokens for that user (`db.RefreshTokens.Count == 0`).
     - Line 79-97: `Register_PublicRequestCannotCreateAdministrator` asserts POST `/api/v1/auth/register` with `secretCode = "NEXUSADMIN"` returns 403 Forbidden.

4. **REST Route Contracts and Deprecation**:
   - `tests/GameStore.API.ProductKeyTests/RestRouteContractTests.cs`:
     - Lines 89-112: Asserts canonical v1 routes exist: `PATCH /api/v1/users/{id:int}`, `POST /api/v1/auth/register`, `POST /api/v1/auth/login`, `POST /api/v1/users/me/password-changes`, `GET /api/v1/users/me/orders`, etc.
     - Lines 113-117: Asserts disallowed legacy routes do NOT exist under v1:
       - No `/api/v1...toggle-lock` (replaced by `PATCH /api/v1/users/{id:int}` with `{ isLocked: bool }`)
       - No `/api/v1...vnpay-return`
       - No `/api/v1/auth/change-password` (replaced by `POST /api/v1/users/me/password-changes`)
     - Lines 121-126: Asserts `Transaction` has a unique composite index on `[PaymentGateway, GatewayTransactionId]`.

5. **Admin Bridge and Session Cookies**:
   - `tests/GameStore.API.ProductKeyTests/AdminBridgeTests.cs`:
     - Lines 15-25: `Bridge` without token redirects to `/Auth/Login` without calling API.
     - Lines 27-37: `Bridge` with expired token (API returns 401) redirects to `/Auth/Login` without setting cookies.
     - Lines 39-51: `Bridge` with Customer token (API returns `roleName: "Customer"`) redirects to `/Auth/Login` without setting cookies.
     - Lines 53-64: `Bridge` with malformed profile JSON fails closed to `/Auth/Login` without setting cookies.
     - Lines 66-86: `Bridge` with Administrator token sets 3 cookies: `access_token` (HttpOnly, SameSite=Lax), `admin_name`, `admin_email` and redirects to `/Home/Index`.

6. **Implementation Alignment in `src/NexusKeys.*`**:
   - `src/NexusKeys.Api/Endpoints/Identity/AuthEndpoints.cs`:
     - JWT generated with HMAC-SHA256, 30-minute expiration, claims: `sub`, `email`, `ClaimTypes.Name`, `ClaimTypes.Role`, `jti`.
     - Refresh tokens generated as 32 cryptographically random bytes, Base64 encoded, 7-day expiration.
     - `V1ProblemDetailsFilter` in `src/NexusKeys.Api/Endpoints/Common/EndpointRouteConventions.cs` intercepts any HTTP result with status >= 400 and returns RFC 7807 `ProblemDetails`.

---

## 2. Logic Chain

1. **Authoritative Specification Source**:
   - The test suite in `tests/GameStore.API.ProductKeyTests/` runs on an isolated in-memory test host (`ApiTestHost.cs`) without external DB dependencies and tests the exact public HTTP endpoints mapped by `NexusKeys.Api`.
   - Therefore, the test suite is the single authoritative source of truth for all routes, payload contracts, status codes, and error formats.

2. **Route and Verb Canonicalization**:
   - `RestRouteContractTests.cs` explicitly verifies that REST v1 conventions are strictly obeyed. Actions on current user resources use `/users/me/...` (e.g. `/users/me/password-changes`, not `/auth/change-password`).
   - Mutations to resources use standard REST verbs: `PATCH` for partial resource updates (such as locking or profile changes) and `PUT` for complete replacement/upsert (e.g. wishlist idempotency, payment configs).
   - Any attempt to introduce custom verb routes like `/toggle-lock` will violate line 113-114 of `RestRouteContractTests.cs`.

3. **Status Codes and Error Handling**:
   - Tests assert specific status codes:
     - Account locked during login: `400 Bad Request` (not 401) with ProblemDetails and `isLocked: true`.
     - Account locked during refresh: `401 Unauthorized`.
     - Non-owner access to orders/reviews: `403 Forbidden`.
     - Deletion of sold product keys or sold products: `409 Conflict`.
     - Missing target products or resources: `404 NotFound`.
     - Successful password change in v1: `204 NoContent`.
     - Successful registration: `201 Created` with `Location` header.
   - All errors >= 400 return `application/problem+json` matching RFC 7807 via `V1ProblemDetailsFilter`.

4. **Session Security & Refresh Token Lifecycle**:
   - Refresh tokens must be rotated on every `/refresh` call (old removed, new stored).
   - Both password change (`/users/me/password-changes`) and password reset (`/auth/reset-password`) must immediately purge all active refresh tokens for that user to terminate any compromised sessions.
   - Locked accounts cannot authenticate or refresh tokens.

---

## 3. Caveats

- **Test Fixture Scheme**: `ApiTestHost` utilizes `TestAuthenticationHandler` (`X-Test-Role` and `X-Test-User-Id` headers) to bypass cryptographic token verification during integration tests, but `AdminBridgeTests` tests actual Bearer token HTTP authorization against the API. Both formats use the same underlying ClaimsPrincipal properties (`ClaimTypes.NameIdentifier`, `ClaimTypes.Role`, `ClaimTypes.Name`).
- **Legacy Route Aliases**: The API supports both legacy `/api/...` and v1 `/api/v1/...` routes. Legacy routes emit deprecation headers (`Deprecation`, `Sunset`, `Link`). New features must be bound to `/api/v1/...`.

---

## 4. Conclusion

All authoritative specifications for the Authentication and User Identity module have been completely mined and documented in `spec_report.md`.
The specifications define:
1. 13 distinct identity/user features and 16 catalog/commerce features.
2. 25 verified edge cases covering validation, cross-tenant security, token rotation, and concurrency.
3. Complete payload schemas, casing, types, validation rules, HTTP status codes, and RFC 7807 ProblemDetails structures.
4. Token claims, lifespan rules (30 min access, 7 day refresh), session revocation triggers, and RBAC matrix.

No code or test files were modified. The findings are immediately actionable for implementation, refactoring, and verification agents.

---

## 5. Verification Method

To independently verify the facts and test status documented in this report:

1. **Verify Test Suite Integrity**:
   Inspect git status to confirm no test files have been altered:
   ```powershell
   git status tests/
   ```
   *Expected outcome*: Working tree clean in `tests/`.

2. **Verify Test Suite Execution**:
   Run the complete test suite:
   ```powershell
   dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj
   ```
   *Expected outcome*: 72 passed, 0 failed, 0 skipped.

3. **Verify Auth & User Tests**:
   Run only the Auth and User test suite:
   ```powershell
   dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --filter FullyQualifiedName~AuthAndUserApiTests
   ```
   *Expected outcome*: 9 passed, 0 failed.

4. **Verify Route Contract Tests**:
   Run the route contract assertions:
   ```powershell
   dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --filter FullyQualifiedName~RestRouteContractTests
   ```
   *Expected outcome*: 1 passed, 0 failed.

5. **Inspect Artifacts**:
   Read `c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\spec_miner_survey_1\spec_report.md` for full contract details.
