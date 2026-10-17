# Original User Request

## 2026-10-16T08:40:51Z

Phát triển, chuẩn hóa và củng cố module Quản lý Người dùng và Xác thực (Authentication & User Identity API) cho hệ thống NexusKeys Game Store theo chuẩn Clean Architecture và REST API v1.

Working directory: c:\Users\kynhu\Documents\DATN\datn
Integrity mode: demo

## Requirements

### R1. Chuẩn hóa luồng Xác thực (Authentication Flow)
Cung cấp và hoàn thiện đầy đủ các API xác thực tài khoản bao gồm Đăng ký (Register), Đăng nhập (Login), Cấp mới token (Refresh Token với cơ chế rotation/revocation), và Đổi mật khẩu (Password Changes). Mọi phản hồi lỗi phải tuân thủ chuẩn RFC 7807 (`application/problem+json`).

### R2. Bảo mật & Quản lý Phiên (JWT Bearer & Session Security)
Đảm bảo JWT Bearer token chứa đầy đủ các claims tiêu chuẩn (`sub`/`nameid`, `email`, `role`, `jti`), cấu hình thời hạn token hợp lý và xử lý an toàn việc thu hồi refresh token khi đăng xuất hoặc đổi mật khẩu.

### R3. Tuân thủ Quy chuẩn Dự án và Bảo toàn Kiểm thử
Tất cả các thay đổi phải tương thích với cấu trúc Clean Architecture hiện có (`NexusKeys.Domain`, `NexusKeys.Infrastructure`, `NexusKeys.Api`). Tuân thủ nghiêm ngặt quy tắc không được sửa đổi, xóa hoặc làm suy yếu bất kỳ test case nào trong thư mục `tests/`.

## Acceptance Criteria

### Build & Integrity
- [ ] Lệnh `dotnet build GameStore.slnx` biên dịch thành công với 0 lỗi (0 errors).
- [ ] Tuyệt đối không có thay đổi nào trong thư mục `tests/` (giữ nguyên tính toàn vẹn của test suite).

### Test Suite Execution
- [ ] Toàn bộ các bài kiểm thử trong `tests/GameStore.API.ProductKeyTests/` chạy thành công (`dotnet test` kết thúc với mã thoát 0).
- [ ] Các test case liên quan đến xác thực và người dùng (`AuthAndUserApiTests`) đều vượt qua đầy đủ 100%.

### API Contract & Error Handling
- [ ] Các endpoint xác thực trả về mã trạng thái HTTP chuẩn (`200 OK`, `201 Created`, `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`).
- [ ] Khi xảy ra lỗi validation hoặc xác thực thất bại, phản hồi trả về đúng định dạng `ProblemDetails` theo chuẩn của hệ thống.


## 2026-10-16T09:53:37Z

Nâng cấp và chuẩn hóa toàn diện giao diện người dùng (UI/UX) cho hệ thống NexusKeys Game Store theo đặc tả tại docs/UI_UX_MASTER_PLAN.md, đồng bộ nhận diện thương hiệu NexusKeys, 100% Tiếng Việt, tối ưu luồng tương tác và đảm bảo chuẩn tiếp cận WCAG 2.1 AA.

Working directory: c:\Users\kynhu\Documents\DATN\datn
Integrity mode: demo

## Requirements

### R1. Đồng bộ Thương hiệu NexusKeys & Việt hóa Toàn diện
Loại bỏ hoàn toàn các chuỗi văn bản tiếng Anh và logo Epic Games Store còn sót lại trên Storefront (Home.razor, Login.razor, Register.razor, ForgotPassword.razor, ResetPassword.razor, Footer.razor, ProfileLayout.razor, NewsList.razor). Thay thế bằng thương hiệu và logo chuẩn NexusKeys, chuẩn hóa <PageTitle> toàn trang sang Tiếng Việt.

### R2. Tối ưu Luồng Tương tác & Trải nghiệm Người dùng (UX Flows)
Gắn kết sự kiện tương tác thật cho các nút Bookmark/Wishlist trên trang chủ kèm thông báo Toast. Bổ sung hiển thị giá bán / nhãn giảm giá trong Live Search Dropdown của Navbar và hỗ trợ tìm kiếm trên Mobile. Bổ sung thanh tiến trình trực quan (Checkout Stepper: Giỏ hàng → Thanh toán → Hoàn tất) trong luồng mua sắm. Bổ sung Badge trạng thái đơn hàng tiếng Việt có màu sắc trực quan trên Dashboard Admin.

### R3. Khả năng Tiếp cận (WCAG 2.1 AA) & Vùng Chạm Di động (Touch Targets)
Nâng cao độ tương phản các đoạn văn bản phụ/hướng dẫn bị mờ (text-white/30, text-white/40) lên mức dễ đọc (>= 4.5:1). Tối ưu kích thước các nút bấm icon và bộ lọc đạt chuẩn thân thiện với màn hình cảm ứng di động.

### R4. Bảo toàn Kiểm thử & Tính Toàn vẹn Hệ thống
Tuyệt đối không chỉnh sửa, xóa hoặc làm suy yếu bất kỳ test case nào trong thư mục tests/. Giữ nguyên các thuộc tính id, type, @bind của các trường nhập liệu trong form Auth để không phá vỡ logic kiểm thử và hành vi hiện có.

## Acceptance Criteria

### Build & Test Inviolability
- [ ] Lệnh dotnet build GameStore.slnx biên dịch thành công với 0 lỗi (0 errors).
- [ ] Lệnh git diff --stat tests/ hoàn toàn trống (0 diff trên toàn bộ thư mục tests/).
- [ ] Toàn bộ 72/72 test cases trong tests/GameStore.API.ProductKeyTests/ đều Passed 100%.

### Brand & Localization Completeness
- [ ] 100% các trang Auth (/login, /register, /forgot-password, /reset-password) hiển thị hoàn toàn bằng Tiếng Việt và mang nhận diện NexusKeys.
- [ ] Không còn chuỗi văn bản "Sign in to Epic Games", "Made by Epic Games", "Featured Discounts" trên giao diện người dùng.

### Interactive Micro-interactions
- [ ] Thao tác bấm Yêu thích trên trang chủ có phản hồi người dùng rõ ràng (Toast notification).
- [ ] Dropdown gợi ý tìm kiếm trên Navbar hiển thị tên và giá game rõ ràng.
- [ ] Bảng trạng thái đơn hàng trên Admin Dashboard hiển thị nhãn tiếng Việt kèm badge màu phân biệt.


## 2026-10-16T11:05:02Z

Sửa chữa và chuẩn hóa triệt để giao diện người dùng (UI/UX) NexusKeys Storefront & Admin: Khôi phục toàn bộ các class CSS tiện ích bị thiếu, tối ưu khoảng cách và cấu trúc chữ Tiếng Việt (Typography & Letter-spacing), dọn dẹp xung đột 4 file CSS, và định vị chính xác các thành phần bố cục theo đặc tả tại ui_ux_deep_defect_report.md.

Working directory: c:\Users\kynhu\Documents\DATN\datn
Integrity mode: demo

[Tài liệu kiểm toán chi tiết: ui_ux_deep_defect_report.md]

## Requirements

### R1. Bổ sung Đầy đủ Các Class CSS Tiện ích Bị Thiếu (Utility Classes Restoration)
Bổ sung toàn bộ 343 class Tailwind tiện ích đang được sử dụng trong các component .razor của Storefront nhưng chưa có trong file CSS (bao gồm các màu nền bg-[#121212], bg-[#18181c], bg-[#202024], bg-[#202020], bg-[#1a1a1a], bg-[#252525], bg-[#303030]; màu thương hiệu bg-[#0078f2], bg-[#0078F2], text-[#0078f2], text-[#0078F2]; các biến thể hover hover:bg-[#006bd8], hover:bg-[#0060c2]; độ phân cấp chữ text-white/70, text-white/80, text-white/65, text-gray-400; kích thước min-w-[44px], min-h-[44px], max-w-[1440px], h-[2px], w-1.5, h-1.5; và hệ thống lưới grid-cols-3, sm:grid-cols-3, sm:grid-cols-4, xl:grid-cols-4). Đồng thời bổ sung 130 class tương ứng còn thiếu trong NexusKeys.Admin (bao gồm xl:col-span-2, w-1.5, h-1.5, bg-amber-500, focus:ring-2, v.v.).

### R2. Tối ưu Cấu trúc Chữ & Khoảng Cách Chữ Tiếng Việt (Vietnamese Typography Harmonization)
Chuẩn hóa letter-spacing của toàn bộ tiêu đề h1–h6 từ mức ép dính -0.045em đến -0.075em về khoảng an toàn -0.01em đến 0em, nâng line-height từ 0.92–1.12 lên tối thiểu 1.25–1.35 để các ký tự có dấu thanh kép (Ư, Ơ, Ấ, Ầ, Ẩ, Ẫ, Ậ, Ắ, Ằ, Ẳ, Ẵ, Ặ, Ế, Ề, Ể, Ễ, Ệ, Ố, Ồ, Ổ, Ỗ, Ộ) không bị dính chùm vào nhau và không bị cắt cụt đầu dấu. Điều chỉnh các nhãn in hoa nhỏ (uppercase) từ tracking-widest (0.1em–0.15em) về tracking-wider (0.04em–0.05em), loại bỏ hoặc định nghĩa rõ ràng cỡ chữ text-[9px] để không bị vỡ kích thước badge.

### R3. Dọn dẹp Xung đột 4 File CSS & Quy tắc Gây Mất Màu (CSS Cascade De-duplication)
Xóa bỏ các khai báo @font-face lỗi trỏ về máy chủ Epic Games trong epic-games.css và atlas.css, thống nhất 100% font chữ Be Vietnam Pro trên toàn hệ thống. Loại bỏ các quy tắc CSS mang tính phá hoại trong nexus-ui.css (đặc biệt là quy tắc ép [class*="bg-emerald-"] và [class*="bg-red-"] thành nền mờ nhạt, và quy tắc .nui-main [class*="shadow-"] { box-shadow: none !important }). Sửa màu chữ của .egs-pill-discount từ #000000 sang #ffffff trên nền xanh #0078f2.

### R4. Sửa Chữa Bố Cục & Vị Trí Các Phần Tử (Positioning & Layout Correction)
Sửa lỗi định vị nút con mắt ẩn/hiện mật khẩu trong Login.razor và Register.razor (đảm bảo luôn nằm ở góc phải ô input, không đè lên chữ mật khẩu). Khôi phục đường kẻ nối 3 bước trên Checkout Stepper (h-[2px]). Đảm bảo danh sách sản phẩm trên ProductList.razor hiển thị đúng lưới 3 cột / 4 cột trên màn hình vừa và lớn. Đảm bảo biểu đồ doanh thu và bảng đơn hàng gần đây trên Admin Dashboard hiển thị đủ tỷ lệ 2 cột (xl:col-span-2) trong lưới 3 cột.

### R5. Bảo toàn Kiểm thử Tuyệt đối (Test Suite Inviolability)
Nghiêm cấm chỉnh sửa, xóa hoặc làm suy yếu bất kỳ file kiểm thử nào trong thư mục tests/. Giữ nguyên toàn bộ thuộc tính id, type, @bind của các input form Auth (login-email, login-password, register-username, register-email, register-password, v.v.) để bảo toàn 100% 72 test case của hệ thống.

## Acceptance Criteria

### Build & Test Inviolability
- [ ] Lệnh dotnet build GameStore.slnx biên dịch thành công với 0 lỗi (0 errors).
- [ ] Lệnh dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj biên dịch thành công với 0 lỗi (0 errors).
- [ ] Lệnh git diff --stat tests/ hoàn toàn trống (0 diff trên toàn bộ thư mục tests/).
- [ ] Toàn bộ 72/72 test cases trong tests/GameStore.API.ProductKeyTests/ đều Passed 100%.

### CSS Utility Coverage (Zero Missing Classes)
- [ ] Toàn bộ 343 class Tailwind trong các file .razor đều có định nghĩa CSS hợp lệ trong các file stylesheet của Storefront (script quét tự động trả về 0 missing classes).
- [ ] Toàn bộ 130 class trong các file .cshtml của Admin đều có định nghĩa CSS hợp lệ trong stylesheet của Admin.

### Typography & Spacing Verification
- [ ] Các tiêu đề h1–h6 trên Storefront hiển thị thoáng, không dính chữ, dấu tiếng Việt rõ ràng, letter-spacing nằm trong khoảng -0.01em đến 0em.
- [ ] Các nhãn badge in hoa không bị giãn cách rạc chữ (tracking <= 0.05em), không còn badge bị phình to do thiếu class cỡ chữ.

### Visual & Color Polish
- [ ] Các card và khung bề mặt (#18181c, #202024) hiển thị màu nền tối rõ ràng, không bị trong suốt.
- [ ] Toàn bộ các nút bấm chính (+ Nạp, Nạp Tiền Vào Ví, Kiểm Tra Đơn Hàng, Xem Thư Viện Key, nút gửi AI, nút Stepper active) hiển thị đúng màu nền xanh thương hiệu #0078f2 với chữ trắng sắc nét.
- [ ] Badge giảm giá trên Trang chủ hiển thị chữ trắng (#ffffff) trên nền xanh (#0078f2), độ tương phản cao đạt chuẩn WCAG.
- [ ] Icon con mắt mật khẩu nằm chuẩn xác ở phía bên phải trong ô input của Login.razor và Register.razor.
- [ ] Biểu đồ và bảng đơn hàng trên Admin Dashboard hiển thị chuẩn lưới 2 cột / 3 cột (xl:col-span-2).


## 2026-10-16T11:47:39Z

This is a single self-contained fix; keep it small and focused.

Sửa chữa và chuẩn hóa triệt để giao diện người dùng (UI/UX) NexusKeys Storefront & Admin: Khôi phục toàn bộ các class CSS tiện ích bị thiếu, tối ưu khoảng cách và cấu trúc chữ Tiếng Việt (Typography & Letter-spacing), dọn dẹp xung đột 4 file CSS, và định vị chính xác các thành phần bố cục theo đặc tả tại ui_ux_deep_defect_report.md.

Working directory: c:\Users\kynhu\Documents\DATN\datn
Integrity mode: demo

[Tài liệu kiểm toán chi tiết: ui_ux_deep_defect_report.md]

## Requirements

### R1. Bổ sung Đầy đủ Các Class CSS Tiện ích Bị Thiếu (Utility Classes Restoration)
Bổ sung toàn bộ 343 class Tailwind tiện ích đang được sử dụng trong các component .razor của Storefront nhưng chưa có trong file CSS (bao gồm các màu nền bg-[#121212], bg-[#18181c], bg-[#202024], bg-[#202020], bg-[#1a1a1a], bg-[#252525], bg-[#303030]; màu thương hiệu bg-[#0078f2], bg-[#0078F2], text-[#0078f2], text-[#0078F2]; các biến thể hover hover:bg-[#006bd8], hover:bg-[#0060c2]; độ phân cấp chữ text-white/70, text-white/80, text-white/65, text-gray-400; kích thước min-w-[44px], min-h-[44px], max-w-[1440px], h-[2px], w-1.5, h-1.5; và hệ thống lưới grid-cols-3, sm:grid-cols-3, sm:grid-cols-4, xl:grid-cols-4). Đồng thời bổ sung 130 class tương ứng còn thiếu trong NexusKeys.Admin (bao gồm xl:col-span-2, w-1.5, h-1.5, bg-amber-500, focus:ring-2, v.v.).

### R2. Tối ưu Cấu trúc Chữ & Khoảng Cách Chữ Tiếng Việt (Vietnamese Typography Harmonization)
Chuẩn hóa letter-spacing của toàn bộ tiêu đề h1–h6 từ mức ép dính -0.045em đến -0.075em về khoảng an toàn -0.01em đến 0em, nâng line-height từ 0.92–1.12 lên tối thiểu 1.25–1.35 để các ký tự có dấu thanh kép (Ư, Ơ, Ấ, Ầ, Ẩ, Ẫ, Ậ, Ắ, Ằ, Ẳ, Ẵ, Ặ, Ế, Ề, Ể, Ễ, Ệ, Ố, Ồ, Ổ, Ỗ, Ộ) không bị dính chùm vào nhau và không bị cắt cụt đầu dấu. Điều chỉnh các nhãn in hoa nhỏ (uppercase) từ tracking-widest (0.1em–0.15em) về tracking-wider (0.04em–0.05em), loại bỏ hoặc định nghĩa rõ ràng cỡ chữ text-[9px] để không bị vỡ kích thước badge.

### R3. Dọn dẹp Xung đột 4 File CSS & Quy tắc Gây Mất Màu (CSS Cascade De-duplication)
Xóa bỏ các khai báo @font-face lỗi trỏ về máy chủ Epic Games trong epic-games.css và atlas.css, thống nhất 100% font chữ Be Vietnam Pro trên toàn hệ thống. Loại bỏ các quy tắc CSS mang tính phá hoại trong nexus-ui.css (đặc biệt là quy tắc ép [class*="bg-emerald-"] và [class*="bg-red-"] thành nền mờ nhạt, và quy tắc .nui-main [class*="shadow-"] { box-shadow: none !important }). Sửa màu chữ của .egs-pill-discount từ #000000 sang #ffffff trên nền xanh #0078f2.

### R4. Sửa Chữa Bố Cục & Vị Trí Các Phần Tử (Positioning & Layout Correction)
Sửa lỗi định vị nút con mắt ẩn/hiện mật khẩu trong Login.razor và Register.razor (đảm bảo luôn nằm ở góc phải ô input, không đè lên chữ mật khẩu). Khôi phục đường kẻ nối 3 bước trên Checkout Stepper (h-[2px]). Đảm bảo danh sách sản phẩm trên ProductList.razor hiển thị đúng lưới 3 cột / 4 cột trên màn hình vừa và lớn. Đảm bảo biểu đồ doanh thu và bảng đơn hàng gần đây trên Admin Dashboard hiển thị đủ tỷ lệ 2 cột (xl:col-span-2) trong lưới 3 cột.

### R5. Bảo toàn Kiểm thử Tuyệt đối (Test Suite Inviolability)
Nghiêm cấm chỉnh sửa, xóa hoặc làm suy yếu bất kỳ file kiểm thử nào trong thư mục tests/. Giữ nguyên toàn bộ thuộc tính id, type, @bind của các input form Auth (login-email, login-password, register-username, register-email, register-password, v.v.) để bảo toàn 100% 72 test case của hệ thống.

## Acceptance Criteria

### Build & Test Inviolability
- [ ] Lệnh dotnet build GameStore.slnx biên dịch thành công với 0 lỗi (0 errors).
- [ ] Lệnh dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj biên dịch thành công với 0 lỗi (0 errors).
- [ ] Lệnh git diff --stat tests/ hoàn toàn trống (0 diff trên toàn bộ thư mục tests/).
- [ ] Toàn bộ 72/72 test cases trong tests/GameStore.API.ProductKeyTests/ đều Passed 100%.

### CSS Utility Coverage (Zero Missing Classes)
- [ ] Toàn bộ 343 class Tailwind trong các file .razor đều có định nghĩa CSS hợp lệ trong các file stylesheet của Storefront (script quét tự động trả về 0 missing classes).
- [ ] Toàn bộ 130 class trong các file .cshtml của Admin đều có định nghĩa CSS hợp lệ trong stylesheet của Admin.

### Typography & Spacing Verification
- [ ] Các tiêu đề h1–h6 trên Storefront hiển thị thoáng, không dính chữ, dấu tiếng Việt rõ ràng, letter-spacing nằm trong khoảng -0.01em đến 0em.
- [ ] Các nhãn badge in hoa không bị giãn cách rạc chữ (tracking <= 0.05em), không còn badge bị phình to do thiếu class cỡ chữ.

### Visual & Color Polish
- [ ] Các card và khung bề mặt (#18181c, #202024) hiển thị màu nền tối rõ ràng, không bị trong suốt.
- [ ] Toàn bộ các nút bấm chính (+ Nạp, Nạp Tiền Vào Ví, Kiểm Tra Đơn Hàng, Xem Thư Viện Key, nút gửi AI, nút Stepper active) hiển thị đúng màu nền xanh thương hiệu #0078f2 với chữ trắng sắc nét.
- [ ] Badge giảm giá trên Trang chủ hiển thị chữ trắng (#ffffff) trên nền xanh (#0078f2), độ tương phản cao đạt chuẩn WCAG.
- [ ] Icon con mắt mật khẩu nằm chuẩn xác ở phía bên phải trong ô input của Login.razor và Register.razor.
- [ ] Biểu đồ và bảng đơn hàng trên Admin Dashboard hiển thị chuẩn lưới 2 cột / 3 cột (xl:col-span-2).

## 2026-10-16T14:10:26Z

This is a single self-contained fix; keep it small and focused.

Nâng cấp và chuẩn hóa toàn diện hệ thống cỡ chữ (Typographic Scale & Font Size Hierarchy) cho NexusKeys Game Store (Storefront & Admin): Loại bỏ triệt để các cỡ chữ siêu nhỏ (< 11px) vi phạm khả năng đọc tiếng Việt, nâng cấp Body text lên 14px (text-sm) và đồng bộ Badge/Chip trạng thái đạt chuẩn WCAG 2.1 AA.

Working directory: c:\Users\kynhu\Documents\DATN\datn
Integrity mode: demo

## Requirements

### R1. Loại Bỏ Triệt Để Cỡ Chữ Siêu Nhỏ (< 11px)
Rà soát và thay thế toàn bộ các class text-[9px] và text-[10px] trong các component Storefront (Navbar.razor, Footer.razor, Wallet.razor, ProfileLayout.razor, Profile.razor, ProductList.razor, ProductDetail.razor, Distribution.razor, Assistant.razor, ChatBubble.razor, BrandLogo.razor) và Admin (Index.cshtml, _Layout.cshtml). Nâng cấp các nhãn này lên tối thiểu text-[11px] hoặc text-xs (12px) kèm độ đậm phù hợp (font-semibold / font-bold), đảm bảo toàn bộ chữ tiếng Việt có dấu thanh kép (Ư, Ơ, Đ, Ấ, Ắ, Ế, Ố) hiển thị sắc nét, không bị nhòe vỡ hạt trên màn hình độ phân giải tiêu chuẩn và thiết bị di động.

### R2. Chuẩn Hóa Phân Cấp Văn Bản Nội Dung (Body Text & Card Descriptions)
Nâng cấp các đoạn văn bản mô tả, hướng dẫn chính sách, chú thích phụ đang bị ép ở mức text-xs (12px) hoặc 11px lên chuẩn text-sm (14px) với khoảng cách dòng thoáng (leading-relaxed hoặc leading-normal). Giữ text-xs (12px) cho các thông số kỹ thuật phụ (meta details, ngày tháng, bản quyền).

### R3. Đồng Bộ Hóa Kích Thước Badge & Chip Trạng Thái
Chuẩn hóa toàn bộ chip giảm giá phần trăm (.egs-pill-discount, badge trên card game), badge trạng thái đơn hàng (Đã giao, Đang xử lý, Thất bại), và chip danh mục game trên Storefront và Admin về kích thước chuẩn text-xs (12px) hoặc text-[11px], padding chuẩn (px-2 py-0.5 hoặc px-2.5 py-1), đảm bảo độ cao dòng tối thiểu 16px để không bị cắt mép badge.

### R4. Tối Ưu Phân Cấp Tiêu Đề Responsive (Mobile Headings Scaling)
Bổ sung các breakpoint responsive cho các tiêu đề trang lớn (h1 từ text-2xl sm:text-4xl lg:text-5xl, h2 từ text-xl sm:text-2xl) trên các trang chủ, chi tiết sản phẩm, và trang phân phối để tiêu đề không bị choán màn hình hoặc ngắt dòng bất hợp lý trên smartphone.

### R5. Bảo Toàn Kiểm Thử Tuyệt Đối (Test Suite Inviolability)
Nghiêm cấm chỉnh sửa, xóa hoặc làm suy yếu bất kỳ file kiểm thử nào trong thư mục tests/. Giữ nguyên toàn bộ thuộc tính id, type, @bind của các input form Auth (login-email, login-password, register-username, register-email, register-password, v.v.) để bảo toàn 100% 72 test case của hệ thống.

## Acceptance Criteria

### Build & Test Inviolability
- [ ] Lệnh dotnet build GameStore.slnx biên dịch thành công với 0 lỗi (0 errors).
- [ ] Lệnh dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj biên dịch thành công với 0 lỗi (0 errors).
- [ ] Lệnh git diff --stat tests/ hoàn toàn trống (0 diff trên toàn bộ thư mục tests/).
- [ ] Toàn bộ 72/72 test cases trong tests/GameStore.API.ProductKeyTests/ đều Passed 100%.

### Typographic Scale & Zero Sub-11px Classes
- [ ] Script quét tự động xác nhận 0 class text-[9px] và 0 class text-[10px] còn sót lại trong toàn bộ mã nguồn .razor và .cshtml.
- [ ] Toàn bộ các nhãn, badge, tag đều có kích thước tối thiểu >= 11px, chiều cao dòng >= 14px.
- [ ] Các đoạn văn bản mô tả chính sách, hướng dẫn hiển thị ở mức text-sm (14px) dễ đọc, đạt chuẩn WCAG 2.1 AA.
- [ ] Tiêu đề h1–h2 co giãn responsive mượt mà trên mobile, không bị vỡ bố cục.


## 2026-10-16T17:01:30Z

This is a single self-contained fix; keep it small and focused.

Phát triển hệ thống phân loại kép và bộ lọc chuyên sâu cho 2 dòng sản phẩm cốt lõi: Trò chơi (Game) và Ứng dụng (App/Software), đồng thời gắn nhãn nhận diện trực quan kép trên tất cả thẻ sản phẩm trong toàn hệ thống NexusKeys Game Store theo đúng sở thích người dùng.

Working directory: c:\Users\kynhu\Documents\DATN\datn
Integrity mode: demo

## Requirements

### R1. Thanh Tabs Chuyển Nhanh Phân Loại Cấp Cao (Game vs App Tabs)
Bổ sung thanh Tabs chuyển đổi nhanh trên đầu trang /products:
- [ Tất cả sản phẩm ]
- [ 🎮 Trò chơi (Game) ]
- [ 💻 Ứng dụng & Phần mềm (App) ]
Khi người dùng bấm chọn:
- Cập nhật URL query parameter (ví dụ type=Game hoặc type=Software).
- Tải lại dữ liệu tương ứng qua API (/api/v1/products?type=...).
- Tab đang kích hoạt hiển thị nổi bật với viền sáng hoặc nền xanh thương hiệu #0078f2.

### R2. Bộ Lọc Chuyên Sâu Theo Từng Loại Tại Sidebar (Contextual Sub-Filters)
Tại thanh bên (Sidebar) của trang /products:
- Bổ sung nhóm bộ lọc "Loại sản phẩm" (Game / Ứng dụng / Thẻ quà tặng).
- Tự động chuyển đổi danh sách thể loại hiển thị theo ngữ cảnh:
  - Khi chọn Trò chơi: Hiển thị các thể loại game (Hành động, Nhập vai RPG, Chiến thuật, Bắn súng, Thể thao, Mô phỏng, Thế giới mở, Sinh tồn, Kinh dị, Đua xe).
  - Khi chọn Ứng dụng: Hiển thị các nhóm phần mềm (Đồ họa & Thiết kế 3D, Biên tập Video & Âm thanh, Lập trình & Tiện ích, Công cụ bản quyền).
  - Khi chọn Tất cả: Hiển thị danh mục tổng hợp đầy đủ.

### R3. Gắn Nhãn Nhận Diện Kép Trên Thẻ Sản Phẩm (Dual Badge Product Identification)
Trên component ProductCard.razor (hiển thị trên Trang chủ, Danh mục /products, Yêu thích /wishlist, và Live Search):
- Huy hiệu trên góc poster (Top-Left):
  - Nếu là Game: Huy hiệu 🎮 GAME màu xanh công nghệ #0078f2 với lớp nền mờ bg-[#0078f2]/90 text-white shadow-md.
  - Nếu là Ứng dụng: Huy hiệu 💻 ỨNG DỤNG hoặc 💻 APP màu tím neon / ngọc lục bảo bg-purple-600/90 text-white shadow-md (hoặc bg-emerald-600/90).
  - Nếu là Thẻ quà tặng: Huy hiệu 🎁 GIFT CARD màu hổ phách bg-amber-600/90 text-white.
- Nhãn phân loại chi tiết bên dưới:
  - Dòng meta text dưới tên hiển thị cụ thể: "Trò chơi cơ bản", "Phiên bản đặc biệt", "Phần mềm đồ họa", "Ứng dụng tiện ích", v.v., kèm màu sắc hài hòa.

### R4. Đồng Bộ Hóa Chi Tiết Sản Phẩm & Live Search Dropdown
- Trang ProductDetail.razor: Hiển thị badge nhận diện Loại sản phẩm ở phần đầu trang (Game vs Ứng dụng) và các thông số phù hợp.
- Live Search Dropdown trên Navbar: Hiển thị icon hoặc chip nhỏ [🎮 Game] / [💻 App] bên cạnh tên sản phẩm để người dùng nhận biết ngay khi gõ tìm kiếm.

### R5. Bảo Toàn Kiểm Thử Tuyệt Đối (Test Suite Inviolability)
Nghiêm cấm sửa đổi, xóa hoặc làm suy yếu bất kỳ test case nào trong thư mục tests/. Giữ nguyên toàn bộ thuộc tính id, type, @bind của các input form Auth (login-email, login-password, register-username, register-email, register-password, v.v.) để bảo toàn 100% 72 test case của hệ thống.

## Acceptance Criteria

### Build & Test Inviolability
- [ ] Lệnh dotnet build GameStore.slnx biên dịch thành công với 0 lỗi (0 errors).
- [ ] Lệnh dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj biên dịch thành công với 0 lỗi (0 errors).
- [ ] Lệnh git diff --stat tests/ hoàn toàn trống (0 diff trên toàn bộ thư mục tests/).
- [ ] Toàn bộ 72/72 test cases trong tests/GameStore.API.ProductKeyTests/ đều Passed 100%.

### Feature Execution & Visual Verification
- [ ] Thanh Tabs trên trang /products chuyển đổi mượt mà giữa "Tất cả", "Trò chơi" và "Ứng dụng".
- [ ] 100% thẻ sản phẩm hiển thị huy hiệu góc poster và nhãn phân loại chi tiết phía dưới phân biệt rõ ràng Game vs Ứng dụng.
- [ ] Bộ lọc Sidebar hiển thị các thể loại/nhóm công cụ tương ứng theo loại sản phẩm đang chọn.
- [ ] Live Search Dropdown gắn chip nhận diện rõ Game hay Ứng dụng.


## 2026-10-17T04:11:05Z

This is a single self-contained fix; keep it small and focused.

Chuẩn hóa và nâng cấp toàn diện giao diện cửa hàng NexusKeys Storefront: Giữ duy nhất 2 dòng sản phẩm cốt lõi Trò chơi (Game) & Ứng dụng (App/Software), loại bỏ triệt để nhãn Steam/Thẻ quà tặng/Bộ lọc nền tảng, thiết kế lại trang sản phẩm hiện đại, tối ưu icon đánh giá Khuyên dùng / Không khuyên dùng chuẩn SVG, mở rộng lưới mini card trên Trang chủ thành 6 thẻ (3 thẻ/dòng), đồng thời quảng bá mạnh mẽ các phần mềm bản quyền với đúng hình ảnh đặc trưng.

Working directory: c:\Users\kynhu\Documents\DATN\datn
Integrity mode: demo

## Requirements

### R1. Tinh Gọn Danh Mục: Chỉ Giữ Game & App, Loại Bỏ Triệt Để Thẻ Quà Tặng
- Loại bỏ hoàn toàn lựa chọn "Thẻ quà tặng" (Gift Card) khỏi:
  - Thanh Tabs phân loại cấp cao và bộ lọc Sidebar trên `/products`.
  - Huy hiệu phân loại trên thẻ sản phẩm `ProductCard.razor`.
  - Bộ gợi ý tìm kiếm nhanh Live Search trên `Navbar.razor`.
- Toàn bộ hệ thống tập trung độc quyền vào 2 dòng sản phẩm:
  - 🎮 **Trò chơi (Game)**
  - 💻 **Ứng dụng & Phần mềm (App)**

### R2. Loại Bỏ Nhãn Nền Tảng (Steam) Trên Thẻ & Giữ Duy Nhất Nhãn Game / App
- Gỡ bỏ hoàn toàn badge nền tảng (nhãn Steam, Epic Games, v.v.) ở góc dưới phải ảnh bìa trong `ProductCard.razor`.
- Mỗi thẻ sản phẩm chỉ hiển thị huy hiệu nhận diện loại sản phẩm rõ nét ở góc trên bên trái:
  - `🎮 GAME` (màu xanh công nghệ `#0078f2`)
  - `💻 APP` (màu tím neon hoặc ngọc lục bảo)
- Dòng phân loại chi tiết phía dưới tên sản phẩm thể hiện rõ đặc tính (ví dụ: *Trò chơi cơ bản, Bản đặc biệt, Đồ họa & Thiết kế 3D, Lập trình & Tiện ích, Biên tập Video*).

### R3. Tinh Chỉnh Trang Danh Mục Sản Phẩm Đẹp & Bỏ Bộ Lọc Nền Tảng
- Trong file `ProductList.razor`:
  - Loại bỏ hoàn toàn nhóm bộ lọc "Nền tảng hỗ trợ" (Windows PC, macOS) ở Sidebar.
  - Tái cấu trúc thanh điều hướng phân loại: Tabs chọn chuyển đổi giữa Tất cả, Trò chơi và Ứng dụng với phong cách pill/tab hiện đại, hiệu ứng active sắc nét.
  - Bố cục lưới sản phẩm cân đối, lề thoáng đãng, đồng bộ màu nền bề mặt `#18181c` và viền `#303030`.

### R4. Tối Ưu Icon Đánh Giá Khuyên Dùng / Không Khuyên Dùng (`ProductDetail.razor`)
- Thay thế triệt để các emoji thô `👍` và `👎` bằng bộ icon SVG Heroicons chuyên nghiệp (`HandThumbUp` / `HandThumbDown`):
  - **Khuyên dùng**: Icon ngón tay cái hướng lên màu xanh ngọc lục bảo Emerald (`text-emerald-400`), nền mờ `bg-emerald-500/15`, viền `border-emerald-500/30`.
  - **Không khuyên dùng**: Icon ngón tay cái hướng xuống màu đỏ hồng Rose (`text-rose-400`), nền mờ `bg-rose-500/15`, viền `border-rose-500/30`.
- Đồng bộ cả trong form gửi nhận xét và danh sách đánh giá từ người dùng.

### R5. Mở Rộng Thẻ Ngang Mini Trên Trang Chủ Thành 6 Card (3 Card / Dòng)
- Tại mục "Khám phá tựa game mới" (`egs-mini-grid` trong `Home.razor`):
  - Mở rộng số lượng hiển thị từ 4 thẻ lên 6 thẻ mini.
  - Bố cục lưới mới: **3 thẻ trên một dòng** (2 dòng x 3 cột = 6 thẻ) trên màn hình trung bình và lớn (`grid-cols-1 sm:grid-cols-2 lg:grid-cols-3`).
  - Lựa chọn danh sách bao gồm cả tựa game hot và các phần mềm thịnh hành.

### R6. Quảng Bá Ứng Dụng/Phần Mềm Trên Trang Chủ & Chuẩn Hóa Đúng Hình Ảnh App
- Bổ sung khu vực/section giới thiệu ứng dụng nổi bật trên `Home.razor` (Banner quảng bá phần mềm đồ họa, lập trình, dựng phim).
- Sửa triệt để hàm ánh xạ ảnh trong `GameMediaHelper.cs`:
  - Tuyệt đối không dùng ảnh poster game (Control, Elden Ring, RDR2) cho ứng dụng phần mềm.
  - Ánh xạ đúng hình ảnh biểu trưng chất lượng cao cho các ứng dụng (Photoshop, Blender, Premiere Pro, FL Studio, Visual Studio, Windows 11, JetBrains, AutoCAD, Substance Painter, Wallpaper Engine).
- Mở rộng dữ liệu mẫu sản phẩm phần mềm đa dạng trong hệ thống.

### R7. Bảo Toàn Kiểm Thử Tuyệt Đối (Test Suite Inviolability)
- Tuyệt đối KHÔNG sửa đổi, xóa hoặc làm suy yếu bất kỳ test case nào trong thư mục `tests/` (`git diff --stat tests/` phải bằng 0).
- Giữ nguyên toàn bộ thuộc tính `id`, `type`, `@bind` của các input form Auth (`login-email`, `login-password`, `register-username`, `register-email`, `register-password`, v.v.).
- Đảm bảo toàn bộ 72/72 test case đều Passed 100%.

## Acceptance Criteria

### Build & Test Inviolability
- [ ] Lệnh `dotnet build GameStore.slnx` biên dịch thành công với 0 lỗi (0 errors).
- [ ] Lệnh `dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj` biên dịch thành công với 0 lỗi (0 errors).
- [ ] Lệnh `git diff --stat tests/` hoàn toàn trống (0 diff trên toàn bộ thư mục `tests/`).
- [ ] Toàn bộ 72/72 test cases trong `tests/GameStore.API.ProductKeyTests/` đều Passed 100%.

### Feature Execution & Visual Verification
- [ ] Hoàn toàn không còn xuất hiện "Thẻ quà tặng" (Gift Card) và "Nền tảng hỗ trợ" (Platform filter) trên trang `/products`.
- [ ] Toàn bộ thẻ sản phẩm không còn hiển thị nhãn "Steam" ở góc dưới; chỉ còn nhãn `🎮 GAME` hoặc `💻 APP` ở góc trên và phân loại chi tiết bên dưới.
- [ ] Icon đánh giá trong trang chi tiết sản phẩm hiển thị bằng SVG Heroicon chuyên nghiệp (Emerald Thumb Up / Rose Thumb Down), không còn emoji thô.
- [ ] Khung card ngang mini trên Trang chủ hiển thị chuẩn 6 thẻ xếp thành 3 thẻ / 1 dòng trên màn hình lớn.
- [ ] Trang chủ có khu vực quảng bá ứng dụng/phần mềm bản quyền rõ ràng.
- [ ] Tất cả các sản phẩm phần mềm (Photoshop, Blender, IDE, Video Editor...) hiển thị đúng hình ảnh ứng dụng đặc trưng, không còn bị nhầm sang hình game.
