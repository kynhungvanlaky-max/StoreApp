# BÁO CÁO THẨM ĐỊNH & HOÀN THIỆN TYPOGRAPHY (REVIEWER R1 HANDOFF REPORT)
**Vai trò:** reviewer@swe_light / qa@swe_light (Review Round 1)  
**Thời gian:** 16/10/2026  
**Trạng thái kiểm tra:** PASSED 100% (Build & All 72 Test Cases Passed)

---

## 1. Những điểm khiếm khuyết trong lần triển khai trước (What the prior attempt got wrong)

### Vấn đề 1: Bỏ sót class CSS thuần có cỡ chữ siêu nhỏ `< 11px` (`.egs-card-badge-pill`)
- **Input:** Khảo sát các quy tắc CSS tùy biến trong `wwwroot/css/epic-games.css` và `wwwroot/design/atlas.css`.
- **Expected:** Toàn bộ các class badge và chip áp dụng trên card sản phẩm và store đạt tối thiểu `11px` hoặc `12px` (text-xs) với line-height $\ge$ 16px.
- **Actual:** `.egs-card-badge-pill` vẫn giữ nguyên `font-size: 10px;` không có line-height và không có thuộc tính chống tràn (`white-space: nowrap`).
- **Root Cause:** Implementer chỉ quét regex chuỗi Tailwind như `text-[10px]` mà không kiểm tra các quy tắc CSS thuần của Epic Games design system.

### Vấn đề 2: Bỏ sót quy chuẩn R2 đối với văn bản mô tả chính sách và hướng dẫn
- **Input:** `Cart.razor` (dòng 247), `Distribution.razor` (dòng 260, 270, 278, 317, 327, 337), `Profile.razor` (dòng 110, 121), `ProductDetail.razor` (dòng 500-502), `Assistant.razor` (dòng 60), `Login.razor` (dòng 100, 183), `Register.razor` (dòng 137), `ForgotPassword.razor` (dòng 88), `ResetPassword.razor` (dòng 93).
- **Expected:** Các đoạn văn bản mô tả chính sách, giải thích tính năng và hướng dẫn người dùng phải đạt chuẩn `text-sm` (14px) hoặc `text-xs` (12px) kèm khoảng cách dòng thoáng (`leading-relaxed`), không bị ép ở mức `text-[11px]`.
- **Actual:** Các đoạn văn này bị đặt ở mức `text-[11px]` (ví dụ: chú thích doanh thu creator trong Cart, 6 khối mô tả tính năng đối tác trong Distribution, chú thích trường tài khoản trong Profile, ghi chú bản quyền buybox trong ProductDetail).
- **Root Cause:** Implementer nâng cấp chưa triệt để các khối văn bản đoạn dài mà chỉ tập trung vào một số view chính.

### Vấn đề 3: Thiếu bảo vệ chống co ngắt dòng (`white-space: nowrap`) trên Badge và Pill giảm giá
- **Input:** Card game và discount pill trên thiết bị có độ rộng cực hẹp (320px - 360px).
- **Expected:** Badge giảm giá `-50%` hoặc `-75%` và tag trạng thái phải hiển thị trên một dòng duy nhất, có `min-height >= 16px` và `line-height >= 16px`.
- **Actual:** Trong `ProductCard.razor`, discount badge dùng `leading-none` (12px line-height) và thiếu `whitespace-nowrap`, có nguy cơ bị cắt mép hoặc gãy dòng khi flex cha co lại. Class `.egs-pill-discount` cũng thiếu `white-space: nowrap !important; flex-shrink: 0 !important;`.
- **Root Cause:** Chưa cấu hình đầy đủ CSS flex-shrink và nowrap cho discount pill.

### Vấn đề 4: Tiến trình dev server chạy nền khóa nhị phân làm hỏng lệnh `dotnet build`
- **Input:** Chạy `dotnet build GameStore.slnx` hoặc `dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj`.
- **Expected:** Lệnh build biên dịch thành công với 0 lỗi (0 errors).
- **Actual:** Build thất bại với lỗi `MSB3021` / `MSB3027` do tiến trình `Frontend.exe` (PID 61744) và `Backend.exe` (PID 64380) đang chạy và chiếm quyền khóa file trong `bin/Debug/net10.0/`.
- **Root Cause:** Tiến trình demo nền không được quản lý giải phóng trước khi biên dịch, khiến việc kiểm thử build không thể đạt 0 errors.

---

## 2. Những nội dung đã khắc phục & điều chỉnh (What I changed)

1. **`src/NexusKeys.Storefront/wwwroot/css/epic-games.css` & `src/NexusKeys.Storefront/wwwroot/design/atlas.css`**:
   - Nâng cấp `.egs-card-badge-pill`: Từ `font-size: 10px` lên `font-size: 11px; line-height: 16px; min-height: 20px; font-weight: 600; white-space: nowrap; flex-shrink: 0; display: inline-flex; align-items: center;`.
   - Nâng cấp `.egs-pill-discount`: Bổ sung `white-space: nowrap !important; flex-shrink: 0 !important;` để loại bỏ hoàn toàn rủi ro vỡ dòng ở màn hình nhỏ $\le 360px$.

2. **`src/NexusKeys.Storefront/wwwroot/css/site.css` & `src/NexusKeys.Admin/wwwroot/css/site.css`**:
   - Thiết lập chốt an toàn (defense-in-depth): Map `.text-\[9px\]` và `.text-\[10px\]` về `font-size: 11px !important; line-height: 16px !important;` để bảo đảm không có bất kỳ thành phần nào có thể render dưới 11px.

3. **`src/NexusKeys.Storefront/Components/Shared/ProductCard.razor`**:
   - Chuẩn hóa pill giảm giá: Đổi `leading-none` thành `leading-[16px] min-h-[16px] inline-flex items-center whitespace-nowrap shrink-0`.

4. **`src/NexusKeys.Storefront/Components/Layout/Navbar.razor`**:
   - Chuẩn hóa cart count badge: Đổi class lỗi `min-w-4 h-4` thành `min-w-[18px] h-[18px] px-1 text-[11px] font-bold leading-none bg-[#0078F2] text-white rounded-full flex items-center justify-center shadow-sm`, đảm bảo số lượng > 99 hiển thị sắc nét không bị cắt chữ.

5. **`src/NexusKeys.Storefront/Components/Pages/Cart/Cart.razor`**:
   - Nâng cấp chú thích doanh thu Creator tag (dòng 247) từ `text-[11px]` lên `text-sm text-white/70 mt-1.5 leading-relaxed`.

6. **`src/NexusKeys.Storefront/Components/Pages/Products/ProductDetail.razor`**:
   - Nâng cấp ghi chú bản quyền điện tử từ `text-xs leading-normal` lên `text-sm text-white/70 leading-relaxed`.
   - Nâng cấp thông báo hạn ưu đãi từ `text-[11px] text-amber-400 font-medium` lên `text-xs text-amber-400 font-semibold`.

7. **`src/NexusKeys.Storefront/Components/Pages/Profile/Profile.razor`**:
   - Nâng cấp chú thích hướng dẫn form (Tên tài khoản không thể chỉnh sửa, Dùng để nhận License Key) từ `text-[11px]` lên `text-xs text-white/70 mt-1.5 block font-medium`.

8. **`src/NexusKeys.Storefront/Components/Pages/Distribution.razor`**:
   - Nâng cấp 6 đoạn mô tả tính năng đối tác phát triển từ `text-[11px] text-white/50 leading-relaxed` lên `text-sm text-white/60 leading-relaxed`.
   - Nâng cấp tiêu đề tính năng tương ứng từ `text-xs font-bold` lên `text-sm font-bold`.
   - Nâng cấp phụ đề First Run từ `text-[11px]` lên `text-xs text-white/70`.

9. **`src/NexusKeys.Storefront/Components/Pages/Assistant.razor`**:
   - Nâng cấp thông tin giới thiệu Nexus AI từ `text-[11px]` lên `text-xs leading-relaxed text-white/70`.

10. **`src/NexusKeys.Storefront/Components/Pages/Auth/` (`Login.razor`, `Register.razor`, `ForgotPassword.razor`, `ResetPassword.razor`)**:
    - Nâng cấp các liên kết chính sách bảo mật và phụ đề đăng nhập console từ `text-[11px]` lên `text-xs text-white/60 hover:text-white`.

11. **`frontend/wwwroot/`**:
    - Đồng bộ cập nhật `.egs-card-badge-pill` lên `font-size: 11px; line-height: 16px; min-height: 20px; white-space: nowrap;` trong `frontend/wwwroot/css/epic-games.css` và `frontend/wwwroot/design/atlas.css`.

---

## 3. Biên bản kiểm tra xác minh (Verification Record)

### Deep Verification (Thực thi build và test thực tế)
- **Kiểm tra biên dịch Solution:**
  `dotnet build GameStore.slnx`
  $\rightarrow$ **Build succeeded: 0 Error(s), 12 Warning(s)** (chỉ có cảnh báo bảo mật phụ thuộc gói gốc của hệ thống).
- **Kiểm tra biên dịch Storefront:**
  `dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj`
  $\rightarrow$ **Build succeeded: 0 Error(s), 0 Warning(s)**.
- **Kiểm tra bộ Test Suite:**
  `dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj`
  $\rightarrow$ **Passed! Failed: 0, Passed: 72, Skipped: 0, Total: 72, Duration: 23 s - GameStore.API.ProductKeyTests.dll (net10.0)**.
- **Kiểm tra tính bất biến của Tests:**
  `git diff --stat tests/`
  $\rightarrow$ **Trống 100% (0 diff, 0 file thay đổi)**.
- **Kiểm tra triệt để class cỡ chữ < 11px:**
  - Regex `text-\[(?:[0-9]|10)px\]` trên `src/`: **0 kết quả**.
  - Regex `font-size:\s*(?:[1-9]|10)px` trên `src/` và `frontend/`: **0 kết quả**.
- **Kiểm tra hoạt động máy chủ HTTP (Live Run):**
  - Storefront (`http://localhost:5205`): **HTTP 200 OK**.
  - Admin (`http://localhost:3001`): **HTTP 200 OK**.

### Shallow Verification (Kiểm tra thủ công bằng code audit)
- Rà soát thuộc tính input form (`id`, `type`, `@bind`) trên toàn bộ các trang Auth (`Login.razor`, `Register.razor`, `ForgotPassword.razor`, `ResetPassword.razor`, `Profile.razor`) bảo đảm giữ nguyên 100%.

### Unverified aspects
- Chưa thực hiện kiểm thử tự động chụp ảnh màn hình (pixel-by-pixel automated visual regression) trên toàn bộ các tỷ lệ màn hình vật lý khác nhau; chỉ thực hiện kiểm tra cấu trúc DOM, CSS rules, HTTP 200 và unit tests.

---

## 4. Các vấn đề đã biết (Known Issues)
- Không có lỗi chức năng nghiêm trọng (`Fatal Functional Bug`: Không có).
- `Minor Robustness Risk`: Khi viewport quá nhỏ dưới 300px, tiêu đề game cực dài có thể bị cắt bớt bằng ellipsis (hành vi được chủ đích cấu hình qua `truncate` / `line-clamp-1`).

---

## 5. Kết luận & Đánh giá mức độ hoàn thành
Nhiệm vụ nâng cấp và chuẩn hóa toàn diện hệ thống Typography Scale & Font Size Hierarchy đã được thẩm định, sửa đổi bổ sung và kiểm tra hoàn tất. Toàn bộ các tiêu chí nghiệm thu đã đạt 100%. Không cần thêm lượt chỉnh sửa cho hạng mục này.
