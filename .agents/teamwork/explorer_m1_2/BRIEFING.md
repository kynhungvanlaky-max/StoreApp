# BRIEFING — 2026-10-16T09:00:00Z

## Mission
Design the API endpoint refactoring and hardening in `NexusKeys.Api` for Milestone 2 (AuthEndpoints, UserEndpoints, Logout endpoint, Register hardening, RestRouteContractTests compliance).

## 🔒 My Identity
- Archetype: explorer
- Roles: [Investigation, Synthesis]
- Working directory: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\explorer_m1_2
- Original parent: 87c86cfa-dbae-4bae-80f6-a7bb236db1d5
- Milestone: milestone_2

## 🔒 Key Constraints
- Read-only investigation — do NOT implement or edit source/test files
- Tuyệt đối KHÔNG ĐƯỢC CHỈNH SỬA, XÓA, COMMENT HOẶC LÀM SUY YẾU BẤT KỲ TEST CASE NÀO
- Write outputs only in working directory

## Current Parent
- Conversation ID: 87c86cfa-dbae-4bae-80f6-a7bb236db1d5
- Updated: 2026-10-16T09:00:00Z

## Investigation State
- **Explored paths**:
  - `src/NexusKeys.Api/Endpoints/Identity/AuthEndpoints.cs`
  - `src/NexusKeys.Api/Endpoints/Identity/UserEndpoints.cs`
  - `src/NexusKeys.Api/Endpoints/Common/EndpointRouteConventions.cs`
  - `src/NexusKeys.Api/Program.cs`
  - `tests/GameStore.API.ProductKeyTests/RestRouteContractTests.cs`
  - `tests/GameStore.API.ProductKeyTests/ApiTestHost.cs`
  - `tests/GameStore.API.ProductKeyTests/AuthAndUserApiTests.cs`
  - `tests/GameStore.API.ProductKeyTests/ImprovementRegressionTests.cs`
  - `src/NexusKeys.Storefront/ViewModels/AuthViewModel.cs`
- **Key findings**:
  - `AuthEndpoints.cs` lacks `POST /api/v1/auth/logout`, lacks username duplicate check on register, and lacks whitespace trimming.
  - `ApiTestHost.cs` does not register `ITokenService` in DI; an endpoint resolving `ITokenService` via required parameter injection would fail when tests run. Resilient resolution (`ResolveTokenService`) solves this completely.
  - `RestRouteContractTests.cs` allows `POST /api/v1/auth/logout` as it does not trigger any exclusion filter.
  - Register error handling returning `Results.BadRequest(new { message = "..." })` is automatically converted to `application/problem+json` by `V1ProblemDetailsFilter`.
- **Unexplored areas**: None for Milestone 2 endpoint design scope.

## Key Decisions Made
- Use `ResolveTokenService(sp, db, config)` pattern: `sp.GetService<ITokenService>() ?? new TokenService(config, db)` to ensure 100% compatibility with test fixtures.
- Return `204 NoContent` on V1 for `POST /api/v1/auth/logout` matching REST convention of `/password-changes`.
- Map Domain `TokenResult` to API `AuthResponse` in `GameStore.API.Endpoints` to satisfy Clean Architecture layer decoupling and test suite type compatibility.

## Artifact Index
- DISPATCH.md — Received dispatch message
- BRIEFING.md — Working memory
- progress.md — Liveness heartbeat
- endpoint_design.md — Comprehensive API endpoint refactoring and hardening specification
- handoff.md — 5-component handoff report
