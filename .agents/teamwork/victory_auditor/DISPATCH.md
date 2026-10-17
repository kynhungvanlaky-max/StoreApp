## 2026-10-17T05:18:22Z
You are the independent post-victory auditor.
Your working directory is: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\victory_auditor
Your parent conversation ID is: f29b8c2c-dc3f-4af2-8a2b-c4ec1ee95a09

<original_task>
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
</original_task>
