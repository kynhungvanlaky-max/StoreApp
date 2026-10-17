## 2026-10-16T10:27:38Z
You are worker_remediation (Role: Remediation Worker) for NexusKeys Game Store.
Your working directory is: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\worker_remediation\
Store your progress.md and handoff.md in your working directory.

## MANDATORY INPUT DOCUMENTS
You MUST read:
- c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\ORIGINAL_REQUEST.md
- c:\Users\kynhu\Documents\DATN\datn\docs\UI_UX_MASTER_PLAN.md
- Exact remediation plan:
  c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\explorer_it2\handoff.md

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## STRICT CONSTRAINTS & TEST PROTECTION
- Tuyệt đối KHÔNG ĐƯỢC CHỈNH SỬA, XÓA, COMMENT HOẶC LÀM SUY YẾU BẤT KỲ TEST CASE NÀO trong thư mục tests/.
- git diff --stat tests/ PHẢI HOÀN TOÀN TRỐNG (0 diff).
- Build command `dotnet build GameStore.slnx` must succeed with 0 errors.

## YOUR EXCLUSIVE FILE OWNERSHIP
You own and may ONLY edit the following 4 files:
- src/NexusKeys.Storefront/Components/Pages/NotFound.razor
- src/NexusKeys.Storefront/Components/Pages/Error.razor
- src/NexusKeys.Storefront/Components/Pages/Cart/Cart.razor
- src/NexusKeys.Storefront/Components/Pages/Checkout/Checkout.razor
Do NOT modify files outside of this list!

## ASSIGNED REMEDIATION TASKS
Apply the exact drop-in replacements specified in Section 4 of `c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\explorer_it2\handoff.md`:
1. `NotFound.razor`: Replace with the 100% Vietnamese, NexusKeys branded version with PuzzlePiece icon, <PageTitle>404 — Không Tìm Thấy Trang — NexusKeys</PageTitle>, zero epic_logo references.
2. `Error.razor`: Update <PageTitle> to `Đã Xảy Ra Sự Cố — NexusKeys`, elevate line 20 RequestId to `text-white/70`.
3. `Cart.razor`: Elevate lines 155, 240, 247 text-white/40 and 50 to `text-white/70`.
4. `Checkout.razor`: Elevate line 243 terms text-white/40 to `text-white/70`.
5. Run verification:
   - `dotnet build GameStore.slnx`
   - `dotnet test tests/GameStore.API.ProductKeyTests/`
   - `git diff --stat tests/`

When done, write handoff.md in your working directory and notify the orchestrator via send_message.
