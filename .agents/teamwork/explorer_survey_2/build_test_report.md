# Báo Cáo Điều Tra Build và Thực Thi Test Suite - NexusKeys GameStore

**Mã điều tra:** `explorer_survey_2`  
**Thời gian thực hiện:** 2026-10-16  
**Môi trường thực thi:** Windows, .NET SDK 10.0.10, xUnit v2.9.2  
**Dự án mục tiêu:** `GameStore.slnx`  

---

## 1. Tóm Tắt Tổng Quan (Executive Summary)

- **Tình trạng biên dịch (Build Status):** **THÀNH CÔNG (0 Errors, 0 Warnings)** trên toàn bộ solution `GameStore.slnx` và từng project thành phần (`NexusKeys.Domain`, `NexusKeys.Infrastructure`, `NexusKeys.Admin`, `NexusKeys.Api`, `NexusKeys.Storefront`, `GameStore.API.ProductKeyTests`).
- **Tình trạng kiểm thử (Test Status):** **72/72 PASS (100% Passed, 0 Failed, 0 Skipped)** trên test suite tích hợp `tests/GameStore.API.ProductKeyTests/`.
- **Bộ test Auth & User Identity (`AuthAndUserApiTests` + Auth Regression Tests):** **15/15 PASS (100% Passed)**.
- **Cơ chế lưu trữ kiểm thử (Database Test Infrastructure):** 100% sử dụng **EF Core In-Memory Database** (`Microsoft.EntityFrameworkCore.InMemory` 10.0.9). Mỗi test case khởi tạo một instance database độc lập với GUID riêng (`api-tests-{Guid.NewGuid():N}`). Không phụ thuộc vào SQL Server thật, PostgreSQL thật hay Docker/TestContainers.

---

## 2. Kết Quả Điều Tra Biên Dịch (Build Investigation)

### 2.1. Lệnh thực thi biên dịch toàn bộ Solution
```powershell
dotnet build GameStore.slnx
```

### 2.2. Chi tiết kết quả Console Output
```text
Determining projects to restore...
All projects are up-to-date for restore.
NexusKeys.Domain -> C:\Users\kynhu\Documents\DATN\datn\src\NexusKeys.Domain\bin\Debug\net10.0\NexusKeys.Domain.dll
NexusKeys.Infrastructure -> C:\Users\kynhu\Documents\DATN\datn\src\NexusKeys.Infrastructure\bin\Debug\net10.0\NexusKeys.Infrastructure.dll
NexusKeys.Admin -> C:\Users\kynhu\Documents\DATN\datn\src\NexusKeys.Admin\bin\Debug\net10.0\Backend.dll
NexusKeys.Api -> C:\Users\kynhu\Documents\DATN\datn\src\NexusKeys.Api\bin\Debug\net10.0\GameStore.API.dll
GameStore.API.ProductKeyTests -> C:\Users\kynhu\Documents\DATN\datn\tests\GameStore.API.ProductKeyTests\bin\Debug\net10.0\GameStore.API.ProductKeyTests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:04.06
```

### 2.3. Kiểm tra project `NexusKeys.Storefront`
Project `src/NexusKeys.Storefront/NexusKeys.Storefront.csproj` được biên dịch độc lập:
```powershell
dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj
```
Kết quả: `Build succeeded. 0 Warning(s), 0 Error(s)`.

---

## 3. Cấu Trúc và Danh Mục Test Suite (Test Suite Inventory)

Toàn bộ dự án chỉ có duy nhất một test project:
- **Đường dẫn:** `tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj`
- **Framework:** .NET 10.0, xUnit 2.9.2, Microsoft.NET.Test.Sdk 17.12.0
- **Tổng số test cases:** **72 test cases** phân bổ trên 8 test classes.

| Test Class | Số lượng Test Cases | Trạng thái | Mục đích kiểm thử |
|---|---|---|---|
| `AuthAndUserApiTests` | 11 | **11/11 PASS** | Xác thực, đăng ký, đăng nhập, refresh token, reset mật khẩu, đổi mật khẩu, xem hồ sơ cá nhân (`/users/me`), lock tài khoản. |
| `ImprovementRegressionTests` | 11 | **11/11 PASS** | Kiểm tra hồi quy an toàn và nghiệp vụ (bao gồm 4 test về Auth/User: lock token rejection, revoke token khi đổi/reset mật khẩu, chặn quyền Admin công khai). |
| `ProductKeyApiTests` | 13 | **13/13 PASS** | Phân trang, tìm kiếm, CRUD product key, bảo toàn trạng thái sold, mã trạng thái HTTP chuẩn. |
| `CatalogAndCommerceApiTests` | 15 | **15/15 PASS** | Tìm kiếm sản phẩm, tạo sản phẩm (phân quyền Admin/Customer), validate coupon, promotion, payment config, upload media. |
| `CustomerWorkflowApiTests` | 15 | **15/15 PASS** | Wishlist, nạp ví (wallet deposit), support tickets, phân quyền chi tiết đơn hàng, checkout mock gateway, reviews, news. |
| `AdminBridgeTests` | 5 | **5/5 PASS** | Cầu nối xác thực phiên Admin qua Token, kiểm tra cookie HTTP-only và phân quyền Administrator. |
| `PaymentWebhookApiTests` | 1 | **1/1 PASS** | Tính bất biến (idempotency) của Webhook thanh toán lặp lại (MoMo). |
| `RestRouteContractTests` | 1 | **1/1 PASS** | Khế ước tài nguyên REST v1 canonical routes và methods. |
| **TỔNG CỘNG** | **72** | **72/72 PASS** | **100% Vượt qua, 0 Lỗi** |

---

## 4. Chi Tiết Thực Thi Bộ Test Auth & User Identity

Lệnh thực thi riêng bộ test Auth:
```powershell
dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --filter "FullyQualifiedName~AuthAndUserApiTests" --logger "console;verbosity=detailed"
```

### 4.1. Chi tiết 11 Test Cases trong `AuthAndUserApiTests`

1. **`Register_InvalidInput_ReturnsProblemDetails` (Theory - 3 test cases)**:
   - Case 1: `username: "ab"`, `email: "valid@example.com"`, `password: "secret1"` -> Kích thước username < 3 -> **Passed** (HTTP 400 Bad Request, Content-Type: `application/problem+json`, 0 User tạo ra).
   - Case 2: `username: "valid-user"`, `email: "invalid-email"`, `password: "secret1"` -> Email không chứa `@` hoặc `.` -> **Passed** (HTTP 400 Bad Request, Content-Type: `application/problem+json`).
   - Case 3: `username: "valid-user"`, `email: "valid@example.com"`, `password: "123"` -> Password < 6 ký tự -> **Passed** (HTTP 400 Bad Request, Content-Type: `application/problem+json`).
2. **`Register_ValidCustomer_CreatesUserAndReturnsLocation` (Fact)**:
   - Input: Username "new-customer", Email "new@example.com", Password "secret1".
   - Kết quả: **Passed** (HTTP 201 Created, Header Location trỏ tới `/api/v1/users/{id}`, Role gán là "Customer", PasswordHash được mã hoá chuẩn BCrypt).
3. **`Login_ValidCredentials_ReturnsTokensAndPersistsRefreshToken` (Fact)**:
   - Input: Email & Password hợp lệ của user đã seed.
   - Kết quả: **Passed** (HTTP 200 OK, trả về JWT Access Token, Refresh Token, Role="Customer", lưu 1 bản ghi vào bảng `RefreshTokens`).
4. **`Login_LockedUser_ReturnsBadRequest` (Fact)**:
   - Input: User có trạng thái `IsLocked = true`.
   - Kết quả: **Passed** (HTTP 400 Bad Request, Content-Type: `application/problem+json`, không tạo RefreshToken).
5. **`Refresh_ValidToken_RotatesRefreshToken` (Fact)**:
   - Input: Gửi Refresh Token hợp lệ còn hạn.
   - Kết quả: **Passed** (HTTP 200 OK, cấp phát JWT mới và xoá token cũ, ghi nhận token mới vào DB - cơ chế Refresh Token Rotation).
6. **`ResetPassword_ValidToken_ChangesPasswordAndConsumesToken` (Fact)**:
   - Input: Gửi `ResetPasswordRequest` kèm Token còn hiệu lực.
   - Kết quả: **Passed** (HTTP 200 OK, User được cập nhật PasswordHash mới qua BCrypt, ResetToken đánh dấu `IsUsed = true`).
7. **`CurrentUserProfile_UsesAuthenticatedUserId` (Fact)**:
   - Input: Gọi `GET /api/v1/users/me` với user identity claim id = 2.
   - Kết quả: **Passed** (HTTP 200 OK, trả về thông tin cá nhân đúng với userId 2).
8. **`ChangePassword_ValidOldPassword_ReturnsNoContentAndUpdatesHash` (Fact)**:
   - Input: Gọi `POST /api/v1/users/me/password-changes` với mật khẩu cũ chính xác.
   - Kết quả: **Passed** (HTTP 204 NoContent, PasswordHash trong DB được cập nhật).
9. **`AdminPatchUser_SetsExplicitLockState` (Fact)**:
   - Input: Role Administrator gọi `PATCH /api/v1/users/1` với body `{ isLocked: true }`.
   - Kết quả: **Passed** (HTTP 204 NoContent, trường `IsLocked` của user đổi thành true).

### 4.2. Chi tiết 4 Test Cases liên quan Auth trong `ImprovementRegressionTests`

1. **`RefreshToken_LockedUser_IsRejected`**:
   - Khi tài khoản bị lock (`IsLocked = true`), mọi nỗ lực refresh token đều bị từ chối với HTTP 401 Unauthorized -> **Passed**.
2. **`ChangePassword_RevokesExistingRefreshTokens`**:
   - Khi đổi mật khẩu thành công qua `/api/v1/users/me/password-changes`, toàn bộ refresh token cũ của user bị thu hồi (`RefreshTokens.Count == 0`) -> **Passed**.
3. **`ResetPassword_RevokesExistingRefreshTokens`**:
   - Khi đặt lại mật khẩu qua `/api/v1/auth/reset-password`, toàn bộ refresh token cũ của user bị thu hồi (`RefreshTokens.Count == 0`) -> **Passed**.
4. **`Register_PublicRequestCannotCreateAdministrator`**:
   - Người dùng công khai cố tình truyền `SecretCode = "NEXUSADMIN"` khi đăng ký bị từ chối với HTTP 403 Forbidden và không có tài khoản Administrator nào được tạo -> **Passed**.

---

## 5. Phân Tích Cơ Sở Dữ Liệu Trong Kiểm Thử (Database Test Dependencies)

Đoạn mã cấu hình DB trong `tests/GameStore.API.ProductKeyTests/ApiTestHost.cs`:
```csharp
var databaseName = $"api-tests-{Guid.NewGuid():N}";
builder.Services.AddDbContext<GameStoreDbContext>(options =>
    options
        .UseInMemoryDatabase(databaseName)
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
```

### Đặc tính môi trường DB kiểm thử:
1. **Loại DB:** Entity Framework Core InMemory Database (`Microsoft.EntityFrameworkCore.InMemory` phiên bản `10.0.9`).
2. **Tính cô lập (Isolation):** Mỗi test case gọi `ApiTestHost.StartAsync()` sẽ tạo ra một database name ngẫu nhiên độc lập (`api-tests-{Guid}`). Nhờ đó, các test case chạy song song hoàn toàn độc lập, không bị xung đột dữ liệu (data race/cross-test contamination).
3. **Không cần hạ tầng ngoài:** Không đòi hỏi PostgreSQL, SQL Server hay SQLite local; không yêu cầu Docker hay TestContainers.
4. **So sánh với Production:**
   - Production (`src/NexusKeys.Api/Program.cs`): Sử dụng PostgreSQL qua `options.UseNpgsql(...)` với retry và split query.
   - Test Host (`tests/GameStore.API.ProductKeyTests/ApiTestHost.cs`): Ghi đè service registration của `GameStoreDbContext` sang InMemory Database.

---

## 6. Phân Tích Lỗi Kiểm Thử (Failing Tests Analysis)

- **Số test bị fail:** **0**
- **Số test bị skip:** **0**
- **Lỗi biên dịch:** **0**
- **Độ lệch assertion (Assertion Discrepancy):** **Không có**.

Toàn bộ 72 test cases trong test suite đều đạt 100% điều kiện Assertion đặt ra trong mã test.

---

## 7. Đánh Giá Khế Ước và Tiêu Chí Chấp Nhận (Acceptance Criteria Verification)

| Tiêu chí | Kỳ vọng | Thực tế quan sát | Kết luận |
|---|---|---|---|
| `dotnet build GameStore.slnx` | 0 Error | 0 Warning, 0 Error, Build Succeeded | **ĐẠT** |
| Bảo toàn thư mục `tests/` | Không sửa đổi bất kỳ file test nào | Thư mục `tests/` hoàn toàn nguyên vẹn | **ĐẠT** |
| Toàn bộ test trong `GameStore.API.ProductKeyTests` | Kết thúc mã thoát 0 | 72/72 Passed, Exit code 0 | **ĐẠT** |
| Bộ test `AuthAndUserApiTests` | 100% Pass | 11/11 Passed (kèm 4/4 test hồi quy auth pass) | **ĐẠT** |
| Chuẩn RFC 7807 (`application/problem+json`) | Trả về `ProblemDetails` khi có lỗi | Đã assert trực tiếp trong `Register_InvalidInput` và `Login_LockedUser` | **ĐẠT** |
| HTTP Status Codes chuẩn | 200, 201, 204, 400, 401, 403 | Các endpoint trả về đúng mã chuẩn REST | **ĐẠT** |
