# Handoff Report — Token & Authentication Service Design (Milestone 1)

## 1. Observation
- **O1. Solution and Test Suite Baseline**:
  - Command: `dotnet test`
  - Output: `Passed! - Failed: 0, Passed: 72, Skipped: 0, Total: 72, Duration: 15 s - GameStore.API.ProductKeyTests.dll (net10.0)`.
  - All 72 existing integration tests are currently green.
- **O2. Test Constraints and ApiTestHost Setup**:
  - File: `tests/GameStore.API.ProductKeyTests/ApiTestHost.cs` (lines 50-67).
  - Verbatim code:
    ```csharp
    builder.Services.AddDbContext<GameStoreDbContext>(options =>
        options
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<VnPayService>();
    builder.Services.AddScoped<MoMoService>();
    builder.Services.AddSingleton<IEmailService, FakeEmailService>();
    builder.Services.AddScoped<V1ProblemDetailsFilter>();
    builder.Services.AddProblemDetails();
    builder.Services.AddSignalR();
    ```
  - `ApiTestHost` instantiates its own `WebApplication` without executing `Program.cs`. Because the project rule strictly forbids editing any test files (`tests/`), any service added must either be registered or handled via optional injection (`ITokenService? tokenService = null`) with fallback.
- **O3. Current Token Generation in AuthEndpoints.cs**:
  - File: `src/NexusKeys.Api/Endpoints/Identity/AuthEndpoints.cs` (lines 433-464).
  - Token generation is embedded directly inside static helper methods:
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
  - Missing claims: `ClaimTypes.NameIdentifier` (`nameid`), `name` claim (for standard JWT readers), and fallback mapping.
- **O4. Existing Domain and Infrastructure Structure**:
  - `src/NexusKeys.Domain`: Contains entities in `namespace GameStore.API.Entities` (`User.cs`, `RefreshToken.cs`, `Role.cs`, `PasswordResetToken.cs`). No interfaces currently exist in Domain.
  - `src/NexusKeys.Infrastructure`: Contains `EmailService.cs` (in `namespace GameStore.API.Services` implementing `IEmailService`), `GameStoreDbContext` (in `namespace GameStore.API.Data`), and payment services.
  - `src/NexusKeys.Infrastructure/NexusKeys.Infrastructure.csproj`: Does not currently reference `System.IdentityModel.Tokens.Jwt`, which is required to compile `TokenService`.
- **O5. AuthResponse Usage Across the Project**:
  - `AuthResponse` is currently declared in `src/NexusKeys.Api/Endpoints/Identity/AuthEndpoints.cs` (lines 475-482).
  - In `tests/GameStore.API.ProductKeyTests/AuthAndUserApiTests.cs` (lines 3-4, 67, 116), tests import `GameStore.API.Endpoints` and `GameStore.API.Entities` and deserialize `ReadFromJsonAsync<AuthResponse>()`.
  - Declaring `AuthResponse` under `GameStore.API.Entities` in addition to `GameStore.API.Endpoints` would trigger `CS0104` (ambiguous reference) in `AuthAndUserApiTests.cs`. Defining `AuthResponse` once in `NexusKeys.Domain` under `namespace GameStore.API.Endpoints` completely prevents ambiguity.

---

## 2. Logic Chain
1. **From O1 & O2 (Test suite and ApiTestHost immutability)**:
   - Because `ApiTestHost.cs` builds its own service provider without calling `Program.cs`, requiring `ITokenService` unconditionally in Minimal API routes would cause `InvalidOperationException` during test runs unless `ITokenService? tokenService = null` is used with a fallback `tokenService ?? new TokenService(config, db)`.
   - In production (`Program.cs`), `builder.Services.AddScoped<ITokenService, TokenService>()` is registered, allowing full DI.
2. **From O3 (Current token generation and claims)**:
   - Claims must include `sub`, `ClaimTypes.NameIdentifier` (`nameid`), `email`, `role`, `jti`, and `name`.
   - `OrderEndpoints.ClaimsPrincipalExtensions.GetUserId()` checks `user.FindFirst(ClaimTypes.NameIdentifier)` before `Sub`. Adding `ClaimTypes.NameIdentifier` resolves user IDs on the primary path.
   - Access token expiration must remain 30 minutes, and refresh token expiration 7 days with 32-byte cryptographic entropy.
3. **From O4 (Project layers and package dependencies)**:
   - `ITokenService` belongs in `src/NexusKeys.Domain/Interfaces/ITokenService.cs` under `namespace GameStore.API.Interfaces`.
   - `TokenService` belongs in `src/NexusKeys.Infrastructure/Services/TokenService.cs` under `namespace GameStore.API.Services`.
   - `NexusKeys.Infrastructure.csproj` must add `<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.1" />` (matching existing cache and API transitive dependencies).
4. **From O5 (AuthResponse location and ambiguity prevention)**:
   - Placing `AuthResponse` in `src/NexusKeys.Domain/Interfaces/AuthResponse.cs` with `namespace GameStore.API.Endpoints` allows `ITokenService` to use it in its signatures, removes the need for duplicate declarations in `AuthEndpoints.cs`, and preserves exact compatibility with `AuthAndUserApiTests.cs`.
5. **Synthesis**:
   - The detailed design in `service_design.md` provides all exact signatures, method bodies, DI configuration, and test compatibility measures necessary for implementation by the coder agent.

---

## 3. Caveats
- `POST /api/v1/auth/logout` is planned for Milestone 2. However, the interface `ITokenService` and implementation `TokenService` designed in Milestone 1 already include `RevokeTokenAsync(string token)` so that Milestone 2 can immediately bind the endpoint with zero interface changes.
- The fallback `tokenService ?? new TokenService(config, db)` is specifically required because `tests/GameStore.API.ProductKeyTests/ApiTestHost.cs` cannot be edited. If test file editing is ever allowed by the user in the future, `builder.Services.AddScoped<ITokenService, TokenService>()` could also be added to `ApiTestHost.cs`.

---

## 4. Conclusion
The implementation strategy for Milestone 1 is completely formulated and documented in `service_design.md`:
1. `src/NexusKeys.Domain/Interfaces/ITokenService.cs` created with full contract (`GenerateAccessToken`, `GenerateRefreshToken`, `IssueTokensAsync`, `RefreshTokenAsync`, `RevokeUserTokensAsync`, `RevokeTokenAsync`).
2. `src/NexusKeys.Domain/Interfaces/AuthResponse.cs` created under `namespace GameStore.API.Endpoints` to avoid `CS0104` ambiguity and allow clean domain signatures.
3. `src/NexusKeys.Infrastructure/NexusKeys.Infrastructure.csproj` updated with `System.IdentityModel.Tokens.Jwt` `8.0.1`.
4. `src/NexusKeys.Infrastructure/Services/TokenService.cs` implemented with all standard claims (`sub`, `nameid`, `email`, `role`, `jti`, `name`), 30-min lifespan, 7-day refresh token, rotation with race recovery, and token revocation.
5. Production DI registered in `src/NexusKeys.Api/Program.cs` via `builder.Services.AddScoped<ITokenService, TokenService>()`.
6. Endpoints in `AuthEndpoints.cs` and `UserEndpoints.cs` wired using optional parameter fallback to maintain 100% pass on all 72 existing tests.

---

## 5. Verification Method
1. **Compilation Check**:
   ```bash
   dotnet build GameStore.slnx
   ```
   Must produce 0 errors and 0 warnings.
2. **Integration Test Suite Execution**:
   ```bash
   dotnet test GameStore.slnx
   ```
   All 72 tests in `GameStore.API.ProductKeyTests` must pass (100% success rate).
3. **Specific Auth & User Test Verification**:
   ```bash
   dotnet test --filter "FullyQualifiedName~AuthAndUserApiTests"
   dotnet test --filter "FullyQualifiedName~ImprovementRegressionTests"
   ```
   Verifies:
   - `Login_ValidCredentials_ReturnsTokensAndPersistsRefreshToken`
   - `Refresh_ValidToken_RotatesRefreshToken`
   - `RefreshToken_LockedUser_IsRejected`
   - `ChangePassword_RevokesExistingRefreshTokens`
   - `ResetPassword_RevokesExistingRefreshTokens`
4. **Test Immutability Inspection**:
   ```bash
   git status tests/
   ```
   Must show no modifications to any file in `tests/`.
