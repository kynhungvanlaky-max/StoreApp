# Plan — NexusKeys UI/UX Defect Repairs & Standardization

## Objective
Repair and standardize UI/UX defects across NexusKeys Storefront and Admin:
1. **R1**: Utility Classes Restoration — restore missing 343 Tailwind utility classes in Storefront and 130 classes in Admin.
2. **R2**: Vietnamese Typography Harmonization — normalize heading letter-spacing (-0.01em to 0em), line-height (1.25–1.35), tracking-wider on small uppercase labels, handle text-[9px].
3. **R3**: CSS Cascade De-duplication — clean up bad `@font-face` pointing to Epic Games, unify on Be Vietnam Pro, eliminate destructive rules in `nexus-ui.css` (bg-emerald-, bg-red-, shadow overrides), fix `.egs-pill-discount` text color to `#ffffff` on `#0078f2`.
4. **R4**: Positioning & Layout Correction — fix password visibility eye icon positioning in `Login.razor` & `Register.razor` (preserve id, type, @bind), restore Checkout Stepper connecting line (`h-[2px]`), ensure `ProductList.razor` 3/4 column grid, fix Admin Dashboard 2/3 column layout (`xl:col-span-2`).
5. **R5**: Test Suite Inviolability — 0 diff in `tests/`, preserve all auth input IDs/binds, verify `dotnet build GameStore.slnx` 0 errors, `dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj` 0 errors, 72/72 tests pass in `tests/GameStore.API.ProductKeyTests/`.

## Phased Approach
### Phase 0: Survey & Technical Investigation (Parallel Explorers)
- **Explorer 1 (`explorer_survey_1`)**: Focus on R1 & R3 — CSS utility classes, missing classes script/inventory across Storefront & Admin, CSS files (`epic-games.css`, `atlas.css`, `nexus-ui.css`, `app.css`), bad @font-face rules, destructive styles.
- **Explorer 2 (`explorer_survey_2`)**: Focus on R2 & R4 — Vietnamese typography & headings, letter-spacing, line-height, text-[9px], layout positioning (`Login.razor`, `Register.razor`, Checkout Stepper `h-[2px]`, `ProductList.razor`, Admin Dashboard `xl:col-span-2`).
- **Explorer 3 (`explorer_survey_3`)**: Focus on R5 & Build/Verification baseline — verify test files, current build status, verify auth form input IDs/binds, verify script/method for detecting missing CSS classes.

### Phase 1: Implementation
- Dispatch Worker to apply comprehensive, high-fidelity changes to CSS stylesheets and component markup.
- Worker executes build and test suites to verify zero errors and test passing.

### Phase 2: Independent Review, Challenge & Forensic Audit
- 2 Reviewers (`teamwork_preview_reviewer`): independently review code diffs, CSS validity, UI semantics, layout adherence.
- 2 Challengers (`teamwork_preview_challenger`): adversarially test CSS classes, contrast, responsiveness, test inviolability, form inputs.
- 1 Forensic Auditor (`teamwork_preview_auditor`): run integrity forensics, test inviolability check, ensure zero hardcoding/facades.

### Phase 3: Gate Status & Reporting
- Evaluate all verdicts strictly (Binary Veto on Forensic Audit).
- Compile handoff and report to Sentinel.
