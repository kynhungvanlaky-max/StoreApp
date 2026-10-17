# NexusKeys REST API v1

`/api/v1` là contract REST được frontend và admin portal sử dụng. Các route `/api` cũ vẫn được map trong giai đoạn chuyển tiếp và trả các header:

- `Deprecation: true`
- `Sunset: Fri, 31 Dec 2027 23:59:59 GMT`
- `Link: </api/v1>; rel="successor-version"`

## Quy ước

- `GET` chỉ đọc dữ liệu.
- `POST` tạo tài nguyên hoặc command không idempotent.
- `PUT` thay thế/tạo một quan hệ idempotent.
- `PATCH` cập nhật một phần bằng trạng thái đích, không dùng thao tác toggle.
- Tạo mới trả `201 Created` và `Location`.
- Xóa hoặc cập nhật không có response body trả `204 No Content`.
- Lỗi v1 trả `application/problem+json` và có `traceId`.
- Route ID dùng constraint `:int`; phân trang giới hạn `pageSize` tối đa 100 (product keys tối đa 50).

## Route v1 chính

### Tài khoản hiện tại

```text
GET    /api/v1/users/me
PATCH  /api/v1/users/me
POST   /api/v1/users/me/password-changes
GET    /api/v1/users/me/orders
GET    /api/v1/users/me/library-items
GET    /api/v1/users/me/transactions
GET    /api/v1/users/me/support-tickets
GET    /api/v1/users/me/wishlist-items
GET    /api/v1/users/me/wishlist-items/{productId}
PUT    /api/v1/users/me/wishlist-items/{productId}
DELETE /api/v1/users/me/wishlist-items/{productId}
```

### Quản trị user, order và support

```text
GET   /api/v1/users
GET   /api/v1/users/{id}
PATCH /api/v1/users/{id}
PUT   /api/v1/users/{id}/role

GET    /api/v1/orders
GET    /api/v1/orders/{id}
PATCH  /api/v1/orders/{id}
DELETE /api/v1/orders/{orderId}/product-keys/{keyId}
POST   /api/v1/orders/{id}/payment-attempts
POST   /api/v1/checkouts

POST  /api/v1/support-tickets
GET   /api/v1/support-tickets
PATCH /api/v1/support-tickets/{id}
```

### Catalog và nội dung

```text
/api/v1/products
/api/v1/product-keys
/api/v1/news
/api/v1/coupons
/api/v1/promotions
/api/v1/reviews
/api/v1/review-comments
/api/v1/media-files
/api/v1/payment-configurations
/api/v1/audit-logs
```

Review theo sản phẩm:

```text
GET  /api/v1/products/{productId}/reviews
POST /api/v1/products/{productId}/reviews
```

Kiểm tra coupon:

```text
POST /api/v1/coupon-validations
```

## Payment integrations

Callback của nhà cung cấp nằm ngoài contract REST v1:

```text
GET  /api/integrations/vnpay/return
POST /api/webhooks/momo
```

Callback cũ vẫn hoạt động trong giai đoạn chuyển tiếp. Hoàn tất thanh toán được bảo vệ bởi khóa theo order, kiểm tra transaction đã xử lý và unique index `(PaymentGateway, GatewayTransactionId)`.

Migration cần chạy khi triển khai:

```text
20260826000000_AddUniquePaymentTransaction
```

Nếu migration báo dữ liệu trùng, cần kiểm tra và xử lý transaction trùng trước khi tạo unique index; không nên tự động xóa giao dịch tài chính.

## Chạy kiểm thử

```powershell
dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj
```

Bộ test kiểm tra CRUD product key, authorization, route v1, `ProblemDetails`, header deprecation, callback route và unique payment index.
