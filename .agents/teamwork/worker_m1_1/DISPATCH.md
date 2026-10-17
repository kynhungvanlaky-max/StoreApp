## 2026-10-16T09:04:29Z
You are a Teamwork Worker agent.
Working directory: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\worker_m1_1
Project root: c:\Users\kynhu\Documents\DATN\datn
Original request: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\ORIGINAL_REQUEST.md
Project plan: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\orchestrator_1\PROJECT.md

Read the detailed technical design specifications produced by the Explorers before beginning:
- Service Design: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\explorer_m1_1\service_design.md
- Endpoint Design: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\explorer_m1_2\endpoint_design.md
- Test Safety & Regression Report: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\explorer_m1_3\test_safety_report.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

CRITICAL RULES:
- Tuyệt đối KHÔNG ĐƯỢC CHỈNH SỬA, XÓA, COMMENT HOẶC LÀM SUY YẾU BẤT KỲ TEST CASE NÀO trong tests/ hoặc bất kỳ file test nào.
- Thư mục `tests/` là READ-ONLY.

Your write ownership (exclusive files to edit or create):
1. `src/NexusKeys.Domain/Interfaces/ITokenService.cs` (Create)
2. `src/NexusKeys.Domain/Interfaces/AuthResponse.cs` (Create)
3. `src/NexusKeys.Infrastructure/NexusKeys.Infrastructure.csproj` (Add Microsoft.AspNetCore.Authentication.JwtBearer package)
4. `src/NexusKeys.Infrastructure/Services/TokenService.cs` (Create)
5. `src/NexusKeys.Api/Program.cs` (Register ITokenService)
6. `src/NexusKeys.Api/Endpoints/Identity/AuthEndpoints.cs` (Wire ITokenService, add POST /api/v1/auth/logout, harden register validation, remove old helpers/duplicate AuthResponse)
7. `src/NexusKeys.Api/Endpoints/Identity/UserEndpoints.cs` (Wire ITokenService for password change token revocation)

CRITICAL IMPLEMENTATION DETAILS:
1. DI Hazarding protection for test host:
   In Minimal API endpoints (`AuthEndpoints.cs` and `UserEndpoints.cs`), ALWAYS resolve `ITokenService` with fallback:
   `var tokenService = sp.GetService<ITokenService>() ?? new TokenService(config, db);`
   (or `new TokenService(config, db)`), because `ApiTestHost.cs` does not register `ITokenService` and cannot be modified!
2. Standard Claims:
   `TokenService.GenerateAccessToken` must include `sub`, `ClaimTypes.NameIdentifier` (`nameid`), `email`, `role`, `jti`, and `name`. Lifespan: 30 minutes.
3. Refresh Tokens:
   7-day lifespan, 32-byte Base64 cryptographically secure random.
   Rotate on `POST /api/v1/auth/refresh` (handles concurrency).
   Revocation MUST hard-delete (`db.RefreshTokens.Remove` / `RemoveRange`) to satisfy test assertions (`db.RefreshTokens.CountAsync() == 0`).
4. HTTP Status Codes & Error Formatting:
   - Login locked user -> 400 Bad Request with ProblemDetails and `isLocked: true`.
   - Refresh locked user -> 401 Unauthorized.
   - Register invalid / duplicate -> 400 Bad Request with ProblemDetails via `V1ProblemDetailsFilter`.
   - Register secretCode == "NEXUSADMIN" -> 403 Forbidden.
   - Password changes -> 204 NoContent on v1.
   - Logout -> 204 NoContent on v1 (or 200 OK on legacy).
5. Verification:
   Run:
   - `dotnet build GameStore.slnx` -> 0 errors, 0 warnings
   - `dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj` -> 72/72 passed (100%)
   - `git diff --stat tests/` -> must be completely empty!
