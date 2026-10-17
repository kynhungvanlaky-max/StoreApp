## 2026-10-16T11:06:41Z

You are the Project Orchestrator for NexusKeys Game Store UI/UX defect repairs and standardization.

Your working directory is: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\orchestrator_3
Your task context and user request are in: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\ORIGINAL_REQUEST.md (specifically the latest request under ## 2026-10-16T11:05:02Z).

Mission & Requirements:
1. R1: Bổ sung Đầy đủ Các Class CSS Tiện ích Bị Thiếu (Utility Classes Restoration):
   - Bổ sung toàn bộ 343 class Tailwind tiện ích đang được sử dụng trong các component .razor của Storefront (bg-[#121212], bg-[#18181c], bg-[#202024], bg-[#202020], bg-[#1a1a1a], bg-[#252525], bg-[#303030]; bg-[#0078f2], bg-[#0078F2], text-[#0078f2], text-[#0078F2]; hover:bg-[#006bd8], hover:bg-[#0060c2]; text-white/70, text-white/80, text-white/65, text-gray-400; min-w-[44px], min-h-[44px], max-w-[1440px], h-[2px], w-1.5, h-1.5; grid-cols-3, sm:grid-cols-3, sm:grid-cols-4, xl:grid-cols-4, v.v.).
   - Bổ sung 130 class tương ứng còn thiếu trong NexusKeys.Admin (xl:col-span-2, w-1.5, h-1.5, bg-amber-500, focus:ring-2, v.v.).
2. R2: Tối ưu Cấu trúc Chữ & Khoảng Cách Chữ Tiếng Việt (Vietnamese Typography Harmonization):
   - Chuẩn hóa letter-spacing h1–h6 về khoảng an toàn -0.01em đến 0em, nâng line-height lên tối thiểu 1.25–1.35 tránh dính chùm hoặc cắt dấu tiếng Việt.
   - Điều chỉnh nhãn in hoa nhỏ về tracking-wider (0.04em–0.05em), loại bỏ hoặc định nghĩa rõ ràng cỡ chữ text-[9px].
3. R3: Dọn dẹp Xung đột 4 File CSS & Quy tắc Gây Mất Màu (CSS Cascade De-duplication):
   - Xóa bỏ khai báo @font-face lỗi trỏ về máy chủ Epic Games trong epic-games.css và atlas.css, thống nhất 100% font chữ Be Vietnam Pro.
   - Loại bỏ quy tắc phá hoại trong nexus-ui.css (đặc biệt ép bg-emerald- và bg-red- thành mờ nhạt, và quy tắc .nui-main [class*="shadow-"] { box-shadow: none !important }).
   - Sửa màu chữ của .egs-pill-discount từ #000000 sang #ffffff trên nền xanh #0078f2.
4. R4: Sửa Chữa Bố Cục & Vị Trí Các Phần Tử (Positioning & Layout Correction):
   - Sửa lỗi định vị nút con mắt ẩn/hiện mật khẩu trong Login.razor và Register.razor (nằm góc phải ô input, không đè lên text, giữ nguyên id, type, @bind).
   - Khôi phục đường kẻ nối 3 bước trên Checkout Stepper (h-[2px]).
   - ProductList.razor hiển thị đúng lưới 3/4 cột.
   - Admin Dashboard biểu đồ & bảng đơn hàng hiển thị chuẩn lưới 2/3 cột (xl:col-span-2).
5. R5: Bảo toàn Kiểm thử Tuyệt đối (Test Suite Inviolability):
   - Nghiêm cấm chỉnh sửa, xóa hoặc làm suy yếu bất kỳ test case nào trong tests/ (git diff --stat tests/ phải hoàn toàn trống).
   - Giữ nguyên toàn bộ id, type, @bind của các input form Auth.
   - dotnet build GameStore.slnx thành công 0 errors.
   - dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj thành công 0 errors.
   - Toàn bộ 72/72 tests trong tests/GameStore.API.ProductKeyTests/ passed 100%.

Please maintain plan.md and progress.md in your working directory. Keep progress updated regularly. When done, write handoff.md and notify me via send_message.
