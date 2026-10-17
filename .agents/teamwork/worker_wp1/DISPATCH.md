## 2026-10-16T09:57:17Z
You are worker_wp1 (Role: ui_design_system_worker) for NexusKeys Game Store UI/UX modernization.
Your working directory is: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\worker_wp1\
You must store your progress.md and handoff.md in your working directory.

## MANDATORY INPUT DOCUMENTS
Read these files first:
- c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\ORIGINAL_REQUEST.md
- c:\Users\kynhu\Documents\DATN\datn\docs\UI_UX_MASTER_PLAN.md

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## STRICT CONSTRAINTS & TEST PROTECTION
- Tuyệt đối KHÔNG ĐƯỢC CHỈNH SỬA, XÓA, COMMENT HOẶC LÀM SUY YẾU BẤT KỲ TEST CASE NÀO trong thư mục tests/.
- git diff --stat tests/ PHẢI HOÀN TOÀN TRỐNG (0 diff).
- Build command `dotnet build GameStore.slnx` must succeed with 0 errors.

## YOUR EXCLUSIVE FILE OWNERSHIP
You own and may ONLY edit the following files:
- src/NexusKeys.Storefront/Components/App.razor
- src/NexusKeys.Storefront/Components/Layout/MainLayout.razor
- src/NexusKeys.Storefront/Components/Layout/Navbar.razor
- src/NexusKeys.Storefront/Components/Layout/Footer.razor
- src/NexusKeys.Storefront/Components/Layout/ToastContainer.razor
- src/NexusKeys.Storefront/wwwroot/css/*
Do NOT modify files outside of this list!

## ASSIGNED TASKS (WP1: Design System, Shell & Layout)
1. Navbar.razor:
   - In Live Search Dropdown: display product price and discount badge (% off or formatted price) next to game title so users see pricing immediately.
   - Add live search dropdown support on Mobile menu (so search query generates immediate suggestions on mobile too).
   - Ensure all icon buttons (Wishlist, Cart, User Profile) have visible focus-visible states and compliant touch targets (min 44x44px).
2. Footer.razor:
   - Complete Vietnamese localization & NexusKeys branding.
   - Replace any remaining Epic Games mentions ("Resources", "Made by Epic Games", "Terms of Service", "Privacy Policy", "Store Refund Policy", copyright text) with standard NexusKeys brand and Vietnamese text ("Tài nguyên", "Phát triển bởi NexusKeys", "Điều khoản dịch vụ", "Chính sách quyền riêng tư", "Chính sách hoàn tiền", v.v.).
3. App.razor & Stylesheet:
   - Synchronize fonts and design tokens for NexusKeys. Ensure proper contrast and clean styling.
4. Build Verification:
   - Run `dotnet build GameStore.slnx` to verify 0 errors.

When done, write handoff.md in your working directory with build results, summary of changes, and call send_message to report to orchestrator.
