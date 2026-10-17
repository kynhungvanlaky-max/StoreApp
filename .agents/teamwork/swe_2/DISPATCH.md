## 2026-10-16T14:11:58Z

You are swe_2, a teamwork_preview_swe orchestrator.
Your working directory is c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\swe_2.
Project root is c:\Users\kynhu\Documents\DATN\datn.

Refer to the latest user request in c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\ORIGINAL_REQUEST.md (timestamp 2026-10-16T14:10:26Z):

Task: Nâng cấp và chuẩn hóa toàn diện hệ thống cỡ chữ (Typographic Scale & Font Size Hierarchy) cho NexusKeys Game Store (Storefront & Admin): Loại bỏ triệt để các cỡ chữ siêu nhỏ (< 11px) vi phạm khả năng đọc tiếng Việt, nâng cấp Body text lên 14px (text-sm) và đồng bộ Badge/Chip trạng thái đạt chuẩn WCAG 2.1 AA.

Key Requirements:
1. R1: Loại bỏ triệt để cỡ chữ siêu nhỏ (< 11px). Thay thế toàn bộ class text-[9px] và text-[10px] trong các component Storefront (Navbar.razor, Footer.razor, Wallet.razor, ProfileLayout.razor, Profile.razor, ProductList.razor, ProductDetail.razor, Distribution.razor, Assistant.razor, ChatBubble.razor, BrandLogo.razor) và Admin (Index.cshtml, _Layout.cshtml). Nâng cấp lên tối thiểu text-[11px] hoặc text-xs (12px) kèm font-semibold/font-bold, đảm bảo tiếng Việt có dấu thanh kép hiển thị sắc nét.
2. R2: Chuẩn hóa phân cấp văn bản nội dung (Body text & card descriptions). Nâng cấp các đoạn văn bản mô tả, hướng dẫn chính sách, chú thích phụ từ text-xs/11px lên text-sm (14px) với leading-relaxed/leading-normal. Giữ text-xs (12px) cho meta details, ngày tháng, bản quyền.
3. R3: Đồng bộ hóa kích thước Badge & Chip trạng thái (.egs-pill-discount, badge trạng thái đơn hàng Đã giao/Đang xử lý/Thất bại, chip danh mục) về text-xs hoặc text-[11px], padding px-2 py-0.5 hoặc px-2.5 py-1, line-height >= 16px.
4. R4: Tối ưu phân cấp tiêu đề responsive (h1 từ text-2xl sm:text-4xl lg:text-5xl, h2 từ text-xl sm:text-2xl) trên home, product detail, distribution.
5. R5: BẢO TOÀN KIỂM THỬ TUYỆT ĐỐI (DO NOT MODIFY TESTS): Tuyệt đối KHÔNG ĐƯỢC CHỈNH SỬA, XÓA, COMMENT HOẶC LÀM SUY YẾU BẤT KỲ TEST CASE NÀO trong tests/. Giữ nguyên toàn bộ id, type, @bind của các input form Auth (login-email, login-password, register-username, register-email, register-password, v.v.). git diff --stat tests/ phải hoàn toàn trống. Toàn bộ 72/72 test case phải Passed 100%.
6. Build verification: `dotnet build GameStore.slnx` and `dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj` must build with 0 errors.

Run the SWE Light loop (one implementer, adversarial reviewer rounds) until all acceptance criteria are met and verified by test execution. Keep progress.md updated. When complete, send a message to the Sentinel reporting victory.
