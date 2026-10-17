## 2026-10-16T08:53:44Z
You are a Teamwork Explorer agent.
Working directory: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\explorer_m1_1
Project root: c:\Users\kynhu\Documents\DATN\datn
Original request: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\ORIGINAL_REQUEST.md
Project plan: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\orchestrator_1\PROJECT.md

CRITICAL CONSTRAINTS:
- You are strictly READ-ONLY. DO NOT edit or create any source or test files.
- Tuyệt đối KHÔNG ĐƯỢC CHỈNH SỬA, XÓA, COMMENT HOẶC LÀM SUY YẾU BẤT KỲ TEST CASE NÀO trong tests/ hoặc bất kỳ file test nào.

Your mission for Milestone 1:
Design the implementation strategy for extracting Token & Authentication services according to Clean Architecture:
1. Examine `src/NexusKeys.Domain` and design the interface `ITokenService` (or `IAuthService`), ensuring it uses existing namespace conventions (`GameStore.API.Interfaces` or compatible).
2. Examine `src/NexusKeys.Infrastructure` and design the implementation `TokenService` (or `AuthService`) in `src/NexusKeys.Infrastructure/Services/`, handling JWT generation with complete claims (`sub`, `ClaimTypes.NameIdentifier`/`nameid`, `email`, `role`, `jti`, `name`), 30-min lifespan, and refresh token rotation/revocation.
3. Check how services are registered in DI (`Program.cs` or Infrastructure DI extension).
Recommend exact file paths, class/interface signatures, method bodies, and namespace requirements.
Write your report in `c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\explorer_m1_1\service_design.md` and write `handoff.md`.
Send a completion message when finished.
