# API contract and product-key tests

Đây là project integration test độc lập cho hợp đồng REST `/api/v1`, khả năng tương thích của `/api/keys` và các luồng nghiệp vụ chính.

- Chạy bằng xUnit trên .NET 10.
- API test khởi động trên một cổng ngẫu nhiên.
- Dữ liệu dùng Entity Framework Core InMemory và được tạo mới cho từng test.
- Không kết nối, migrate hoặc thay đổi cơ sở dữ liệu `GameStoreDB` thật.
- Authentication giả lập chỉ tồn tại trong test project.
- Kiểm tra route/method REST, `ProblemDetails`, header deprecation và unique index giao dịch thanh toán.
- Kiểm tra auth: đăng ký, đăng nhập, refresh token, reset/đổi mật khẩu và khóa tài khoản.
- Kiểm tra catalog: product, product key, coupon, promotion, media và cấu hình thanh toán.
- Kiểm tra customer workflow: wishlist, support, ví, order, checkout, review và news.
- Kiểm tra webhook thanh toán gửi lặp không tạo transaction/license/key trùng.

Chạy riêng project:

```powershell
dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj
```

Chạy toàn bộ solution:

```powershell
dotnet test GameStore.slnx
```

## Regression backlog cố ý đỏ

`ImprovementRegressionTests` mô tả các yêu cầu an toàn/nghiệp vụ chưa được API đáp ứng. Các test này có trait `Category=Improvement` và được kỳ vọng **fail** cho đến khi từng vấn đề được sửa.

Chạy riêng backlog cải thiện:

```powershell
dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --filter Category=Improvement
```

Chạy baseline đang xanh, loại trừ backlog:

```powershell
dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj --filter Category!=Improvement
```
