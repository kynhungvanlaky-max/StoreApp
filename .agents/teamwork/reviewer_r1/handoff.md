# Reviewer R1 Handoff Report

> [!WARNING] **Skepticism Disclaimer**
> Đã xác minh tính toàn vẹn 100% của test suite (72/72 passed, 0 diff trên `tests/`) và biên dịch sạch toàn bộ solution, tuy nhiên môi trường kiểm thử là headless CLI (không có display browser vật lý để chụp lại screenshot render runtime).

## 1. What the prior attempt got wrong
- **Lỗi nhận diện AppId do so khớp chuỗi con quá rộng (`GameMediaHelper.FindAppId`)**:
  - *Input*: Sản phẩm có tên ngắn hoặc chứa các từ đơn lẻ ("A", "Pro", "War").
  - *Expected*: `FindAppId` chỉ khớp chính xác hoặc khớp tên game dài có nghĩa, không bắt nhầm các từ ngắn vào các tựa game như "Agent A" (appId 801480) hay "VEGAS Pro" (appId 1325400).
  - *Actual*: Do logic `kvp.Key.Contains(name, StringComparison.OrdinalIgnoreCase)` không kiểm tra độ dài tối thiểu của `name`, chuỗi ngắn 1 ký tự như "A" cũng bị khớp vào "Agent A", biến sản phẩm bất kỳ thành game Steam.
  - *Root cause*: `FindAppId` thiếu điều kiện ràng buộc độ dài `name.Length >= 5` trước khi kiểm tra `kvp.Key.Contains(name)`.
- **Hàm ánh xạ ảnh thiếu ngữ cảnh phân loại (`Product.Type` và `Product.Categories`)**:
  - *Input*: Một sản phẩm phần mềm mới trong DB có tên độc lạ hoặc tùy biến không nằm trong từ khóa cố định, `Type = "App"`, `ImageUrl = null`.
  - *Expected*: Hiển thị ảnh cover/banner/capsule đặc trưng của phần mềm (Wallpaper Engine 431960).
  - *Actual*: `GetCover(name, fallbackUrl)` chỉ nhận `name` và `fallbackUrl`, không nhận `type` hay `categories`, dẫn đến fallback về ảnh game Black Myth: Wukong (2358720), vi phạm trực tiếp R6 ("Tuyệt đối không dùng ảnh poster game cho ứng dụng phần mềm").
  - *Root cause*: Chữ ký của `GetCover`, `GetHeroBanner`, `GetCapsule`, `GetMediaGallery` thiếu tham số ngữ cảnh `string? type = null, IEnumerable<string>? categories = null`.
- **Thiếu đồng bộ trải nghiệm người dùng theo ngữ cảnh sản phẩm trong `ProductDetail.razor`**:
  - *Input*: Truy cập trang chi tiết một ứng dụng phần mềm (Adobe Photoshop, Blender, Premiere Pro, v.v.).
  - *Expected*: Các văn bản mô tả, placeholder đánh giá, tỷ lệ hài lòng và thông tin phiên bản phù hợp với ứng dụng (ví dụ: "Dựa trên người dùng đã mua", "Các gói bản quyền & Tiện ích bổ sung", "Bao gồm ứng dụng gốc...").
  - *Actual*: Vẫn cố định nội dung cho game: "Dựa trên người chơi đã mua", "Hầu hết người chơi đều hài lòng với chất lượng tựa game", "Các phiên bản & Tiện ích bổ sung", "Bao gồm trò chơi gốc...".
  - *Root cause*: Các nhãn văn bản trong tab Editions và Reviews chưa được rẽ nhánh theo `GameMediaHelper.IsApp(product.Name, product.Type, product.Categories)`.
- **Đường dẫn liên kết thẻ phần mềm trên Trang chủ (`Home.razor`) chưa chuyển đúng bộ lọc**:
  - *Input*: Người dùng bấm vào thẻ phần mềm mini (Blender, Wallpaper Engine) hoặc 4 thẻ trong Section 5 Software Showcase trên Trang chủ.
  - *Expected*: Chuyển hướng tới `/products?type=App` để lọc ngay kho phần mềm.
  - *Actual*: Trỏ về `/products` chung không có filter type, khiến người dùng phải thao tác chọn lại tab.
  - *Root cause*: `href="/products"` thay vì `href="/products?type=App"`.

## 2. What I changed
- `src/NexusKeys.Storefront/Services/GameMediaHelper.cs`:
  - Thêm điều kiện bảo vệ độ dài `(name.Length >= 5 && kvp.Key.Contains(name, ...))` trong `FindAppId` để tránh việc các chuỗi ký tự ngắn bị khớp nhầm.
  - Mở rộng chữ ký của `GetCover`, `GetHeroBanner`, `GetCapsule`, `GetMediaGallery` hỗ trợ `string? type = null, IEnumerable<string>? categories = null`.
  - Cập nhật logic fallback ảnh: Khi `IsApp(name, type, categories)` là `true`, fallback sang hình ảnh phần mềm chất lượng cao (Wallpaper Engine `431960`), tuyệt đối không fallback sang poster game.
- `src/NexusKeys.Storefront/Components/Shared/ProductCard.razor`:
  - Truyền `Product.Type` và `Product.Categories` vào `GameMediaHelper.GetCover`.
- `src/NexusKeys.Storefront/Components/Pages/Products/ProductDetail.razor`:
  - Truyền `product.Type` và `product.Categories` vào `GetCover`, `GetHeroBanner`, `GetCapsule`, `GetMediaGallery` và khi thêm vào giỏ hàng.
  - Tối ưu ngữ cảnh văn bản cho ứng dụng: Placeholder đánh giá, subtext đánh giá cộng đồng, tỷ lệ hài lòng, tiêu đề và mô tả gói Standard / Deluxe Edition.
  - Xóa bỏ chú thích cũ còn nhắc đến Gift Card.
- `src/NexusKeys.Storefront/Components/Layout/Navbar.razor`:
  - Truyền `product.Type` và `product.Categories` vào `GetCover` trong kết quả tìm kiếm live search cả trên desktop và mobile drawer.
- `src/NexusKeys.Storefront/Components/Pages/Home.razor`:
  - Cập nhật liên kết thẻ mini Blender 3D Studio, Wallpaper Engine và cả 4 thẻ Section 5 Creative Software sang `/products?type=App`.

## 3. Verification Record
- **Deep Verification (ran actual tests):**
  - `git diff --stat tests/` -> 0 diff (Bảo toàn 100% test suite, tuyệt đối không chỉnh sửa test).
  - `dotnet test tests/GameStore.API.ProductKeyTests/` -> Passed! Failed: 0, Passed: 72, Skipped: 0, Total: 72 (thời gian chạy 13s).
  - `dotnet build GameStore.slnx` -> Build succeeded. 0 Error(s).
  - `dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj` -> Build succeeded. 0 Warning(s), 0 Error(s).
- **Shallow Verification (manual inspection):**
  - Kiểm tra toàn bộ codebase không còn nhãn Steam badge trên card, không còn bộ lọc nền tảng trên sidebar `/products`, không còn emoji `👍` / `👎`.
  - Kiểm tra tính nhất quán giữa CSS Grid (`repeat(3, 1fr)`) và Tailwind class (`grid-cols-1 sm:grid-cols-2 lg:grid-cols-3`) trên `.egs-mini-grid`.
- **Unverified aspects:**
  - Chưa chạy live UI browser automation test với headless Chrome / Playwright trên runtime Blazor Server.

## 4. Known Issues
- `Shallow Verification` — Chưa có bài test E2E mô phỏng hành vi click chuyển tab trực tiếp trên trình duyệt thực tế.
- `Minor Robustness Risk` — Dữ liệu danh mục và sản phẩm phụ thuộc vào cơ chế ánh xạ chuỗi tiếng Việt không dấu (RemoveDiacritics), nếu tên nhập vào có ký tự unicode đặc biệt ngoài bảng mã Latin/Việt có thể fallback về phân loại mặc định.

## 5. Remaining risk & next step
- Toàn bộ 7 yêu cầu cốt lõi (R1 - R7) cùng các tiêu chí chấp nhận đã được hoàn thành, nghiệm thu và khắc phục triệt để. Task đã hoàn tất, sẵn sàng để bàn giao.
