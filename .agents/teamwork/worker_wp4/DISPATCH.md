## 2026-10-16T09:57:17Z
You are worker_wp4 (Role: ui_commerce_support_worker) for NexusKeys Game Store UI/UX modernization.
Your working directory is: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\worker_wp4\
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
- src/NexusKeys.Storefront/Components/Pages/Cart/Cart.razor
- src/NexusKeys.Storefront/Components/Pages/Checkout/Checkout.razor
- src/NexusKeys.Storefront/Components/Pages/Checkout/CheckoutSuccess.razor
- src/NexusKeys.Storefront/Components/Pages/Orders/OrderList.razor
- src/NexusKeys.Storefront/Components/Pages/Orders/OrderDetail.razor
- src/NexusKeys.Storefront/Components/Pages/Wishlist/Wishlist.razor
- src/NexusKeys.Storefront/Components/Pages/News/NewsList.razor & NewsDetail.razor
- src/NexusKeys.Storefront/Components/Pages/Support.razor, Assistant.razor, Components/Layout/ChatWidget.razor
- src/NexusKeys.Storefront/Components/Pages/Distribution.razor, Creator.razor, Careers.razor, Policies.razor, Download.razor
- src/NexusKeys.Admin/Views/Home/Index.cshtml
Do NOT modify files outside of this list!

## ASSIGNED TASKS (WP4: Commerce Funnel, Support & Admin Portal)
1. Checkout Stepper across Cart, Checkout, CheckoutSuccess:
   - Add a visual 3-step progress bar (Checkout Stepper: "1. Giỏ hàng" -> "2. Thanh toán" -> "3. Hoàn tất & Nhận Key") with clear active/completed indicators to give users immediate awareness of where they are in the checkout flow.
2. News, Assistant & Support pages:
   - Update `<PageTitle>` and headings to remove any lingering "Epic Games" references (e.g., "Tin tức Epic Games" -> "Tin Tức & Sự Kiện — NexusKeys", Assistant -> "Trợ Lý AI NexusKeys").
3. Admin Portal Dashboard (src/NexusKeys.Admin/Views/Home/Index.cshtml):
   - In recent orders table (around line 71), replace raw English status strings ("Completed", "Pending", "Failed") with beautiful Vietnamese badges with distinctive colors (e.g. "Đã hoàn thành" with green badge, "Chờ thanh toán" with amber badge, "Thất bại" with red badge).
4. Build Verification:
   - Run `dotnet build GameStore.slnx` to verify 0 errors.

When done, write handoff.md in your working directory with build results, summary of changes, and call send_message to report to orchestrator.
