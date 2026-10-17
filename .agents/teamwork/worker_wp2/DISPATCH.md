## 2026-10-16T09:57:17Z
You are worker_wp2 (Role: ui_storefront_discovery_worker) for NexusKeys Game Store UI/UX modernization.
Your working directory is: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\worker_wp2\
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
- src/NexusKeys.Storefront/Components/Pages/Home.razor
- src/NexusKeys.Storefront/Components/Pages/Products/ProductList.razor
- src/NexusKeys.Storefront/Components/Pages/Products/ProductDetail.razor
- src/NexusKeys.Storefront/Components/Shared/ProductCard.razor
- src/NexusKeys.Storefront/Components/Shared/ProductImage.razor
Do NOT modify files outside of this list!

## ASSIGNED TASKS (WP2: Storefront Discovery & Product Catalog)
1. Home.razor:
   - Update `<PageTitle>` to `"NexusKeys | Cửa Hàng Game Bản Quyền Chính Hãng"`.
   - Localize 100% of remaining English headings and labels (e.g., "Featured Discounts" -> "Ưu Đãi Giảm Giá Nổi Bật", "Top Sellers", etc.).
   - Attach REAL interactive click handlers to the Bookmark/Wishlist buttons on game cards (inject WishlistViewModel and/or IToastService / ToastService) so clicking triggers visual feedback via Toast ("Đã thêm vào danh sách yêu thích" or toggle status).
2. ProductList.razor & ProductCard.razor:
   - Update `<PageTitle>` to `"Kho Game Bản Quyền — NexusKeys"`.
   - Raise contrast of secondary text (upgrade dim `text-white/30`, `text-white/40` to `text-white/65` or `text-zinc-300`) to meet WCAG 2.1 AA (>= 4.5:1).
   - Improve touch targets for filter chips and pagination buttons (min 44px height/touch area).
3. ProductDetail.razor:
   - Update `<PageTitle>` suffix from `| Epic Games Store` to `— NexusKeys`.
   - Enhance contrast and sticky purchase box layout, reviews, and system requirements.
4. Build Verification:
   - Run `dotnet build GameStore.slnx` to verify 0 errors.

When done, write handoff.md in your working directory with build results, summary of changes, and call send_message to report to orchestrator.
