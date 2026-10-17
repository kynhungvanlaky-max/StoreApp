# Handoff Report — explorer_m1_3 (Test Safety & Regression Analysis)

**Role:** Explorer / Test Safety & Regression Investigator  
**Target:** Milestone 1 & 2 Implementation Safety  
**Working Directory:** `c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\explorer_m1_3`  
**Report Artifact:** `c:\Users\kynhu\Documents\DATN\datn\.agents\teamwork\explorer_m1_3\test_safety_report.md`  

---

## 1. Observation

1. **Test Suite Status & Execution**:
   - Command: `dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj`
   - Result: `Passed! - Failed: 0, Passed: 72, Skipped: 0, Total: 72, Duration: 16 s - GameStore.API.ProductKeyTests.dll (net10.0)`.
   - Tất cả 72 bài kiểm thử (gồm 8 test class) đều vượt qua 100%.

2. **Test Infrastructure Independence (`ApiTestHost.cs` & `RestRouteContractTests.cs`)**:
   - `tests/GameStore.API.ProductKeyTests/ApiTestHost.cs` (dòng 35-67): Tạo host bằng `WebApplication.CreateBuilder` độc lập, đăng ký:
     `GameStoreDbContext` (InMemory), `HttpClient`, `VnPayService`, `MoMoService`, `FakeEmailService`, `V1ProblemDetailsFilter`, `ProblemDetails`, `SignalR`, `Authentication`, `Authorization`.
   - `ApiTestHost.cs` và `RestRouteContractTests.cs` (dòng 30-46) **hoàn toàn không đăng ký `ITokenService` hay `TokenService`**.
   - `ApiTestHost.cs` không gọi `Program.cs`. Mọi service đăng ký ở `Program.cs` không tồn tại trong container của test host.

3. **Missing Package Reference in Infrastructure**:
   - File `src/NexusKeys.Infrastructure/NexusKeys.Infrastructure.csproj` (dòng 23-37) không có `Microsoft.AspNetCore.Authentication.JwtBearer` hay `System.IdentityModel.Tokens.Jwt`.
   - File `src/NexusKeys.Api/NexusKeys.Api.csproj` (dòng 22) có `<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.9" />`.

4. **Database Assertions on Token Revocation**:
   - `tests/GameStore.API.ProductKeyTests/ImprovementRegressionTests.cs`:
     - Dòng 48: `Assert.Equal(0, await api.WithDbAsync(db => db.RefreshTokens.CountAsync()));`
     - Dòng 75: `Assert.Equal(0, await api.WithDbAsync(db => db.RefreshTokens.CountAsync()));`
   - `tests/GameStore.API.ProductKeyTests/AuthAndUserApiTests.cs`:
     - Dòng 122: `var tokens = await api.WithDbAsync(db => db.RefreshTokens.Select(item => item.Token).ToListAsync());`
     - Dòng 123: `Assert.DoesNotContain("old-refresh-token", tokens);`

5. **Locked User Response Code Divergence**:
   - `tests/GameStore.API.ProductKeyTests/AuthAndUserApiTests.cs` dòng 89:
     `Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);` (`Login_LockedUser_ReturnsBadRequest`).
   - `tests/GameStore.API.ProductKeyTests/ImprovementRegressionTests.cs` dòng 29:
     `Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);` (`RefreshToken_LockedUser_IsRejected`).

6. **Password Change vs Password Reset Response Codes**:
   - `tests/GameStore.API.ProductKeyTests/AuthAndUserApiTests.cs` dòng 185:
     `Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);` (`/api/v1/users/me/password-changes`).
   - `tests/GameStore.API.ProductKeyTests/AuthAndUserApiTests.cs` dòng 150:
     `Assert.Equal(HttpStatusCode.OK, response.StatusCode);` (`/api/v1/auth/reset-password`).

7. **Banned Legacy Routes in V1**:
   - `tests/GameStore.API.ProductKeyTests/RestRouteContractTests.cs` dòng 113-118:
     - `Assert.DoesNotContain(routes, route => route.Route.StartsWith("/api/v1") && route.Route.Contains("toggle-lock"));`
     - `Assert.DoesNotContain(routes, route => route.Route.StartsWith("/api/v1") && route.Route.Contains("vnpay-return"));`
     - `Assert.DoesNotContain(routes, route => route.Route == "/api/v1/auth/change-password");`

8. **Admin Bridge DTO Dependencies**:
   - `src/NexusKeys.Admin/Controllers/AuthController.cs` dòng 141-154: `AdminProfileDto` mong đợi các thuộc tính `username`, `email`, `roleName`, `isLocked` từ response `GET /api/v1/users/me`.

---

## 2. Logic Chain

1. *Từ Observation 2*: Vì `ApiTestHost.cs` không đăng ký `ITokenService` và quy tắc của dự án cấm chỉnh sửa bất kỳ file nào trong `tests/`, nếu Worker tiêm `ITokenService` như một tham số bắt buộc trong các endpoint delegate của `AuthEndpoints`, runtime của Minimal API sẽ ném ngoại lệ `System.InvalidOperationException: No service for type 'ITokenService' has been registered` khi test chạy.
   -> *Hệ quả*: Tất cả các test case gọi vào `/api/v1/auth/*` sẽ bị lỗi 500.
   -> *Giải pháp*: Worker bắt buộc phải sử dụng `IServiceProvider sp` (hoặc `[FromServices] ITokenService? tokenService = null`) kết hợp fallback `sp.GetService<ITokenService>() ?? new TokenService(db, config);`.

2. *Từ Observation 3*: Khi trích xuất `TokenService.cs` sang `NexusKeys.Infrastructure`, code sẽ sử dụng `JwtSecurityToken` và `SymmetricSecurityKey`. Nếu không thêm package `Microsoft.AspNetCore.Authentication.JwtBearer` vào `NexusKeys.Infrastructure.csproj`, `dotnet build GameStore.slnx` sẽ sinh lỗi biên dịch CS0246.
   -> *Giải pháp*: Thêm package `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.9 vào `NexusKeys.Infrastructure.csproj`.

3. *Từ Observation 4*: Cả `ImprovementRegressionTests` và `AuthAndUserApiTests` đều kiểm tra số lượng bản ghi `db.RefreshTokens.CountAsync() == 0` và không chứa token cũ khi đổi mật khẩu/reset mật khẩu.
   -> *Hệ quả*: Nếu dùng soft delete (`IsRevoked = true`), test sẽ đếm được > 0 và fail 100%.
   -> *Giải pháp*: Mọi hàm thu hồi token trong `ITokenService` (`RevokeTokenAsync`, `RevokeUserTokensAsync`) phải thực hiện xóa cứng (`db.RefreshTokens.Remove(rt)` / `RemoveRange`).

4. *Từ Observation 5*: Test suite phân định rạch ròi HTTP status code giữa đăng nhập và làm mới token khi bị khóa: `/login` trả về 400 Bad Request (ProblemDetails), còn `/refresh` trả về 401 Unauthorized.
   -> *Hệ quả*: Đồng nhất hóa hai mã này sẽ làm fail một trong hai test.

5. *Từ Observation 6*: Endpoint `/api/v1/users/me/password-changes` phải trả về `204 NoContent`, trong khi `/api/v1/auth/reset-password` trả về `200 OK`.

6. *Từ Observation 7 & 8*: Không được ánh xạ `/api/v1/auth/change-password` dưới v1, và không được thay đổi các trường dữ liệu của `UserDto` (đặc biệt là `RoleName`).

---

## 3. Caveats

- **No caveats.** Toàn bộ 72 test cases đã được biên dịch, phân tích mã nguồn và chạy kiểm thử trực tiếp trên môi trường máy trạm.

---

## 4. Conclusion

1. **Mức độ an toàn**: Kế hoạch chuẩn hóa Clean Architecture và bổ sung endpoint `/api/v1/auth/logout` hoàn toàn khả thi và có thể đạt 100% test pass (72/72) với 0 lỗi biên dịch, **VỚI ĐIỀU KIỆN** Worker tuân thủ nghiêm ngặt 5 chỉ dẫn an toàn sau:
   - **DI Fallback**: Resolve `ITokenService` qua `sp.GetService<ITokenService>() ?? new TokenService(db, config);` để tương thích với `ApiTestHost`.
   - **Infrastructure Package**: Bổ sung `Microsoft.AspNetCore.Authentication.JwtBearer` vào `NexusKeys.Infrastructure.csproj`.
   - **Hard Delete Refresh Tokens**: Dùng `db.RefreshTokens.RemoveRange` khi thu hồi token để thỏa mãn assert `CountAsync() == 0`.
   - **Preserve Status Codes**: Giữ 400 cho Login Locked, 401 cho Refresh Locked, 204 cho Password Changes, 200 cho Password Reset, 403 cho SecretCode `NEXUSADMIN`.
   - **Preserve DTOs & Namespaces**: Giữ `AuthResponse` trong `GameStore.API.Endpoints` và `RoleName` trong `UserDto`.

---

## 5. Verification Method

Worker sau khi hoàn thành code phải chạy đúng trình tự kiểm thử độc lập sau để xác thực:

```powershell
# 1. Biên dịch toàn bộ Solution (phải đạt 0 errors, 0 warnings)
dotnet build GameStore.slnx -c Debug

# 2. Biên dịch Storefront
dotnet build src/NexusKeys.Storefront/NexusKeys.Storefront.csproj -c Debug

# 3. Chạy toàn bộ 72 test cases (phải đạt 72/72 passed)
dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --logger "console;verbosity=normal"

# 4. Chạy riêng bộ test Auth
dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --filter "FullyQualifiedName~AuthAndUserApiTests"

# 5. Chạy riêng bộ test Hồi quy
dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --filter "FullyQualifiedName~ImprovementRegressionTests"

# 6. Kiểm tra tính nguyên vẹn của thư mục tests
git status -- tests/
```
*Điều kiện thất bại*: Nếu `dotnet test` trả về bất kỳ test nào Failed, hoặc `git status -- tests/` hiển thị file bị sửa đổi, kết quả sẽ bị coi là vi phạm và không đạt tiêu chuẩn.
