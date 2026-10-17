# Báo Cáo Phân Tích An Toàn Kiểm Thử & Rủi Ro Hồi Quy (Test Safety & Regression Risk Report)

**Dự án:** NexusKeys Game Store - Authentication & User Identity Standardization  
**Mã điều tra:** `explorer_m1_3`  
**Ngày thực hiện:** 2026-10-16  
**Môi trường:** .NET SDK 10.0.10, xUnit v2.9.2, C# 14  
**Đường dẫn tài liệu:** `c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\explorer_m1_3\test_safety_report.md`

---

## 1. Tóm Tắt Tổng Quan (Executive Summary)

1. **Hiện trạng Test Suite**: Toàn bộ **72/72 test cases** trong dự án `tests/GameStore.API.ProductKeyTests/` đang chạy thành công 100% (**Passed: 72, Failed: 0, Skipped: 0, Duration: ~16s**).
2. **Quy tắc bất biến cốt lõi**:
   - Tuyệt đối **KHÔNG ĐƯỢC CHỈNH SỬA, XÓA, COMMENT HOẶC LÀM SUY YẾU BẤT KỲ TEST CASE NÀO** trong thư mục `tests/`.
   - Mọi file trong `tests/` (bao gồm cả `ApiTestHost.cs`, `ProductKeyApiTests.cs`, v.v.) là **READ-ONLY**.
3. **Phát hiện rủi ro nghiêm trọng số 1 (Breaking Change Hazard)**:
   - File cấu hình test host `ApiTestHost.cs` và `RestRouteContractTests.cs` **KHÔNG** sử dụng `Program.cs`, mà tự khởi tạo `WebApplication.CreateBuilder` độc lập và tự đăng ký các service (DI container riêng).
   - `ApiTestHost.cs` **KHÔNG** đăng ký `ITokenService` hay `TokenService`.
   - Nếu Minimal API endpoints tại `AuthEndpoints.cs` yêu cầu `ITokenService` dưới dạng tham số bắt buộc (non-nullable / non-optional), runtime sẽ quăng ngoại lệ `System.InvalidOperationException: No service for type 'ITokenService' has been registered`, dẫn tới **toàn bộ các integration test của Auth bị lỗi HTTP 500**.
   - **Giải pháp an toàn tuyệt đối**: Endpoints phải resolve service linh hoạt qua `IServiceProvider sp` (hoặc `[FromServices] ITokenService? tokenService = null`) với fallback: `sp.GetService<ITokenService>() ?? new TokenService(db, config);`.
4. **Phát hiện rủi ro biên dịch số 2 (Build Failure Hazard)**:
   - Project `src/NexusKeys.Infrastructure/NexusKeys.Infrastructure.csproj` hiện chưa có package reference tới `Microsoft.AspNetCore.Authentication.JwtBearer` hoặc `System.IdentityModel.Tokens.Jwt`.
   - Khi tách `TokenService.cs` sang `NexusKeys.Infrastructure`, nếu không bổ sung package reference này thì solution sẽ bị **lỗi biên dịch CS0246 (0 errors bị vi phạm)**.
5. **Phát hiện rủi ro logic DB số 3 (Database Assertions)**:
   - Các test `ChangePassword_RevokesExistingRefreshTokens` và `ResetPassword_RevokesExistingRefreshTokens` assert trực tiếp:
     `Assert.Equal(0, await api.WithDbAsync(db => db.RefreshTokens.CountAsync()));`
   - Test `Refresh_ValidToken_RotatesRefreshToken` assert:
     `Assert.DoesNotContain("old-refresh-token", tokens);`
   - Điều này bắt buộc cơ chế thu hồi refresh token (Revocation/Rotation) phải **xóa cứng bản ghi khỏi bảng `db.RefreshTokens`** (`db.RefreshTokens.Remove(rt)` / `RemoveRange`), tuyệt đối không được dùng soft-delete (đánh dấu cờ) vì sẽ làm fail test assert count = 0.

---

## 2. Danh Mục Toàn Bộ 72 Test Cases (Test Suite Inventory)

Test suite nằm trọn vẹn trong `tests/GameStore.API.ProductKeyTests/` gồm 8 file test class:

| File Test Class | Số lượng Test | Loại Test | Mục đích & Phạm vi kiểm thử |
|---|---|---|---|
| `AuthAndUserApiTests.cs` | **11** | Integration | Đăng ký (validation, RFC 7807, tạo User), Đăng nhập (thành công, tài khoản bị khóa), Refresh token rotation, Reset password (tiêu thụ token), Profile (`/users/me`), Đổi mật khẩu, Khóa tài khoản qua PATCH. |
| `ImprovementRegressionTests.cs` | **11** | Integration / Regression | Từ chối refresh token khi user bị khóa, Thu hồi refresh token khi đổi/reset mật khẩu, Chặn cấp quyền Administrator công khai (`NEXUSADMIN`), Validation giá sản phẩm âm, Validation coupon, Khóa key không hợp lệ, Chặn trạng thái order rác, Moderation review comment, Concurrency checkout key. |
| `ProductKeyApiTests.cs` | **13** | Integration | Phân quyền Admin quản lý key, Phân trang gom nhóm theo Product, Tìm kiếm trim ký tự, Gán orderId làm Sold key, Khôi phục/Xóa key khả dụng, Route v1 canonical, Format RFC 7807, Header cảnh báo Deprecation/Sunset. |
| `CatalogAndCommerceApiTests.cs` | **15** | Integration | Tìm kiếm/Sắp xếp sản phẩm, Phân quyền tạo sản phẩm Admin, Upload media file, Chặn xóa sản phẩm đã bán, CRUD coupon, Validate coupon (hết hạn, hết lượt, không kích hoạt), Idempotent promotion, Cấu hình payment gateway. |
| `CustomerWorkflowApiTests.cs` | **15** | Integration | Idempotency Wishlist, Support ticket theo owner và Admin trả lời, Nạp tiền ví (Wallet deposit) kèm validation khoảng giá trị, Phân quyền xem đơn hàng cá nhân, Checkout MockGateway, Đánh giá sản phẩm (yêu cầu đã mua hàng, chống duplicate), Tin tức (News). |
| `AdminBridgeTests.cs` | **6** | Unit / Controller | Cầu nối xác thực Token sang Admin Portal, Redirect Login khi thiếu/hết hạn/sai quyền, Kiểm tra cookie HttpOnly và SameSite=Lax, Cơ chế TokenHandler giữ nguyên Header Authorization. |
| `PaymentWebhookApiTests.cs` | **1** | Integration | Tính bất biến (Idempotency) khi nhận callback webhook MoMo lặp lại. |
| `RestRouteContractTests.cs` | **1** | Architecture / Contract | Kiểm tra khế ước 24 route REST v1 canonical, kiểm tra không xuất hiện các route cũ bị cấm (`toggle-lock`, `vnpay-return`, `auth/change-password`), xác thực Unique Index trên `Transaction`. |
| **TỔNG CỘNG** | **72** | | **100% Pass hiện tại** |

---

## 3. Ma Trận Phân Tích Rủi Ro Hồi Quy Chi Tiết (Detailed Regression Risk Matrix)

### Nhóm 1: Rủi Ro Cơ Cấu Hạ Tầng Kiểm Thử & Dependency Injection (DI)

#### R1.1: Thiếu Service Registration trong `ApiTestHost.cs` và `RestRouteContractTests.cs`
- **Vị trí quan sát**:
  - `tests/GameStore.API.ProductKeyTests/ApiTestHost.cs`: Dòng 50-67.
  - `tests/GameStore.API.ProductKeyTests/RestRouteContractTests.cs`: Dòng 30-46.
- **Hiện trạng mã nguồn**:
  ```csharp
  // ApiTestHost.cs
  builder.Services.AddDbContext<GameStoreDbContext>(...);
  builder.Services.AddHttpClient();
  builder.Services.AddScoped<VnPayService>();
  builder.Services.AddScoped<MoMoService>();
  builder.Services.AddSingleton<IEmailService, FakeEmailService>();
  builder.Services.AddScoped<V1ProblemDetailsFilter>();
  builder.Services.AddProblemDetails();
  builder.Services.AddSignalR();
  builder.Services.AddAuthentication(TestAuthenticationHandler.SchemeName)...;
  builder.Services.AddAuthorization();
  ```
- **Rủi ro hồi quy**:
  - Test host **không gọi `Program.cs`**. Do đó, mọi service chỉ đăng ký trong `Program.cs` (như `builder.Services.AddScoped<ITokenService, TokenService>()`) sẽ **hoàn toàn vắng mặt** trong container của test host.
  - Theo quy tắc bất biến, Worker **không được phép chạm vào `tests/GameStore.API.ProductKeyTests/ApiTestHost.cs`** hay bất kỳ file test nào.
  - Nếu Worker tiêm trực tiếp `ITokenService tokenService` vào Minimal API delegate:
    ```csharp
    // CÁCH NÀY SẼ GÂY CRASH TEST:
    group.MapPost("/login", async (LoginRequest req, GameStoreDbContext db, ITokenService tokenService) => ...);
    ```
    Runtime Minimal API sẽ kích hoạt DI resolution thất bại, trả về HTTP 500 cho tất cả integration test của Auth!
- **Khuyến nghị kỹ thuật bắt buộc cho Worker**:
  - Thiết kế endpoint cho phép fallback linh hoạt khi chạy dưới test host:
    ```csharp
    group.MapPost("/login", async (LoginRequest req, GameStoreDbContext db, IConfiguration config, IServiceProvider sp) =>
    {
        var tokenService = sp.GetService<ITokenService>() ?? new TokenService(db, config);
        // Tiến hành xác thực qua tokenService an toàn 100%
    });
    ```
  - Cách làm này đảm bảo:
    - Khi chạy thực tế (Production): Service lấy từ scoped DI container được cấu hình tại `Program.cs`.
    - Khi chạy kiểm thử (Integration Test): Tự động fallback sang instance `TokenService(db, config)` tương ứng với In-Memory DB của test case đó mà không cần sửa bất kỳ dòng mã nào trong thư mục `tests/`.

---

#### R1.2: Lỗi biên dịch do thiếu Package Reference trong `NexusKeys.Infrastructure`
- **Vị trí quan sát**: `src/NexusKeys.Infrastructure/NexusKeys.Infrastructure.csproj`.
- **Hiện trạng**: Project chỉ có reference tới `BCrypt.Net-Next`, `Google.Apis.Auth`, EF Core, PostgreSQL. Hoàn toàn **chưa có** `Microsoft.AspNetCore.Authentication.JwtBearer` hoặc `System.IdentityModel.Tokens.Jwt`.
- **Rủi ro hồi quy**:
  - Khi triển khai `TokenService.cs` trong `src/NexusKeys.Infrastructure/Services/TokenService.cs`, các kiểu dữ liệu như `JwtSecurityToken`, `SymmetricSecurityKey`, `SigningCredentials`, `JwtRegisteredClaimNames` sẽ báo lỗi `CS0246: The type or namespace name could not be found`.
  - Biên dịch `dotnet build GameStore.slnx` sẽ thất bại với mã lỗi > 0.
- **Khuyến nghị kỹ thuật**:
  - Bổ sung `<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.9" />` vào `src/NexusKeys.Infrastructure/NexusKeys.Infrastructure.csproj`.

---

#### R1.3: Bảo toàn Khế ước Namespace và DTO (`AuthResponse`, `UserDto`)
- **Vị trí quan sát**:
  - `tests/GameStore.API.ProductKeyTests/AuthAndUserApiTests.cs` (Dòng 3, 67, 116, 167):
    - `using GameStore.API.Endpoints;`
    - `response.Content.ReadFromJsonAsync<AuthResponse>()`
    - `response.Content.ReadFromJsonAsync<UserDto>()`
  - `src/NexusKeys.Admin/Controllers/AuthController.cs` (Dòng 75, 141-154):
    - `AdminProfileDto` kỳ vọng JSON trả về từ `/api/v1/users/me` có các trường: `username`, `email`, `roleName`, `isLocked`.
- **Rủi ro hồi quy**:
  - Nếu Worker di chuyển `AuthResponse` sang namespace mới mà không giữ type forwarding hoặc alias tại `GameStore.API.Endpoints`, test project sẽ không biên dịch được.
  - Nếu Worker đổi tên thuộc tính `RoleName` trong `UserDto` thành `Role`, Admin Bridge Controller sẽ không nhận diện được vai trò của quản trị viên và làm fail bài test `AdminBridgeTests.Bridge_WithAdministratorToken_CreatesHttpOnlySessionAndRedirectsHome`.
- **Khuyến nghị kỹ thuật**:
  - Giữ nguyên định nghĩa `public class AuthResponse` trong namespace `GameStore.API.Endpoints` (hoặc type alias tương thích).
  - Giữ nguyên cấu trúc của `UserDto`:
    ```csharp
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal WalletBalance { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsLocked { get; set; }
    }
    ```

---

### Nhóm 2: Rủi Ro Cơ Chế Thu Hồi và Xoay Vòng Token (Token Lifecycle & Revocation)

#### R2.1: Cơ chế Thu hồi Refresh Token bắt buộc phải XÓA CỨNG (Hard Delete) khỏi DB
- **Vị trí quan sát**:
  - `AuthAndUserApiTests.cs`: Dòng 120-123 (`Refresh_ValidToken_RotatesRefreshToken`).
  - `ImprovementRegressionTests.cs`: Dòng 48 (`ChangePassword_RevokesExistingRefreshTokens`).
  - `ImprovementRegressionTests.cs`: Dòng 75 (`ResetPassword_RevokesExistingRefreshTokens`).
- **Mã kiểm thử thực tế**:
  ```csharp
  // ChangePassword_RevokesExistingRefreshTokens:
  Assert.Equal(0, await api.WithDbAsync(db => db.RefreshTokens.CountAsync()));

  // ResetPassword_RevokesExistingRefreshTokens:
  Assert.Equal(0, await api.WithDbAsync(db => db.RefreshTokens.CountAsync()));

  // Refresh_ValidToken_RotatesRefreshToken:
  var tokens = await api.WithDbAsync(db => db.RefreshTokens.Select(item => item.Token).ToListAsync());
  Assert.DoesNotContain("old-refresh-token", tokens);
  Assert.Contains(body.RefreshToken, tokens);
  ```
- **Rủi ro hồi quy**:
  - Nếu Worker áp dụng thiết kế "Soft Delete" (ví dụ thêm thuộc tính `IsRevoked = true` vào entity `RefreshToken` và giữ lại bản ghi trong bảng), lệnh `db.RefreshTokens.CountAsync()` sẽ trả về > 0, lập tức làm **FAIL cả 3 test case trên**.
- **Khuyến nghị kỹ thuật**:
  - Trong `TokenService.RevokeUserTokensAsync(userId)` và `TokenService.RevokeTokenAsync(token)`:
    Sử dụng `db.RefreshTokens.RemoveRange(...)` hoặc `db.RefreshTokens.Remove(...)` và `SaveChangesAsync()`.
  - Giữ đúng bản chất schema hiện tại của `RefreshToken` (chỉ gồm `Id`, `UserId`, `Token`, `Expires`, `CreatedAt`).

---

#### R2.2: Phân định Mã Lỗi HTTP cho Tài Khoản Bị Khóa (Locked User)
- **Vị trí quan sát**:
  - `AuthAndUserApiTests.cs`: Dòng 89 (`Login_LockedUser_ReturnsBadRequest`).
  - `ImprovementRegressionTests.cs`: Dòng 29 (`RefreshToken_LockedUser_IsRejected`).
- **So sánh 2 trường hợp**:
  1. Khi gọi `POST /api/v1/auth/login` với tài khoản bị khóa (`IsLocked == true`):
     - Test assert: `Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);` (HTTP 400).
     - Test assert: `Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);`
     - Test assert: `Assert.Equal(0, await api.WithDbAsync(db => db.RefreshTokens.CountAsync()));`
  2. Khi gọi `POST /api/v1/auth/refresh` với refresh token của tài khoản bị khóa:
     - Test assert: `Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);` (HTTP 401).
- **Rủi ro hồi quy**:
  - Nếu Worker quy chuẩn hóa "người dùng bị khóa" đều trả về 401 hoặc đều trả về 403, bài test `Login_LockedUser_ReturnsBadRequest` sẽ bị FAIL.
  - Nếu Worker đổi refresh token bị khóa thành trả về 400, bài test `RefreshToken_LockedUser_IsRejected` sẽ bị FAIL.
- **Khuyến nghị kỹ thuật**:
  - Endpoint `/login`: Giữ nguyên mã trả về `Results.Problem(statusCode: 400, title: "Tài khoản bị khoá", ...)` với extension `isLocked: true`.
  - Endpoint `/refresh`: Giữ nguyên mã trả về `Results.Unauthorized()` (401) khi user bị khóa.

---

#### R2.3: Mã HTTP Phản hồi khi Đổi Mật Khẩu vs Đặt Lại Mật Khẩu
- **Vị trí quan sát**:
  - `AuthAndUserApiTests.cs`: Dòng 185 (`ChangePassword_ValidOldPassword_ReturnsNoContentAndUpdatesHash`).
  - `AuthAndUserApiTests.cs`: Dòng 150 (`ResetPassword_ValidToken_ChangesPasswordAndConsumesToken`).
- **So sánh mã HTTP**:
  1. Đổi mật khẩu (`POST /api/v1/users/me/password-changes`):
     - Test assert: `Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);` (HTTP 204).
  2. Đặt lại mật khẩu (`POST /api/v1/auth/reset-password`):
     - Test assert: `Assert.Equal(HttpStatusCode.OK, response.StatusCode);` (HTTP 200).
- **Rủi ro hồi quy**:
  - Nếu Worker vô tình sửa endpoint đổi mật khẩu trả về `Results.Ok(...)` (200), hai bài test `ChangePassword_ValidOldPassword_ReturnsNoContentAndUpdatesHash` và `ChangePassword_RevokesExistingRefreshTokens` sẽ bị FAIL.

---

### Nhóm 3: Rủi Ro Khế Ước Route và Endpoint Conventions

#### R3.1: Các Route Cũ Bị Cấm Tuyệt Đối trong V1
- **Vị trí quan sát**: `tests/GameStore.API.ProductKeyTests/RestRouteContractTests.cs` (Dòng 113-118).
- **Mã kiểm tra tiêu cực (Negative Assertions)**:
  ```csharp
  Assert.DoesNotContain(routes, route => route.Route.StartsWith("/api/v1", StringComparison.Ordinal)
      && route.Route.Contains("toggle-lock", StringComparison.Ordinal));
  Assert.DoesNotContain(routes, route => route.Route.StartsWith("/api/v1", StringComparison.Ordinal)
      && route.Route.Contains("vnpay-return", StringComparison.Ordinal));
  Assert.DoesNotContain(routes, route => route.Route == "/api/v1/auth/change-password");
  ```
- **Rủi ro hồi quy**:
  - Route đổi mật khẩu chuẩn v1 là `/api/v1/users/me/password-changes`.
  - Nếu Worker đăng ký route alias `/change-password` trong v1 của `AuthEndpoints`, `RestRouteContractTests` sẽ bị FAIL ngay lập tức!
- **Khuyến nghị kỹ thuật**:
  - Luôn kiểm tra cờ v1:
    ```csharp
    if (!EndpointRouteConventions.IsV1(apiPrefix))
    {
        group.MapPost("/change-password", ...);
    }
    ```

---

#### R3.2: Chặn Đặc Quyền Tạo Admin qua Đăng Ký Công Khai (`NEXUSADMIN`)
- **Vị trí quan sát**: `tests/GameStore.API.ProductKeyTests/ImprovementRegressionTests.cs` (Dòng 79-97).
- **Mã kiểm thử**:
  ```csharp
  using var response = await api.CreateClient().PostAsJsonAsync(
      "/api/v1/auth/register",
      new
      {
          username = "privilege-escalation",
          email = "admin-attempt@example.com",
          password = "password1",
          secretCode = "NEXUSADMIN"
      });

  Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  Assert.False(await api.WithDbAsync(db =>
      db.Users.Include(item => item.Role).AnyAsync(item => item.Role.RoleName == "Administrator")));
  ```
- **Rủi ro hồi quy**:
  - Nếu Worker bỏ qua logic kiểm tra `req.SecretCode == "NEXUSADMIN"` hoặc đổi sang trả về `BadRequest` (400), test này sẽ FAIL.
- **Khuyến nghị kỹ thuật**:
  - Duy trì chính xác:
    ```csharp
    if (req.SecretCode == "NEXUSADMIN")
    {
        return Results.Forbid();
    }
    ```

---

#### R3.3: Endpoint Mới `POST /api/v1/auth/logout`
- **Yêu cầu (Feature 5)**: Thêm endpoint đăng xuất để thu hồi refresh token từ server-side.
- **Tác động tới Test Suite**:
  - `RestRouteContractTests` kiểm tra bằng `Assert.Contains` các route cụ thể và không assert số lượng tổng route. Do đó, việc thêm route `POST /api/v1/auth/logout` là **hoàn toàn an toàn**, không làm ảnh hưởng đến route contract hiện tại.
- **Lưu ý thiết kế**:
  - Request DTO:
    ```csharp
    public class LogoutRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
    ```
  - Route mapping: Đặt trong `AuthEndpoints.cs` dưới `group.MapPost("/logout", ...)`. Khi ánh xạ vào `/api/v1`, URL sẽ là `/api/v1/auth/logout`.
  - Hành vi: Nếu có `RefreshToken`, gọi `tokenService.RevokeTokenAsync(req.RefreshToken)`. Cho phép gọi khi chưa đăng nhập (anonymous) để người dùng có access token vừa hết hạn vẫn có thể hủy refresh token. Trả về `200 OK` (`Results.Ok(new { message = "Đăng xuất thành công!" })`) hoặc `204 NoContent`.

---

### Nhóm 4: Rủi Ro Claims & JWT Bearer Token

#### R4.1: Tiêu Chuẩn Hóa Claims và Tương Thích Ngược
- **Yêu cầu (R2 & Feature 3)**: JWT Access Token phải chứa: `sub`, `ClaimTypes.NameIdentifier` (`nameid`), `email`, `role`, `jti`, `name`. Hạn token: 30 phút.
- **Cách hệ thống đọc Claim hiện tại**:
  ```csharp
  // OrderEndpoints.cs ClaimsPrincipalExtensions:
  public static int? GetUserId(this ClaimsPrincipal user)
  {
      var idStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                  user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
      if (int.TryParse(idStr, out int id)) return id;
      return null;
  }
  ```
- **Phân tích tương thích**:
  - Khi `TokenService.GenerateAccessToken` phát hành cả hai claim `sub` và `ClaimTypes.NameIdentifier` với cùng giá trị `user.Id.ToString()`, `GetUserId` sẽ đọc thành công 100% trong mọi trường hợp.
  - `ClaimTypes.Role` phải được gán giá trị `user.Role?.RoleName ?? "Customer"` để tương thích với `RequireRole("Administrator")`.
  - Khuyến nghị danh sách claims đầy đủ:
    ```csharp
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Customer"),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };
    ```

---

## 4. Danh Sách Lệnh Biên Dịch & Xác Minh Cho Worker (Exact Verification Commands)

Để đảm bảo kết quả nghiệm thu đạt tiêu chí **0 lỗi biên dịch** và **100% test pass (72/72)**, Worker bắt buộc phải thực hiện tuần tự các lệnh sau:

### Bước 1: Biên dịch toàn bộ Solution (GameStore.slnx)
```powershell
dotnet build GameStore.slnx -c Debug
```
*Điều kiện thành công*: `Build succeeded. 0 Warning(s), 0 Error(s)`.

### Bước 2: Biên dịch độc lập Storefront
```powershell
dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj -c Debug
```
*Điều kiện thành công*: `Build succeeded. 0 Warning(s), 0 Error(s)`.

### Bước 3: Chạy toàn bộ Test Suite với Logger chi tiết
```powershell
dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --logger "console;verbosity=normal"
```
*Điều kiện thành công*:
- Mã thoát (Exit code) = `0`.
- Kết quả console: `Passed! - Failed: 0, Passed: 72, Skipped: 0, Total: 72`.

### Bước 4: Chạy riêng bộ kiểm thử Xác thực & Định danh (Auth Focus)
```powershell
dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --filter "FullyQualifiedName~AuthAndUserApiTests" --logger "console;verbosity=detailed"
```
*Điều kiện thành công*: `Passed: 11, Failed: 0, Total: 11`.

### Bước 5: Chạy riêng bộ kiểm thử Hồi quy Nghiệp vụ (Regression Focus)
```powershell
dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --filter "FullyQualifiedName~ImprovementRegressionTests" --logger "console;verbosity=detailed"
```
*Điều kiện thành công*: `Passed: 11, Failed: 0, Total: 11`.

### Bước 6: Kiểm tra khế ước Route v1
```powershell
dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --filter "FullyQualifiedName~RestRouteContractTests" --logger "console;verbosity=detailed"
```
*Điều kiện thành công*: `Passed: 1, Failed: 0, Total: 1`.

### Bước 7: Kiểm tra tính toàn vẹn của thư mục tests (Git Diff Check)
```powershell
git status -- tests/
git diff --stat tests/
```
*Điều kiện thành công*: Tuyệt đối không có file nào bị thay đổi trong thư mục `tests/` (output rỗng).

---

## 5. Kết Luận & Hướng Dẫn Hành Động (Actionable Checklist)

| Mục kiểm tra | Hướng dẫn cho Worker | Mức độ rủi ro |
|---|---|---|
| **Tách Service Clean Architecture** | Đặt interface `ITokenService` trong `src/NexusKeys.Domain/Interfaces/ITokenService.cs` và implementation `TokenService` trong `src/NexusKeys.Infrastructure/Services/TokenService.cs`. | Cao |
| **Package Reference** | Bổ sung `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.9 vào `src/NexusKeys.Infrastructure/NexusKeys.Infrastructure.csproj`. | Rất cao |
| **Tiêm Service vào Endpoint** | Dùng `IServiceProvider sp` và fallback `sp.GetService<ITokenService>() ?? new TokenService(db, config);` để không gây lỗi khi chạy qua `ApiTestHost`. | **TỐI KHẨN CẤP** |
| **Xóa Refresh Token** | Sử dụng `db.RefreshTokens.Remove(...)` / `RemoveRange(...)` để đảm bảo `CountAsync() == 0` khi thu hồi. | **TỐI KHẨN CẤP** |
| **Mã lỗi Login vs Refresh** | `/login` với user bị khóa trả về `400 BadRequest` (kèm `isLocked: true`). `/refresh` với user bị khóa trả về `401 Unauthorized`. | Rất cao |
| **Mã lỗi Đổi vs Reset mật khẩu** | `/users/me/password-changes` trả về `204 NoContent`. `/auth/reset-password` trả về `200 OK`. | Cao |
| **Bảo toàn DTOs** | Giữ nguyên thuộc tính `RoleName` trong `UserDto` và namespace `GameStore.API.Endpoints` cho `AuthResponse`. | Cao |
| **Bảo toàn Thư mục Tests** | Tuyệt đối không chỉnh sửa bất kỳ file nào trong thư mục `tests/`. | **TUYỆT ĐỐI** |
