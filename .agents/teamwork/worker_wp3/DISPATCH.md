## 2026-10-16T09:57:17Z
You are worker_wp3 (Role: ui_auth_identity_worker) for NexusKeys Game Store UI/UX modernization.
Your working directory is: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\worker_wp3\
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
- CRITICAL: Giữ nguyên các thuộc tính id, type, @bind và validation của các trường nhập liệu trong form Auth (Login.razor, Register.razor, ForgotPassword.razor, ResetPassword.razor) để không phá vỡ logic kiểm thử và hành vi hiện có!
- Build command `dotnet build GameStore.slnx` must succeed with 0 errors.

## YOUR EXCLUSIVE FILE OWNERSHIP
You own and may ONLY edit the following files:
- src/NexusKeys.Storefront/Components/Pages/Auth/Login.razor
- src/NexusKeys.Storefront/Components/Pages/Auth/Register.razor
- src/NexusKeys.Storefront/Components/Pages/Auth/ForgotPassword.razor
- src/NexusKeys.Storefront/Components/Pages/Auth/ResetPassword.razor
- src/NexusKeys.Storefront/Components/Layout/ProfileLayout.razor
- src/NexusKeys.Storefront/Components/Pages/Profile/Profile.razor
- src/NexusKeys.Storefront/Components/Pages/Profile/Library.razor
- src/NexusKeys.Storefront/Components/Pages/Profile/Wallet.razor
Do NOT modify files outside of this list!

## ASSIGNED TASKS (WP3: Authentication & Account Center)
1. Auth Pages (Login.razor, Register.razor, ForgotPassword.razor, ResetPassword.razor):
   - Replace static Epic logo (epic_logo.png) with NexusKeys brand badge/icon and "NEXUSKEYS" typography matching Navbar.
   - 100% Vietnamese localization: Translate all headings ("Sign in to Epic Games" -> "Đăng nhập NexusKeys", "Create an Epic Games Account" -> "Đăng ký tài khoản NexusKeys", "Email address" -> "Địa chỉ email", "Password" -> "Mật khẩu", "Forgot password?" -> "Quên mật khẩu?", "Continue" -> "Tiếp tục", "New here? Create an account" -> "Chưa có tài khoản? Tạo tài khoản ngay", "Only played on console?" -> "Đã từng mua game bản quyền?", "Other ways to sign in" -> "Phương thức đăng nhập khác", etc.).
   - Standardize `<PageTitle>` to Vietnamese with "— NexusKeys".
   - PRESERVE EXACT id, type, @bind attributes on all form elements.
2. Account Center (ProfileLayout.razor, Profile.razor, Library.razor, Wallet.razor):
   - In ProfileLayout.razor: replace "Epic Games Store Ecosystem" with "Hệ Sinh Thái Bản Quyền NexusKeys".
   - Improve contrast on dim text (text-white/30, text-white/40 -> text-white/60).
   - Enhance the key copying UX in Library.razor.
3. Build Verification:
   - Run `dotnet build GameStore.slnx` to verify 0 errors.
