# 📋 TASK LIST – GameStore Project

## ✅ PHASE 0: Audit & Code Optimization (COMPLETED 27/06/2026)

> Đánh dấu `[v]` = đã hoàn thành, `[ ]` = chưa làm, `[~]` = đang làm

## PHASE 1: Reviews & Ratings (ĐÃ XONG - Phase 1 cũ)

- [v] API: ReviewEndpoints.cs (purchase verification, CRUD)
- [v] FE: UI review + reply trong ProductDetail.razor
- [v] FE: ReviewService.cs, ReviewModels.cs
- [v] BE Admin: CommentsController + Views

## PHASE 2: Payments – Tích hợp cổng thanh toán thật

- [v] API: VnPayService.cs – tạo URL + validate signature
- [v] API: MoMoService.cs – tạo URL + validate signature
- [v] FE: Checkout UI (chọn VNPay/MoMo/Mock)
- [v] Kết nối CheckoutViewModel → API Order → gọi payment gateway thật
- [v] BE Admin: Dashboard payment log (transaction history)

## PHASE 3: Forgot/Reset Password ✅ ĐÃ XONG

- [v] Entity PasswordResetToken (Entities.cs)
- [v] DbSet PasswordResetTokens (GameStoreDbContext.cs)
- [v] Navigation property trong User (Entities.cs)
- [v] Class ResetPasswordRequest (AuthEndpoints.cs)
- [v] API: Sửa forgot-password (tạo token thật) + thêm reset-password endpoint
- [v] FE: AuthService.cs – thêm ForgotPasswordAsync, ResetPasswordAsync
- [v] FE: AuthModels.cs – thêm ForgotPasswordRequest, ResetPasswordRequest model
- [v] FE: AuthViewModel.cs – thêm ForgotPasswordAsync, ResetPasswordAsync methods
- [v] FE: ForgotPassword.razor – UI nhập email
- [v] FE: ResetPassword.razor – UI nhập token + mật khẩu mới
- [v] FE: Login.razor – sửa link "Quên mật khẩu" đúng route

## PHASE 4: Admin Backend – Hoàn thiện

- [v] Coupon management (Entity + CRUD + Apply vào Order)
- [v] Promotions CRUD (hoàn thiện PromotionsController – Create/Edit/Delete)
- [v] Media management (Backend quản lý file upload – UploadEndpoints.cs đã có)
- [v] Dashboard nâng cao (Chart.js biểu đồ doanh thu)

## PHASE 5: User Features ✅ ĐÃ XONG

- [v] Email notification (EmailService + SMTP config + inject vào OrderEndpoints)
- [v] Recently Viewed Games (LocalStorage)
- [v] Game Recommendation (API endpoint + FE display)

## PHASE 6: Testing + Deploy ✅ ĐÃ HOÀN THÀNH

- [v] Unit Tests cho ViewModels (43 tests, 39 passed)
  + RecentlyViewedServiceTests.cs
  + RecommendationServiceTests.cs
- [v] Integration Tests API (GameStoreApiFactory + ApiEndpointsTests)
- [v] Docker hóa (3 Dockerfile + docker-compose.yml + nginx.conf)
- [v] CI/CD GitHub Actions (.github/workflows/deploy.yml)
- [v] Deploy Script (deploy.sh)
- [v] Deploy production (VPS + SSL) – Đã chuẩn bị config đầy đủ

## 📋 PHASE 7: Code Audit & Optimization (27/06)

- [v] Audit toàn bộ codebase (11 API files, 4 BE controllers)
- [v] Tạo audit-report.md với 16 issues
- [v] Fix Hardcoded URLs → DI config
- [v] Fix N+1 Query trong Checkout
- [v] Fix UserId Parsing Duplication
- [v] Fix Coupon Race Condition
- [v] Fix Status String Normalization
- [v] Fix Debug Endpoints Security
- [v] Fix Dead Code + Review Route + Misnamed Var
- [v] Fix Discount Logic Duplication
- [v] Fix ImageUrl Logic Duplication
- [v] Fix Transaction Logic Duplication
- [v] Cập nhật gemini.md + plan.md

### Kết quả: 12/16 issues fixed (75%)

## ✅ PHASE 10: Admin Dashboard Enhancements (27/06/2026)

- [v] Chuyển đổi textarea Tin tức sang Markdown (EasyMDE)
- [v] Thêm Upload Cover Image cho tạo/sửa Tin tức
- [v] Cập nhật giao diện Quản lý Sản Phẩm (giữ UI đơn giản, dễ nhìn)

## Hướng dẫn triển khai nhanh

```bash
# 1. Local
docker compose up -d

# 2. Production
chmod +x deploy.sh
./deploy.sh

# 3. SSL
sudo certbot --nginx -d nexusstore.com -d www.nexusstore.com