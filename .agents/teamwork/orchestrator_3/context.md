# Orchestrator Task Context

## User Request
See `c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\ORIGINAL_REQUEST.md` (section `## 2026-10-16T11:05:02Z`).

## Mission
Deep UI/UX defect repairs, CSS utility restoration, Vietnamese typography harmonization, CSS de-duplication, and layout positioning for NexusKeys Storefront & Admin.

## Key Constraints
- Strict preservation of `tests/` directory: zero test modifications (`git diff --stat tests/` must be 0 diff).
- 72/72 tests in `tests/GameStore.API.ProductKeyTests/` must pass 100%.
- Both `dotnet build GameStore.slnx` and `dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj` must succeed with 0 errors.
- 0 missing Tailwind utility classes in Storefront (.razor) and Admin (.cshtml).
- Vietnamese typography: letter-spacing -0.01em to 0em on h1-h6, line-height >= 1.25-1.35.
- Fix eye icon positioning in Login.razor & Register.razor without breaking input bindings or ids.
- Restore 3-step checkout stepper connector lines.
- Fix Admin Dashboard grid-cols layout (xl:col-span-2).
