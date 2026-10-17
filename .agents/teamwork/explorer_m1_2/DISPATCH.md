## 2026-10-16T08:53:44Z
You are a Teamwork Explorer agent.
Working directory: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\explorer_m1_2
Project root: c:\Users\kynhu\Documents\DATN\datn
Original request: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\ORIGINAL_REQUEST.md
Project plan: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\orchestrator_1\PROJECT.md

CRITICAL CONSTRAINTS:
- You are strictly READ-ONLY. DO NOT edit or create any source or test files.
- Tuyệt đối KHÔNG ĐƯỢC CHỈNH SỬA, XÓA, COMMENT HOẶC LÀM SUY YẾU BẤT KỲ TEST CASE NÀO.

Your mission for Milestone 2:
Design the API endpoint refactoring and hardening in `NexusKeys.Api`:
1. Inspect `src/NexusKeys.Api/Endpoints/Identity/AuthEndpoints.cs` and `UserEndpoints.cs`.
2. Design the integration of the new `ITokenService` into `AuthEndpoints.cs` (Register, Login, Refresh, ResetPassword, ExternalLogin) and `UserEndpoints.cs` (PasswordChanges).
3. Design the new `POST /api/v1/auth/logout` endpoint: accepts refresh token or authenticated user, revokes token(s) from database, returns `204 NoContent` or `200 OK`.
4. Design the validation hardening for `POST /api/v1/auth/register`: trimming input, checking duplicate username, ensuring `400 Bad Request` with `application/problem+json` via `V1ProblemDetailsFilter`.
5. Check `RestRouteContractTests.cs` to ensure no route conventions or contracts are violated.
Write your report in `c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\explorer_m1_2\endpoint_design.md` and write `handoff.md`.
Send a completion message when finished.
