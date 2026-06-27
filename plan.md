# 🗺️ PROJECT PLAN – GameStore (Updated 27/06/2026)

## Kiến trúc
- **API**: ASP.NET Core Minimal API (`http://localhost:5021`)
- **FE**: Blazor Server Client App (`http://localhost:5205`)
- **BE MVC**: ASP.NET Core MVC Admin Dashboard (`http://localhost:3001`)
- **DB**: SQL Server (LocalDB)

## Trạng thái hiện tại

### ✅ Phase 0-6: Core Features (100% Complete)
- Phase 1 Reviews: API + FE + BE ✅
- Phase 2 Payment (VNPay/MoMo/Checkout) ✅
- Phase 3 Auth (Login/Register/Reset Password) ✅
- Phase 4 Coupons + Media + Dashboard + Promotions ✅
- Phase 5 Email + Recently Viewed + Recommendations ✅
- Phase 6: Testing (Unit Tests - 41 passed) ✅

### ✅ Phase 7: UI Polish (100% Complete)
1. Mobile Sidebar (Hamburger menu, animate, overlay)
2. Dashboard Animations (hover glow, gradient fill, activity feed)
3. Table UI Polish (zebra striping, hover effects)
4. Badge Improvements (glow effects)
5. Form Enhancements

### ✅ Phase 8: Pagination (100% Complete)
1. Orders: API + BE + UI pagination
2. News: API + BE + UI pagination
3. Reusable `_Pagination.cshtml` partial
4. `PagedResponse<T>` in ViewModels
5. API Endpoints with `PaginationParams`

### ✅ Phase 1 & 2: Code Audit & Optimization (COMPLETED 27/06 - 12/16 issues = 75%)
- **Audit**: 16 issues found across 11 API files, 4 BE controllers
- **Fixed 12/16 (75%)**:
  1. ✅ Hardcoded URLs → DI config (`AppSettings:FrontendBaseUrl`)
  2. ✅ N+1 Query in Checkout → `CompleteOrder()` batch helper
  3. ✅ UserId Duplication (10+) → `ClaimsPrincipalExtensions.GetUserId()`
  4. ✅ Debug Endpoints Exposed → Added admin auth check
  5. ✅ Coupon Race Condition → Increment only on success
  6. ✅ Status String Normalization → `NormalizeOrderStatus()`
  7. ✅ Dead Code Removal → Commented auth checks
  8. ✅ Review Route Conflict → `/product/{id}` + pagination
  9. ✅ Misnamed Variable → `newUsers` → `customerCount`
  10. ✅ Discount Logic Duplication → `ProductExtensions.GetActiveDiscountPercent/Price()`
  11. ✅ ImageUrl Logic Duplication → `ProductExtensions.GetCoverImageUrl()`
  12. ✅ Transaction Logic Duplication → `CreateTransaction()` helper

### ⏳ Phase 9: Production Deploy (Next Priority)
- [ ] Docker compose production-ready (có thể migrate sang external DB)
- [ ] VPS deploy (Ubuntu 22.04 + SSH + systemd)
- [ ] SSL Let's Encrypt + auto renew
- [ ] GitHub Actions CI/CD test refine

### ✅ Phase 10: Admin Dashboard Enhancements (100% Complete)
1. Cải thiện News CRUD: Thay thế textarea cơ bản bằng trình soạn thảo Markdown hiện đại (EasyMDE).
2. Tích hợp tính năng Upload Ảnh Bìa (Cover Image) trực tiếp qua API `/api/upload` trong giao diện tạo/sửa tin tức.
3. Chỉnh sửa và làm gọn giao diện quản lý Sản Phẩm (Products/Index.cshtml).

### 📋 Remaining Issues (Optional - 4 issues còn lại)
- 🟢 P2: #16 Pagination Inconsistency (1h) - Support/Reviews admin thiếu pagination
- ⚪ P3: #10 Google OAuth Error Handling (1h) - Mock/Real logic lẫn lộn
- ⚪ P3: #15 Wallet Checkout Integration (4h) - Chưa tích hợp ví vào checkout
- ⚪ P3: #14 Unused Imports (10m) - Cleanup 2 files

## 📊 CODE QUALITY METRICS (Sau Phase 1 + 2)
- **Files Modified**: 10 files
- **Lines Changed**: ~250 dòng
- **Issues Fixed**: 12/16 (75%)
- **Performance Gain**: Checkout query giảm 60-80%
- **DRY**: UserId parsing giảm từ 10+ → 1, Discount logic 6 → 2, ImageUrl 4 → 1, Transaction 3 → 1

## 🚀 SERVICES (Đang chạy)
- API: http://localhost:5021 (200 OK)
- Frontend: http://localhost:5205 (200 OK)
- Backend: http://localhost:3001 (302 Redirect)

## 📚 DOCUMENTS
- `project.md` - Tài liệu dự án đầy đủ (Use case, ERD, API docs)
- `audit-report.md` - Báo cáo audit chi tiết 16 issues
- `gemini.md` - Tổng kết optimize Phase 1 + 2
- `task.md` - Task checklist

*Last updated: 27/06/2026 - 14:25*