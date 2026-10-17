# Báo Cáo Khảo Sát Kiến Trúc Hệ Thống (Solution Architecture Survey Report)

**Dự án**: NexusKeys Game Store (`GameStore.slnx`)  
**Ngày thực hiện**: 2026-10-16  
**Agent thực hiện**: `explorer_survey_1`  
**Mục tiêu**: Khảo sát toàn diện kiến trúc giải pháp Clean Architecture, mô hình dữ liệu Identity, luồng xác thực và phân quyền, đối chiếu các yêu cầu R1, R2, R3 để làm cơ sở cho kế hoạch triển khai tiếp theo.

---

## 1. Tổng Quan Giải Pháp (Solution Overview)

Tập tin solution `GameStore.slnx` tổ chức dự án theo mô hình Clean Architecture đa dự án trên nền tảng .NET 10 (`net10.0`):

| Dự án | Thư mục vật lý | Assembly / Root Namespace | Loại dự án | Vai trò trong hệ thống |
|---|---|---|---|---|
| **NexusKeys.Domain** | `src/NexusKeys.Domain` | `NexusKeys.Domain` (Entities dùng `GameStore.API.Entities`) | Class Library (`net10.0`) | Tầng Domain: Chứa toàn bộ các Entity đại diện cho mô hình nghiệp vụ |
| **NexusKeys.Infrastructure** | `src/NexusKeys.Infrastructure` | `NexusKeys.Infrastructure` (Data/Services dùng `GameStore.API.*`) | Class Library (`net10.0`) | Tầng Hạ tầng: EF Core DbContext, Migrations, Seeders, Background Tasks, tích hợp bên thứ ba (Email, AI, Cổng thanh toán MoMo/VnPay) |
| **NexusKeys.Api** | `src/NexusKeys.Api` | `GameStore.API` / `GameStore.API` | ASP.NET Core Web API (`net10.0`) | Tầng API / Presentation: Minimal API endpoints, cấu hình Authentication/Authorization, Middleware, ProblemDetails Filter |
| **NexusKeys.Admin** | `src/NexusKeys.Admin` | `Backend` / `Backend` | ASP.NET Core MVC / Blazor Server | Cổng quản trị nội bộ (Admin Dashboard) xác thực qua cơ chế Bridge Token sang API |
| **NexusKeys.Storefront** | `src/NexusKeys.Storefront` | `Frontend` / `Frontend` | ASP.NET Core Blazor Web | Giao diện cửa hàng game cho khách hàng (Storefront) |
| **GameStore.API.ProductKeyTests** | `tests/GameStore.API.ProductKeyTests` | `GameStore.API.ProductKeyTests` | xUnit Test Project (`net10.0`) | Bộ kiểm thử tích hợp (Integration Tests) tự khởi tạo In-Memory Host |

### Trạng thái Build & Test hiện tại
- **Build**: Lệnh `dotnet build GameStore.slnx` thành công 100% với **0 Error(s)** và **0 Warning(s)**.
- **Test Suite**: Chạy `dotnet test` vượt qua toàn bộ **72/72 tests** (0 failed, 0 skipped, 72 passed).

---

## 2. Chi Tiết Kiến Trúc Từng Phân Hệ (Detailed Architecture)

### 2.1. NexusKeys.Domain
- **Vị trí**: `src/NexusKeys.Domain/Entities/`
- **Các phân nhóm Entity**:
  - `Identity/`: `User.cs`, `Role.cs`, `RefreshToken.cs`, `PasswordResetToken.cs`.
  - `Catalog/`: `Category.cs`, `Product.cs`, `ProductCategory.cs`, `ProductExtensions.cs`, `ProductMedia.cs`, `Release.cs`, `SystemRequirement.cs`.
  - `Commerce/`: `CartItem.cs`, `Order.cs`, `OrderItem.cs`, `Transaction.cs`, `Wishlist.cs`.
  - `Community/`: `Review.cs`, `ReviewComment.cs`.
  - `Config/`: `AiAssistantConfig.cs`, `AuditLog.cs`, `PaymentGatewayConfig.cs`.
  - `Keys/`: `License.cs`, `ProductKey.cs`.
  - `Promotions/`: `Coupon.cs`, `ProductPromotion.cs`, `Promotion.cs`.
  - `Support/`: `NewsArticle.cs`, `Notification.cs`, `SupportTicket.cs`.
- **Mô hình thực thể Identity**:
  - **`User`**:
    - Thuộc tính: `Id` (int, PK), `Username` (string), `Email` (string), `PasswordHash` (string), `WalletBalance` (decimal), `RoleId` (int, FK), `CreatedAt` (DateTime), `GoogleId` (string?), `AvatarUrl` (string?), `IsLocked` (bool).
    - Quan hệ: `Role` (1-n), `RefreshTokens` (1-n), `PasswordResetTokens` (1-n), `Orders`, `Wishlists`, v.v.
  - **`Role`**:
    - Thuộc tính: `Id` (int, PK), `RoleName` (string).
    - Các vai trò tiêu chuẩn hệ thống: `"Customer"`, `"Administrator"`, `"Staff"`, `"Developer"`.
  - **`RefreshToken`**:
    - Thuộc tính: `Id` (int, PK), `UserId` (int, FK), `Token` (string), `Expires` (DateTime), `CreatedAt` (DateTime), navigation property `User`.
    - Ghi nhận: Thực thể này hiện không có các cờ đánh dấu thu hồi mềm như `IsRevoked`, `RevokedAt`, hay `ReplacedByToken`. Khi refresh hoặc đổi mật khẩu, token cũ đang được xử lý bằng cơ chế hard-delete khỏi cơ sở dữ liệu (`Remove` / `RemoveRange`).
  - **`PasswordResetToken`**:
    - Thuộc tính: `Id` (int, PK), `UserId` (int, FK), `Token` (string), `CreatedAt` (DateTime), `ExpiresAt` (DateTime), `IsUsed` (bool).
- **Đánh giá tầng Domain**:
  - Tầng Domain hiện tập trung hoàn toàn vào mô hình thực thể (Anemic Domain Model) với Data Annotations (`[Key]`, `[ForeignKey]`, `[Table]`).
  - Chưa định nghĩa các Interface nghiệp vụ (như `IUserRepository`, `ITokenRepository`) hay Domain Services tại tầng này; toàn bộ nghiệp vụ truy vấn và xác thực hiện nằm trực tiếp ở Minimal API lambdas.

### 2.2. NexusKeys.Infrastructure
- **DbContext (`GameStoreDbContext`)**:
  - Kế thừa `Microsoft.EntityFrameworkCore.DbContext`.
  - Khai báo đầy đủ các `DbSet<T>` cho 28 thực thể.
  - Cấu hình toàn cục trong `OnModelCreating`:
    - Precision cho kiểu `decimal`: `SetPrecision(18); SetScale(2)`.
    - Vô hiệu hoá Cascade Delete mặc định: `relationship.DeleteBehavior = DeleteBehavior.Restrict`.
    - Cấu hình Composite Keys cho các bảng nối (`ProductCategory`, `ProductPromotion`, `Wishlist`, `CartItem`).
    - Chỉ mục duy nhất cho giao dịch thanh toán: `HasIndex(t => new { t.PaymentGateway, t.GatewayTransactionId }).IsUnique()`.
- **Dữ liệu khởi tạo & Khởi chạy (`DbInitializer`)**:
  - `EnsureSeedRolesAndUsers`: Đảm bảo 4 roles cơ sở (`Administrator`, `Customer`, `Developer`, `Staff`) và khởi tạo sẵn các tài khoản mẫu với mật khẩu băm BCrypt (`Admin123!`, `Staff123!`, `Dev123!`, `User123!`).
- **Migrations**:
  - Thư mục `Data/Migrations/` chứa 10 migrations lịch sử từ `20260615143953_InitialCreate` đến `20260826000000_AddUniquePaymentTransaction`.
  - Cấu hình provider chính trong `Program.cs` là PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`) kết nối đến Supabase, hỗ trợ tự động `databaseCreator.CreateTables()` khi khởi chạy nếu bảng chưa tồn tại.
- **Dịch vụ hạ tầng (`Services/`)**:
  - `IEmailService` và `EmailService`: Gửi email qua SMTP (hỗ trợ gửi link reset password).
  - `GeminiService`: Tích hợp Google Gemini AI và fallback Ollama.
  - `MoMoService`, `VnPayService`: Tạo URL và xử lý IPN/Webhook thanh toán.
  - `BackgroundTasks`: `OrderCleanupService`, `PromotionCleanupService` (dọn dẹp đơn hàng và khuyến mãi hết hạn).

### 2.3. NexusKeys.Api
- **Cấu hình Authentication & JWT Bearer** (`Program.cs` lines 31-45):
  - Scheme: `JwtBearerDefaults.AuthenticationScheme`.
  - `TokenValidationParameters`:
    - `ValidateIssuer = true` (`builder.Configuration["Jwt:Issuer"]`).
    - `ValidateAudience = true` (`builder.Configuration["Jwt:Audience"]`).
    - `ValidateLifetime = true`.
    - `ValidateIssuerSigningKey = true` (SymmetricSecurityKey dựa trên `Jwt:Key`).
    - `ClockSkew = TimeSpan.Zero` (loại bỏ độ trễ thời gian nhằm kiểm soát chính xác hạn sống token).
- **Hệ thống Routing kép (Dual Routing Architecture)**:
  - Dự án thiết lập hệ thống định tuyến phiên bản V1 (`/api/v1/*`) kết hợp song song các route tương thích ngược kế thừa (`/api/*`):
    - Nhóm `/api/v1` được gắn filter chuẩn hoá: `.AddEndpointFilter<V1ProblemDetailsFilter>()`.
    - Nhóm `/api` cũ được giám sát bởi `LegacyApiDeprecationMiddleware` (tự động đính kèm response headers `Deprecation: true`, `Sunset: Fri, 31 Dec 2027 23:59:59 GMT`, và `Link: </api/v1>; rel="successor-version"`).
- **Cơ chế Xử lý Lỗi & RFC 7807 (ProblemDetails)**:
  - `V1ProblemDetailsFilter` (`src/NexusKeys.Api/Endpoints/Common/EndpointRouteConventions.cs`):
    - Đón bắt mọi IResult trả về từ endpoint trong nhóm V1.
    - Nếu mã trạng thái >= 400 và chưa phải là `ProblemDetails`, tự động trích xuất thông điệp (từ `message` property hoặc text) và đóng gói thành `Results.Problem(statusCode, title, detail)` trả về Content-Type `application/problem+json`.
  - `app.UseStatusCodePages(...)`: Bổ sung ProblemDetails cho các mã trạng thái phát sinh ở tầng middleware (như 401 Unauthorized do framework từ chối khi thiếu Bearer token).
  - `builder.Services.AddProblemDetails(...)`: Tự động bổ sung `traceId` và `instance` vào payload ProblemDetails.
- **Endpoints Định danh (`Endpoints/Identity/`)**:
  - `AuthEndpoints.cs`:
    - `POST /api/v1/auth/register`: Đăng ký tài khoản Customer, băm mật khẩu bằng BCrypt, trả về 201 Created với Header `Location`.
    - `POST /api/v1/auth/login`: Xác thực Email/Password, chặn tài khoản bị khoá (`IsLocked`), sinh JWT và RefreshToken (hạn 7 ngày).
    - `POST /api/v1/auth/refresh`: Thu hồi token cũ, sinh token mới (Rotation), giải quyết xung đột đồng thời (`DbUpdateConcurrencyException`).
    - `POST /api/v1/auth/forgot-password`: Tạo `PasswordResetToken` và gửi email.
    - `POST /api/v1/auth/reset-password`: Xác minh token, cập nhật mật khẩu, thu hồi toàn bộ RefreshToken của người dùng.
    - `POST /api/v1/auth/external-login`: Đăng nhập qua Google (xác thực token hoặc mock).
  - `UserEndpoints.cs`:
    - `GET /api/v1/users/me`: Lấy thông tin tài khoản người dùng đang đăng nhập dựa trên claim định danh.
    - `PATCH /api/v1/users/me`: Cập nhật thông tin profile (`Username`, `AvatarUrl`).
    - `POST /api/v1/users/me/password-changes`: Đổi mật khẩu tài khoản hiện tại, kiểm tra mật khẩu cũ, cập nhật hash mới và thu hồi toàn bộ RefreshToken hiện có (trả về 204 NoContent).
    - `GET /api/v1/users`: Danh sách người dùng phân trang (chỉ Administrator).
    - `PATCH /api/v1/users/{id}`: Quản trị viên cập nhật trạng thái khoá (`isLocked`), thông tin người dùng (trả về 204 NoContent).
    - `PUT /api/v1/users/{id}/role`: Phân quyền người dùng.

---

## 3. Khảo Sát Chi Tiết Luồng Xác Thực Hiện Tại & Đối Chiếu Yêu Cầu

### 3.1. Đối chiếu Yêu cầu R1 — Chuẩn hóa luồng Xác thực (Authentication Flow)

| Luồng nghiệp vụ | Endpoint hiện có | Hiện trạng thực tế | Mức độ đáp ứng | Khoảng cách / Ghi chú (Gaps) |
|---|---|---|---|---|
| **Đăng ký (Register)** | `POST /api/v1/auth/register` | Đã kiểm tra email hợp lệ, độ dài username (>=3), password (>=6), kiểm tra email trùng, chặn mã bí mật `NEXUSADMIN` (trả về 403 Forbidden). Trả về 201 Created kèm header `Location`. | **Đạt 90%** | Chưa kiểm tra trùng lặp `Username`. Chưa thực hiện trim ký tự trắng trên input trước khi lưu. |
| **Đăng nhập (Login)** | `POST /api/v1/auth/login` | Kiểm tra email và mật khẩu (BCrypt). Tài khoản bị khoá trả về HTTP 400 ProblemDetails (phù hợp test case). Trả về JWT Token, RefreshToken, Role. | **Đạt 95%** | Chưa có cơ chế đếm số lần đăng nhập thất bại để tạm thời khoá bảo vệ (rate limit/lockout). |
| **Cấp mới token (Refresh Token)** | `POST /api/v1/auth/refresh` | Nhận `RefreshToken`, kiểm tra hạn dùng, kiểm tra `IsLocked`, xoá token cũ và thêm token mới vào DB (Rotation). Xử lý concurrency race condition. | **Đạt 95%** | Cơ chế rotation đang xoá vật lý (hard-delete) thay vì đánh dấu thu hồi mềm (`IsRevoked`), khiến không lưu lại được lịch sử token bị thay thế. |
| **Đổi mật khẩu (Password Changes)** | `POST /api/v1/users/me/password-changes` | Đòi hỏi Authorization Bearer, xác thực mật khẩu cũ bằng BCrypt, cập nhật hash mật khẩu mới, thu hồi toàn bộ refresh token cũ của user. Trả về 204 NoContent. | **Đạt 100%** | Đúng chuẩn REST API v1. Đã được bảo vệ chặt chẽ trong `AuthAndUserApiTests` và `ImprovementRegressionTests`. |
| **Quên & Đặt lại mật khẩu** | `POST /api/v1/auth/forgot-password`<br>`POST /api/v1/auth/reset-password` | Gửi email liên kết kèm token ngẫu nhiên. Khi reset: kiểm tra token chưa dùng (`!IsUsed`), còn hạn, đổi mật khẩu và thu hồi toàn bộ RefreshToken của user. | **Đạt 100%** | Đã hoàn thiện và có test case kiểm chứng. |
| **Đăng xuất (Logout) / Thu hồi Token** | *Chưa có endpoint API* | Hiện tại Storefront và Admin chỉ xoá cookie/local storage phía client. Không có endpoint gọi lên API để thu hồi refresh token của phiên đó. | **Chưa có (Gap)** | Cần bổ sung endpoint đăng xuất/thu hồi token an toàn (ví dụ `POST /api/v1/auth/logout` hoặc `POST /api/v1/auth/revoke`). |
| **Chuẩn RFC 7807 (`application/problem+json`)** | Toàn bộ nhóm `/api/v1/*` | `V1ProblemDetailsFilter` tự động chuyển đổi phản hồi lỗi (>=400) sang ProblemDetails chuẩn. | **Đạt 100%** | Tất cả bài test kiểm tra Content-Type `application/problem+json` đều đã pass. |

---

### 3.2. Đối chiếu Yêu cầu R2 — Bảo mật & Quản lý Phiên (JWT Bearer & Session Security)

| Tiêu chuẩn bảo mật | Triển khai hiện tại trong `AuthEndpoints.GenerateJwtToken` | Đánh giá & Khoảng cách (Gaps) |
|---|---|---|
| **Claims tiêu chuẩn (`sub` / `nameid`)** | Sử dụng `new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())`. | **Thiếu explicit `nameid` claim**: Mặc dù claim `sub` có giá trị `user.Id`, `ClaimTypes.NameIdentifier` (`nameid`) chưa được khai báo tường minh trong mảng claims. Nên bổ sung `new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())` để tương thích hoàn toàn cả hai chuẩn parser. |
| **Claim `email`** | `new Claim(JwtRegisteredClaimNames.Email, user.Email)` | **Đạt**. Có đầy đủ email người dùng. |
| **Claim `role`** | `new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Customer")` | **Đạt**. Cung cấp đúng vai trò phân quyền. |
| **Claim `jti`** | `new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())` | **Đạt**. Cung cấp định danh duy nhất cho mỗi JWT token. |
| **Claim `name` / username** | `new Claim(ClaimTypes.Name, user.Username)` | **Đạt**. Cung cấp username người dùng. |
| **Thời hạn Access Token** | `DateTime.UtcNow.AddMinutes(30)` | **Đạt**. 30 phút là thời hạn hợp lý, cân bằng giữa bảo mật và hiệu năng. |
| **Thời hạn Refresh Token** | `DateTime.UtcNow.AddDays(7)` | **Đạt**. 7 ngày là thời hạn phiên làm việc dài tiêu chuẩn. |
| **Thu hồi khi đổi mật khẩu** | `db.RefreshTokens.RemoveRange(userTokens)` trong `password-changes` và `reset-password`. | **Đạt**. Đã kiểm tra và có test case hồi quy (`ChangePassword_RevokesExistingRefreshTokens`, `ResetPassword_RevokesExistingRefreshTokens`). |
| **Thu hồi khi đăng xuất** | Chưa có endpoint API để client gọi huỷ RefreshToken khi đăng xuất. | **Gap lớn**: Cần cung cấp API đăng xuất nhận RefreshToken hoặc token của user hiện tại để vô hiệu hoá session trong DB. |

---

### 3.3. Đối chiếu Yêu cầu R3 — Tuân thủ Quy chuẩn Dự án và Bảo toàn Kiểm thử

| Quy chuẩn & Tiêu chí | Hiện trạng | Yêu cầu kiểm soát nghiêm ngặt |
|---|---|---|
| **Quy tắc bất khả xâm phạm: Không sửa Test** | 100% các file trong `tests/` nguyên vẹn, không bị sửa đổi, xoá hay comment. | Mọi điều chỉnh mã nguồn phải phục vụ việc thỏa mãn test case hiện có, không được thay đổi test case dưới bất kỳ hình thức nào. |
| **Tương thích `GameStore.API.ProductKeyTests`** | 72/72 tests pass. | Mọi thay đổi kiến trúc/refactor bắt buộc phải duy trì pass 100% toàn bộ 72 tests. |
| **Quy ước Route V1 (`RestRouteContractTests`)** | Hệ thống route tuân thủ nghiêm ngặt các quy ước REST: <br>- `POST /api/v1/auth/register`<br>- `POST /api/v1/auth/login`<br>- `POST /api/v1/auth/refresh`<br>- `POST /api/v1/users/me/password-changes`<br>- `PATCH /api/v1/users/{id:int}`<br>- CẤM tồn tại route `/api/v1/auth/change-password`<br>- CẤM route `/api/v1` nào chứa chuỗi `toggle-lock`. | Khi mở rộng hoặc chuẩn hoá, tuyệt đối không được làm thay đổi cấu trúc URL contract đã được `RestRouteContractTests` kiểm chứng. |
| **Quy ước Tên Namespace** | Các entity trong `NexusKeys.Domain` đang thuộc `namespace GameStore.API.Entities`. `GameStoreDbContext` thuộc `namespace GameStore.API.Data`. | Do các test suite và dự án liên quan đang import trực tiếp `using GameStore.API.*`, việc tái cấu trúc phải giữ nguyên các namespace này (hoặc cấu hình type alias / global using tương thích) để không gây lỗi biên dịch liên dự án. |
| **Xử lý phản hồi lỗi tài khoản bị khoá** | `Login_LockedUser_ReturnsBadRequest` yêu cầu rõ ràng: Đăng nhập với tài khoản bị khoá phải trả về mã `400 Bad Request` kèm ProblemDetails (chứ không phải 401 hay 403). | Cần lưu ý duy trì HTTP 400 cho trường hợp `user.IsLocked` khi đăng nhập để đảm bảo tính tương thích với bài test. |

---

## 4. Tổng Hợp Các Điểm Thiếu Hụt (Gap Summary Table)

| ID | Vấn đề / Khoảng cách phát hiện | Mức độ ưu tiên | Tác động | Đề xuất giải pháp |
|---|---|---|---|---|
| **GAP-01** | Chưa có API Đăng xuất (`POST /api/v1/auth/logout`) để thu hồi Refresh Token trên máy chủ | **Cao** | Người dùng đăng xuất ở client nhưng refresh token trong DB vẫn còn hiệu lực đến 7 ngày, tiềm ẩn rủi ro chiếm đoạt phiên nếu token bị rò rỉ | Bổ sung endpoint `POST /api/v1/auth/logout` nhận `refreshToken` (hoặc lấy userId từ Bearer claim) để xoá/thu hồi token khỏi DB |
| **GAP-02** | JWT Token sinh ra thiếu tường minh claim `ClaimTypes.NameIdentifier` (`nameid`) | **Trung bình** | Một số client hoặc library phân tích JWT chuẩn mong đợi claim `nameid` bên cạnh `sub` | Bổ sung cả `ClaimTypes.NameIdentifier` (`nameid`) vào mảng claims trong hàm sinh token `GenerateJwtToken` |
| **GAP-03** | Thiếu kiểm tra trùng lặp `Username` khi Đăng ký (`register`) | **Trung bình** | Người dùng có thể tạo hai tài khoản trùng tên hiển thị dù khác email | Bổ sung kiểm tra `await db.Users.AnyAsync(u => u.Username == req.Username)` kèm phản hồi 400 ProblemDetails |
| **GAP-04** | Dữ liệu đầu vào đăng ký chưa được trim chuẩn hóa khoảng trắng | **Thấp** | Có thể lưu các username/email chứa khoảng trắng thừa ở đầu/cuối | Thêm `.Trim()` cho Username và Email trong `RegisterRequest` |
| **GAP-05** | Toàn bộ logic xác thực và sinh token nằm trực tiếp trong Minimal API Endpoint | **Trung bình** | Giảm tính đóng gói của Clean Architecture, khó tái sử dụng giữa các phân hệ hoặc viết unit test độc lập | Có thể trích xuất dịch vụ quản lý token / identity (`ITokenService` hoặc `IIdentityService`) trong Infrastructure/Domain nhưng phải đảm bảo không làm gãy các contract và test hiện hữu |

---

## 5. Kết Luận & Khuyến Nghị Cho Các Bước Kế Tiếp

1. **Về tính toàn vẹn hệ thống**:
   - Hiện trạng mã nguồn và test suite đang hoạt động rất tốt (toàn bộ 72 tests pass).
   - Kiến trúc Minimal API kết hợp `V1ProblemDetailsFilter` đã giải quyết triệt để yêu cầu RFC 7807 trên phiên bản V1.
2. **Kế hoạch củng cố (Hardening Plan)**:
   - Bổ sung endpoint `POST /api/v1/auth/logout` để hoàn thiện chu trình sống của phiên xác thực (đăng ký -> đăng nhập -> cấp mới token -> đổi mật khẩu -> đăng xuất).
   - Đảm bảo token phát hành có đủ cả `sub`, `ClaimTypes.NameIdentifier` (`nameid`), `email`, `role`, và `jti`.
   - Bổ sung validation kiểm tra trùng `Username` và chuẩn hoá dữ liệu đầu vào.
   - Luôn chạy `dotnet test` sau mọi tinh chỉnh để đảm bảo bảo toàn 100% kết quả kiểm thử.
