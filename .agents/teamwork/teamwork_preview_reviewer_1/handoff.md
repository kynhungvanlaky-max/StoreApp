# Handoff Report: Adversarial Review & Defect Resolution

## Overview
- **Role**: reviewer@swe_light / qa@swe_light
- **Task**: Adversarial Review and Defect Fixing for Dual Classification & Filters (Game vs App/Software), Dual Badge UI Visual Identification, and Live Search Chip Rendering across NexusKeys Storefront.
- **Status**: COMPLETE & VERIFIED
- **Test Inviolability**: `tests/` directory strictly untouched (`git diff --stat tests/` is 0). All 72/72 tests pass.

---

## 1. What the Prior Attempt Got Wrong

### Issue 1: Hybrid Classification Overriding Explicit Type
- **Input**: Product with `Type = "Game"` and Category `"Lập trình & Tiện ích"` or name containing `"Editor" / "Studio"`.
- **Expected**: `GameMediaHelper.IsApp(product)` returns `false` because explicit `Type = "Game"` should strictly anchor the product as a Game.
- **Actual**: `GameMediaHelper.IsApp(product)` returned `true` because the prior attempt checked category and name heuristics before or alongside `Type == "Game"`.
- **Root Cause**: In `GameMediaHelper.IsApp`, heuristic category matches (`c.Contains("công cụ")`, `c.Contains("lập trình")`) and keyword matches were executed without respecting explicit `Type == "Game"`. Similarly, in `GetDetailedBadge`, a game named "Cyberpunk 2077 Photo Studio" would be mislabeled as "Công cụ" rather than "Game".
- **Fix**: Refactored `GameMediaHelper.cs` to enforce strict priority: explicit `Type == "Game"` always returns `false`; explicit `Type == "Software"` always returns `true`. Category metadata takes second priority, and name keyword substring matching is strictly a last-resort fallback.

### Issue 2: Brittle Substring Matching in Backend Product Filtering
- **Input**: Querying `api/v1/products?type=Software&category=L%E1%BA%ADp+tr%C3%ACnh+%26+Ti%E1%BB%87n+%C3%ADch`.
- **Expected**: Correctly filters software tools belonging to programming/utilities without picking up arbitrary products with "Pro" or "Clone" in their name.
- **Actual**: `ProductEndpoints.cs` executed `p.Name.Contains("Pro") || p.Name.Contains("Clone")`, which accidentally captured games or products like "Windows 11 Pro" and failed on genuine programming tools. Furthermore, if `type` query parameter was passed as null or whitespace, query composition was brittle.
- **Root Cause**: Lack of seeded programming software in `DbInitializer.cs` led to hardcoding `Name.Contains("Pro")`.
- **Fix**: Replaced brittle string matching with clean category/name predicate checks (`Tool`, `IDE`, `Studio`, `Developer`, `Code`). Added `DevStudio Code IDE` to `DbInitializer.cs` under category "Lập trình & Tiện ích" and tag "Công cụ bản quyền". Sanitized `type` query parameter with `string.IsNullOrWhiteSpace` check.

### Issue 3: Missing `Type` Projection in Wishlist API & Storefront Razor
- **Input**: User adds a Software product (e.g., "Windows 11 Pro" or "Office 2024 Home & Business") to their Wishlist and visits `/wishlist`.
- **Expected**: Product card in Wishlist displays the cyan "Ứng dụng" dual badge with code/wrench icon.
- **Actual**: Product card displayed the default purple "Trò chơi" game badge.
- **Root Cause**: `WishlistEndpoints.cs` projected `WishlistItemDto` without selecting `Type = i.Product.Type`. Similarly, `WishlistModels.cs` lacked the `Type` field on `WishlistItemDto`, causing `Wishlist.razor` to bind `ProductDto` with null `Type`.
- **Fix**: Added `Type = i.Product.Type` to `WishlistEndpoints.cs`, `public string? Type { get; set; }` to `WishlistModels.cs`, and mapped `Type = item.Type` in `Wishlist.razor`.

### Issue 4: Keyboard Navigation and Search Dropdown Accessibility in `Navbar.razor`
- **Input**: User types in the live search bar and presses `Escape` or uses keyboard navigation.
- **Expected**: Dropdown closes upon pressing `Escape`, and clicking search suggestion chips triggers immediate navigation without blur event races.
- **Actual**: `Escape` key was unhandled. Clicking a suggestion chip could race with the `@onblur` handler closing the dropdown before the link activated.
- **Root Cause**: `Navbar.razor` had only a small 200ms blur timeout and no `OnSearchKeyDown` handler for `Escape`.
- **Fix**: Added `@onkeydown="HandleSearchKeyDown"` to dismiss search on `Escape`, added `@onclick="() => CloseSearch()"` directly on search result links, and increased blur debounce timeout to 300ms.

### Issue 5: Gift Card Inappropriate Certification in `ProductDetail.razor`
- **Input**: Viewing a Gift Card or non-game product detail page (e.g., Steam Wallet / Discord Nitro).
- **Expected**: Certification banner should show "Nội dung & Dịch vụ số" or general digital merchandise badge instead of "18+ Bạo lực".
- **Actual**: Showed "18+ Bạo lực, Ngôn từ mạnh" or game-specific ESRB content warning on gift cards.
- **Root Cause**: `ProductDetail.razor` had static/hardcoded game age rating badges without checking `GameMediaHelper.IsApp(product) || product.Categories.Any(c => c.Name.Contains("Thẻ") || c.Name.Contains("Gift"))`.
- **Fix**: Contextualized the certification box in `ProductDetail.razor` so Software and Gift Cards display "Bản quyền chính hãng" & "Phát hành số tiêu chuẩn" rather than age/violence ratings. Also refined breadcrumb context from "Trò chơi" to "Ứng dụng & Phần mềm" when viewing apps.

### Issue 6: Null Safety Risk on `ProductCard.razor`
- **Input**: `ProductCard` rendered with a null or uninitialized `Product` parameter.
- **Expected**: Gracefully render empty/placeholder without throwing `NullReferenceException`.
- **Actual**: Threw unhandled `NullReferenceException`.
- **Root Cause**: Direct property accesses without top-level `@if (Product == null) return;` guard.
- **Fix**: Added `@if (Product == null) { return; }` null guard at the top of `ProductCard.razor`.

### Issue 7: Mobile Responsiveness on `ProductList.razor` Filter Drawer
- **Input**: User opens filter sidebar on mobile viewport.
- **Expected**: Explicit close button to dismiss the mobile drawer.
- **Actual**: Sidebar could only be dismissed by clicking the dark overlay, which is not obvious on small screens.
- **Root Cause**: Missing close `(X)` button in the sidebar header.
- **Fix**: Added a dedicated mobile close button `<button type="button" class="lg:hidden ...">✕</button>` wired to `@onclick="() => isMobileFilterOpen = false"`.

---

## 2. Verification Record

### Deep Verification (Ran Actual Tests)
1. **Automated Unit Assertions for Classification Edge Cases**:
   - Created and ran isolated C# assertion suite covering 10 distinct edge cases for `GameMediaHelper`:
     - Test 1: Explicit Game with tool category -> Returns `false` (Pass)
     - Test 2: Explicit Software with game category -> Returns `true` (Pass)
     - Test 3: Null Type with app category -> Returns `true` (Pass)
     - Test 4: Null Type with game category -> Returns `false` (Pass)
     - Test 5: Empty Type & Categories with tool in name -> Returns `true` (Pass)
     - Test 6: Empty Type & Categories with generic name -> Returns `false` (Pass)
     - Test 7: GetDetailedBadge for Game -> Labeled "Game" (Pass)
     - Test 8: GetDetailedBadge for Software Category -> Labeled with category name (Pass)
     - Test 9: GetDetailedBadge for Game named 'Studio' -> Labeled "Game", NOT "Công cụ" (Pass)
     - Test 10: Null handling in GetDetailedBadge -> Defaults cleanly (Pass)
   - **Result**: 10/10 assertions passed.

2. **Project Compilation**:
   - `dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj` -> 0 Errors, 0 Warnings.
   - `dotnet build GameStore.slnx` -> 0 Errors, 0 Warnings.

3. **Full Regression Test Suite**:
   - `dotnet test tests/GameStore.API.ProductKeyTests/`
   - **Result**:
     - Passed: 72
     - Failed: 0
     - Skipped: 0
     - Duration: ~15s
     - **Integrity**: 100% passed.

4. **Test Inviolability Check**:
   - `git diff --stat tests/` -> Exactly 0 files modified, 0 lines changed.
   - `git status --porcelain tests/` -> Pristine / untracked baseline.

---

## 3. Known Issues & Remaining Risk

- `Fatal Functional Bug`: None.
- `Shallow Verification`: Blazor interactive WebAssembly/Server circuit interactions (DOM event dispatching and CSS animations) were validated via compile-time verification, C# AST logic verification, and unit tests, but not via browser end-to-end automation (Playwright/Selenium) as browser runtime environment is headless.
- `Minor Robustness Risk`: If external catalog sync imports products with novel Vietnamese diacritics in category names (e.g., "Phần Mềm Đồ Họa"), string comparison relies on `StringComparison.OrdinalIgnoreCase` which handles standard casing, but unaccented fuzzy matching is not currently implemented.

---

## 4. Modified Files List
1. `src/NexusKeys.Storefront/Services/GameMediaHelper.cs`
2. `src/NexusKeys.Api/Endpoints/Catalog/ProductEndpoints.cs`
3. `src/NexusKeys.Api/Endpoints/Community/WishlistEndpoints.cs`
4. `src/NexusKeys.Storefront/Models/Wishlist/WishlistModels.cs`
5. `src/NexusKeys.Storefront/Components/Pages/Wishlist/Wishlist.razor`
6. `src/NexusKeys.Storefront/Components/Shared/ProductCard.razor`
7. `src/NexusKeys.Storefront/Components/Layout/Navbar.razor`
8. `src/NexusKeys.Storefront/Components/Pages/Products/ProductDetail.razor`
9. `src/NexusKeys.Storefront/Components/Pages/Products/ProductList.razor`
10. `src/NexusKeys.Infrastructure/Data/DbInitializer.cs`
