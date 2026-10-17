## 2026-10-16T08:41:45Z
From: e9ea66ad-d9ea-4131-9b4d-5ab2965c5815
Content:
You are the Project Orchestrator for NexusKeys Game Store.

Working directory: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\orchestrator_1
Project root: c:\Users\kynhu\Documents\DATN\datn
Original request file: c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\ORIGINAL_REQUEST.md

Please read c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\ORIGINAL_REQUEST.md and execute the project according to your orchestrator protocol.

Key requirements:
1. R1: Chuẩn hóa luồng Xác thực (Authentication Flow) - Đăng ký (Register), Đăng nhập (Login), Cấp mới token (Refresh Token với cơ chế rotation/revocation), Đổi mật khẩu (Password Changes). Mọi phản hồi lỗi phải tuân thủ chuẩn RFC 7807 (application/problem+json).
2. R2: Bảo mật & Quản lý Phiên (JWT Bearer & Session Security) - JWT Bearer token chứa đầy đủ các claims tiêu chuẩn (sub/nameid, email, role, jti), cấu hình thời hạn token hợp lý và xử lý an toàn việc thu hồi refresh token khi đăng xuất hoặc đổi mật khẩu.
3. R3: Tuân thủ Clean Architecture (NexusKeys.Domain, NexusKeys.Infrastructure, NexusKeys.Api) và bảo toàn kiểm thử.

CRITICAL RULES:
- Tuyệt đối KHÔNG ĐƯỢC CHỈNH SỬA, XÓA, COMMENT HOẶC LÀM SUY YẾU BẤT KỲ TEST CASE NÀO trong tests/ hoặc bất kỳ file test nào.
- dotnet build GameStore.slnx biên dịch thành công với 0 errors.
- Toàn bộ bài kiểm thử trong tests/GameStore.API.ProductKeyTests/ và AuthAndUserApiTests chạy thành công 100%.
- Maintain progress.md and BRIEFING.md continuously in your working directory.
- Report back when the project is completed so that verification and victory audit can be conducted.
