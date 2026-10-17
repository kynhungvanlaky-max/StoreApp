# Implementation Plan: UI/UX Modernization & NexusKeys Localization

## Overview
Based on `docs/UI_UX_MASTER_PLAN.md` and `ORIGINAL_REQUEST.md`, execute UI/UX Modernization, 100% Vietnamese localization, NexusKeys branding, WCAG 2.1 AA contrast improvements, interactive enhancements (Wishlist toasts, Navbar live search pricing, Checkout stepper, Admin order badges), and test protection across 4 isolated work packages.

## Strict Rules & Constraints
1. **Zero Test Modifications**:
   - `tests/` directory must NOT be touched. `git diff --stat tests/` must remain completely empty.
   - 72/72 tests in `tests/GameStore.API.ProductKeyTests/` must pass.
2. **Form Inviolability**:
   - In Auth forms (`Login`, `Register`, `ForgotPassword`, `ResetPassword`), preserve all `id`, `type`, `@bind`, and validation elements intact.
3. **Build Quality**:
   - `dotnet build GameStore.slnx` must succeed with 0 errors.

## Work Breakdown & File Isolation Matrix

### Package 1 (WP1): Design System, Shell & Layout
- **Assigned Subagent**: `teamwork_preview_worker` (Role: `ui_design_system_worker`)
- **Exclusive Files**:
  - `src/NexusKeys.Storefront/Components/App.razor`
  - `src/NexusKeys.Storefront/Components/Layout/MainLayout.razor`
  - `src/NexusKeys.Storefront/Components/Layout/Navbar.razor`
  - `src/NexusKeys.Storefront/Components/Layout/Footer.razor`
  - `src/NexusKeys.Storefront/Components/Layout/ToastContainer.razor`
  - `src/NexusKeys.Storefront/wwwroot/css/*`
- **Tasks**:
  1. `Navbar.razor`: Display price / discount badge alongside game title in Live Search Dropdown. Add live search dropdown support on Mobile menu. Ensure icon buttons (Wishlist, Cart, Account) have accessible touch target and focus-visible.
  2. `Footer.razor`: 100% Vietnamese localization, replace Epic Games mentions with NexusKeys branding, clean copyright & legal links.
  3. `App.razor` & CSS: Ensure proper font configuration and color tokens for NexusKeys.

### Package 2 (WP2): Storefront Discovery & Product Catalog
- **Assigned Subagent**: `teamwork_preview_worker` (Role: `ui_storefront_discovery_worker`)
- **Exclusive Files**:
  - `src/NexusKeys.Storefront/Components/Pages/Home.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Products/ProductList.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Products/ProductDetail.razor`
  - `src/NexusKeys.Storefront/Components/Shared/ProductCard.razor`
  - `src/NexusKeys.Storefront/Components/Shared/ProductImage.razor`
- **Tasks**:
  1. `Home.razor`: Update `<PageTitle>` to `"NexusKeys | Cửa Hàng Game Bản Quyền Chính Hãng"`. Localize all English headings (e.g. `"Featured Discounts"` -> `"Ưu Đãi Giảm Giá Nổi Bật"`). Attach real click handlers for Wishlist/Bookmark buttons to show Toast notifications.
  2. `ProductList.razor` & `ProductCard.razor`: `<PageTitle>` to `"Kho Game Bản Quyền — NexusKeys"`. Enhance secondary text contrast (`text-white/40` -> `text-white/65`). Improve touch targets for filter chips and pagination.
  3. `ProductDetail.razor`: PageTitle suffix to `— NexusKeys`. Sticky purchase box optimization and contrast review.

### Package 3 (WP3): Authentication & Account Center
- **Assigned Subagent**: `teamwork_preview_worker` (Role: `ui_auth_identity_worker`)
- **Exclusive Files**:
  - `src/NexusKeys.Storefront/Components/Pages/Auth/Login.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Auth/Register.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Auth/ForgotPassword.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Auth/ResetPassword.razor`
  - `src/NexusKeys.Storefront/Components/Layout/ProfileLayout.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Profile/Profile.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Profile/Library.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Profile/Wallet.razor`
- **Tasks**:
  1. Auth pages (`Login`, `Register`, `ForgotPassword`, `ResetPassword`):
     - Replace static Epic logo with NexusKeys brand icon/typography.
     - 100% Vietnamese translation of labels, buttons, helpers, placeholders, titles.
     - PRESERVE EXACT `id`, `type`, `@bind` attributes on inputs.
  2. Account Center (`ProfileLayout`, `Profile`, `Library`, `Wallet`):
     - In `ProfileLayout.razor`: change "Epic Games Store Ecosystem" to "Hệ Sinh Thái Bản Quyền NexusKeys".
     - Increase contrast for dim text (`text-white/30`, `text-white/40` -> `text-white/60`). Enhance license key copy UX.

### Package 4 (WP4): Commerce Funnel, Support & Admin Portal
- **Assigned Subagent**: `teamwork_preview_worker` (Role: `ui_commerce_support_worker`)
- **Exclusive Files**:
  - `src/NexusKeys.Storefront/Components/Pages/Cart/Cart.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Checkout/Checkout.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Checkout/CheckoutSuccess.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Orders/OrderList.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Orders/OrderDetail.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Wishlist/Wishlist.razor`
  - `src/NexusKeys.Storefront/Components/Pages/News/NewsList.razor` & `NewsDetail.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Support.razor`, `Assistant.razor`, `Components/Layout/ChatWidget.razor`
  - `src/NexusKeys.Storefront/Components/Pages/Distribution.razor`, `Creator.razor`, `Careers.razor`, `Policies.razor`, `Download.razor`
  - `src/NexusKeys.Admin/Views/Home/Index.cshtml`
- **Tasks**:
  1. Checkout Stepper: Add visual 3-step progress bar (`1. Giỏ hàng` -> `2. Thanh toán` -> `3. Hoàn tất & Nhận Key`) across Cart, Checkout, CheckoutSuccess.
  2. Support & News: Update titles and localized text, remove Epic Games references.
  3. Admin Dashboard (`src/NexusKeys.Admin/Views/Home/Index.cshtml`): Wrap recent orders status in colored badges with Vietnamese labels (`Đã hoàn thành` - green, `Chờ thanh toán` - amber, `Thất bại` - red).

## Quality Assurance & Quality Gate
1. **Verification**: Worker build checks (`dotnet build GameStore.slnx`).
2. **Reviewer & Auditor**:
   - `teamwork_preview_reviewer`: Verify responsive layout, touch targets, translations, and UX flows.
   - `teamwork_preview_auditor`: Run 6-pillar quality gate check (Brand, WCAG Contrast, Responsive, UX States, Build & Test Inviolability).
