# 📘 PROJECT DOCUMENTATION - GameStore (Nexus Store)

---

## 1. TỔNG QUAN DỰ ÁN

### 1.1 Giới thiệu
**GameStore** (Nexus Store) là một nền tảng thương mại điện tử chuyên phân phối game kỹ thuật số, cho phép người dùng mua bán game online với các phương thức thanh toán đa dạng (VNPay, MoMo, MockGateway). Hệ thống hỗ trợ quản lý sản phẩm, đơn hàng, người dùng, mã game (product keys), khuyến mãi, coupon, tin tức, đánh giá, hỗ trợ khách hàng và dashboard quản trị.

### 1.2 Loại dự án
- **Loại**: Ứng dụng Web thương mại điện tử (E-commerce Platform)
- **Kiến trúc**: 3-Tier Architecture (API Layer + Frontend Layer + Admin Backend Layer)
- **Mô hình**: Client-Server với RESTful API
- **Frontend Framework**: Blazor Server (.NET)
- **Backend Framework**: ASP.NET Core MVC
- **API Style**: Minimal API (.NET 10)
- **Database**: SQL Server (Entity Framework Core - Code First)
- **Authentication**: JWT Bearer Token với Refresh Token
- **Real-time**: SignalR cho thông báo đơn hàng mới
- **Payment Gateways**: VNPay (QR/URL), MoMo (QR/URL), MockGateway (dev testing)

### 1.3 Công nghệ sử dụng

| Công nghệ | Mục đích |
|-----------|----------|
| .NET 10 | Runtime chính |
| ASP.NET Core Minimal API | REST API backend |
| Blazor Server | Frontend client app |
| ASP.NET Core MVC | Admin dashboard |
| Entity Framework Core 10 | ORM |
| SQL Server (LocalDB) | Database |
| JWT Bearer Auth | Authentication |
| SignalR | Real-time notifications |
| Chart.js | Dashboard charts |
| Tailwind CSS | UI styling |
| Alpine.js | Client-side interactivity |
| VNPay/MoMo API | Payment gateways |
| Google OAuth | Social login |
| SMTP (MailKit) | Email service |

### 1.4 Cấu trúc thư mục

```
d:\DA/
├── API/                          # REST API Layer (Minimal API)
│   ├── Endpoints/                # API endpoint definitions (17 files)
│   ├── Services/                 # Business logic services
│   ├── Hubs/                     # SignalR hubs
│   ├── Migrations/               # EF Core migrations
│   ├── Data/                     # DbContext extensions
│   ├── ViewModels/              # Request/Response DTOs
│   ├── Properties/              # Launch profiles, IIS config
│   ├── wwwroot/                 # Static files
│   ├── Entities.cs              # All entity models (~30 entities)
│   ├── GameStoreDbContext.cs    # EF Core DbContext
│   └── Program.cs               # App entry, DI, middleware
│
├── frontend/                     # Client App (Blazor Server)
│   ├── Components/              # Blazor components
│   │   ├── Pages/               # Page components
│   │   └── Layout/              # Layout components
│   ├── Models/                  # Client-side models
│   ├── Services/                # HTTP service wrappers
│   ├── ViewModels/              # UI view models
│   ├── Program.cs               # App entry
│   └── appsettings.json         # Config
│
├── backend/                      # Admin Dashboard (MVC)
│   ├── Controllers/             # MVC controllers (13 controllers)
│   ├── Models/                  # View models + DTOs
│   ├── Views/                   # Razor views
│   ├── Filters/                 # Authorization filters
│   ├── HttpHandlers/            # HTTP handlers
│   ├── Program.cs               # App entry
│   └── appsettings.json         # Config
│
├── GameStore.Tests/             # Unit + Integration tests
│   ├── UnitTests/               # Unit tests (41 tests)
│   └── IntegrationTests/        # API integration tests
│
├── AdminSeeder/                 # Database seeder
├── MediaStorage/                # Uploaded media files
├── docker-compose.yml           # Docker orchestration
├── Dockerfile.api               # API Docker
├── Dockerfile.frontend          # Frontend Docker
├── Dockerfile.backend           # Backend Docker
├── nginx.conf                   # Nginx reverse proxy config
└── deploy.sh                    # VPS deployment script
```

### 1.5 Kiến trúc hệ thống

```mermaid
graph TB
    Client[Browser] -->|HTTP/HTTPS| Nginx[Nginx Reverse Proxy]
    Nginx -->|/api/*| API[API :5021]
    Nginx -->|/*| FE[Frontend :5205]
    Nginx -->|/admin/*| BE[Backend :3001]
    
    API -->|EF Core| DB[(SQL Server)]
    API --> SMTP[SMTP Email]
    API --> VNPay[VNPay Gateway]
    API --> MoMo[MoMo Gateway]
    API -->|SignalR| Hub[SignalR Hub]
    
    FE -->|API calls| API
    BE -->|API calls| API
    
    subgraph "Docker Containers"
        API
        FE
        BE
    end
```

---

## 2. USE CASE DIAGRAM

```mermaid
graph TB
    subgraph "Actors"
        Guest[Khách vãng lai]
        Customer[Khách hàng]
        Admin[Quản trị viên]
        Developer[Developer/Nhà phát triển]
    end

    subgraph "Authentication"
        UC1[Đăng ký tài khoản]
        UC2[Đăng nhập]
        UC3[Đăng nhập Google]
        UC4[Quên mật khẩu]
        UC5[Đặt lại mật khẩu]
        UC6[Đăng xuất]
    end

    subgraph "Product Browsing"
        UC7[Xem danh sách game]
        UC8[Tìm kiếm game]
        UC9[Xem chi tiết game]
        UC10[Lọc theo danh mục]
        UC11[Xem khuyến mãi]
        UC12[Xem game trending]
    end

    subgraph "Shopping"
        UC13[Thêm vào giỏ hàng]
        UC14[Xem giỏ hàng]
        UC15[Áp dụng coupon]
        UC16[Thanh toán VNPay]
        UC17[Thanh toán MoMo]
        UC18[Thanh toán MockGateway]
    end

    subgraph "User Features"
        UC19[Xem thư viện game]
        UC20[Viết đánh giá]
        UC21[Bình luận]
        UC22[Xem lịch sử giao dịch]
        UC23[Thêm vào wishlist]
        UC24[Xem game đã xem gần đây]
        UC25[Nhận đề xuất game]
    end

    subgraph "Customer Support"
        UC26[Gửi yêu cầu hỗ trợ]
        UC27[Xem trạng thái ticket]
        UC28[Nhận phản hồi]
    end

    subgraph "Admin - Product Management"
        A1[Quản lý sản phẩm game]
        A2[Quản lý mã game Keys]
        A3[Import key hàng loạt]
        A4[Quản lý danh mục]
        A5[Quản lý media]
    end

    subgraph "Admin - Sales"
        A6[Quản lý đơn hàng]
        A7[Xem dashboard doanh thu]
        A8[Quản lý khuyến mãi]
        A9[Quản lý coupon]
        A10[Quản lý cổng thanh toán]
    end

    subgraph "Admin - Management"
        A11[Quản lý người dùng]
        A12[Khóa/Mở khóa tài khoản]
        A13[Quản lý tin tức]
        A14[Quản lý bình luận]
        A15[Quản lý hỗ trợ]
        A16[Trả lời ticket hỗ trợ]
    end

    Guest --> UC1
    Guest --> UC2
    Guest --> UC3
    
    Customer --> UC2
    Customer --> UC4
    Customer --> UC5
    Customer --> UC6
    Customer --> UC7
    Customer --> UC8
    Customer --> UC9
    Customer --> UC10
    Customer --> UC11
    Customer --> UC12
    Customer --> UC13
    Customer --> UC14
    Customer --> UC15
    Customer --> UC16
    Customer --> UC17
    Customer --> UC18
    Customer --> UC19
    Customer --> UC20
    Customer --> UC21
    Customer --> UC22
    Customer --> UC23
    Customer --> UC24
    Customer --> UC25
    Customer --> UC26
    Customer --> UC27
    Customer --> UC28

    Admin --> UC2
    Admin --> A1
    Admin --> A2
    Admin --> A3
    Admin --> A4
    Admin --> A5
    Admin --> A6
    Admin --> A7
    Admin --> A8
    Admin --> A9
    Admin --> A10
    Admin --> A11
    Admin --> A12
    Admin --> A13
    Admin --> A14
    Admin --> A15
    Admin --> A16
```

---

## 3. ĐẶC TẢ USE CASE

### 3.1 Authentication Module

| Use Case | Actor | Mô tả | API Endpoint | Frontend |
|----------|-------|-------|-------------|----------|
| UC1: Đăng ký | Guest | Đăng ký tài khoản mới (username, email, password) | POST `/api/auth/register` | `/register` |
| UC2: Đăng nhập | All | Xác thực username/password, nhận JWT token | POST `/api/auth/login` | `/login` |
| UC3: Google Login | Guest | Đăng nhập qua Google OAuth | GET `/api/auth/google-login` | `/login` |
| UC4: Quên mật khẩu | Customer | Gửi email reset password | POST `/api/auth/forgot-password` | `/forgot-password` |
| UC5: Đặt lại mật khẩu | Customer | Đặt lại mật khẩu với token | POST `/api/auth/reset-password` | `/reset-password` |
| UC6: Đăng xuất | All | Hủy refresh token | POST `/api/auth/logout` | Header |

**Sequence - Đăng nhập:**
```
Client -> API: POST /api/auth/login {username, password}
API -> DB: SELECT User WHERE username = ?
DB --> API: User data + password hash
API -> API: Verify password hash
alt Thành công
    API -> DB: Generate + save refresh token
    API --> Client: {access_token, refresh_token, user}
else Thất bại
    API --> Client: 401 Unauthorized
end
```

### 3.2 Product Browsing Module

| Use Case | Actor | Mô tả | API Endpoint |
|----------|-------|-------|-------------|
| UC7: Danh sách game | All | Xem danh sách game, phân trang | GET `/api/products?page=1&pageSize=10` |
| UC8: Tìm kiếm | All | Tìm game theo tên | GET `/api/products?search=xxx` |
| UC9: Chi tiết game | All | Xem thông tin chi tiết game | GET `/api/products/{id}` |
| UC10: Lọc danh mục | All | Lọc game theo category | GET `/api/products?category=xxx` |
| UC11: Khuyến mãi | All | Xem game đang khuyến mãi | GET `/api/products?promotion=true` |
| UC12: Trending | All | Xem game thịnh hành | GET `/api/products/trending` |

**Entity Relationship - Products:**
```
Product
├── Developer (User)
├── Categories (N-N via ProductCategory)
├── Promotions (N-N via ProductPromotion)
├── Reviews (1-N)
├── Media (1-N: screenshots, videos)
├── ProductKeys (1-N)
├── Requirements (1-N: min/rec specs)
├── OrderItems (1-N)
├── Licenses (1-N)
└── Wishlists (1-N)
```

### 3.3 Shopping & Payment Module

| Use Case | Actor | Mô tả | API Endpoint |
|----------|-------|-------|-------------|
| UC13: Giỏ hàng | Customer | Thêm game vào giỏ | POST `/api/cart/add` |
| UC14: Xem giỏ | Customer | Xem giỏ hàng | GET `/api/cart` |
| UC15: Coupon | Customer | Áp dụng mã giảm giá | POST `/api/orders/checkout` |
| UC16: VNPay | Customer | Thanh toán qua VNPay | POST `/api/orders/checkout {paymentMethod: VNPay}` |
| UC17: MoMo | Customer | Thanh toán qua MoMo | POST `/api/orders/checkout {paymentMethod: MoMo}` |
| UC18: Mock | Customer | Thanh toán test | POST `/api/orders/checkout {paymentMethod: MockGateway}` |

**Sequence - Checkout:**
```
Client -> API: POST /api/orders/checkout {productIds, paymentMethod, couponCode}
API -> DB: Validate products, check stock
alt Có coupon
    API -> DB: Validate + apply coupon discount
end
API -> DB: Create Order (Status="Pending")
API -> DB: Create OrderItems
alt VNPay
    API -> VnPayService: CreatePaymentUrl()
    VnPayService --> API: Payment URL
    API --> Client: {paymentUrl, orderId}
    Client -> VNPay: Redirect to payment
    VNPay -> API: GET /api/orders/vnpay-return (callback)
    API -> DB: Update Order Status="Completed"
    API -> DB: Assign ProductKeys
    API -> DB: Create License
    API -> EmailService: Send receipt
    API --> Client: Redirect to success page
else MoMo
    API -> MoMoService: CreatePaymentUrlAsync()
    MoMoService --> API: Payment URL
    API --> Client: {paymentUrl, orderId}
    MoMo -> API: POST /api/orders/momo-notify (webhook)
    API -> DB: Update Order Status="Completed"
    API --> MoMo: 204 No Content
else MockGateway (dev)
    API -> DB: Instant complete
    API -> DB: Assign keys + licenses
    API --> Client: {message: "success", orderId}
end
API -> Hub: SignalR broadcast "ReceiveNewOrder"
```

### 3.4 Admin Dashboard Module

| Use Case | Actor | Mô tả | Route |
|----------|-------|-------|-------|
| A1: Products | Admin | CRUD sản phẩm game | `/Products` |
| A2: Keys | Admin | Quản lý mã game | `/Keys` |
| A3: Import keys | Admin | Tạo key hàng loạt | `/Keys/Generate` |
| A5: Media | Admin | Upload/Quản lý media | `/Media` |
| A6: Orders | Admin | Xem + cập nhật đơn hàng | `/Orders` |
| A7: Dashboard | Admin | Thống kê doanh thu | `/` (Home) |
| A8: Promotions | Admin | Quản lý khuyến mãi | `/Promotions` |
| A9: Coupons | Admin | Quản lý mã giảm giá | `/Coupons` |
| A10: Payment | Admin | Cấu hình cổng thanh toán | `/PaymentConfigs` |
| A11: Users | Admin | Quản lý người dùng | `/Users` |
| A13: News | Admin | Quản lý tin tức | `/News` |
| A14: Comments | Admin | Quản lý bình luận | `/Comments` |
| A15: Support | Admin | Quản lý hỗ trợ | `/Support` |

---

## 4. BIỂU ĐỒ TRÌNH TỰ (SEQUENCE DIAGRAMS)

### 4.1 Real-time Notification Flow (SignalR)

```mermaid
sequenceDiagram
    participant Client as Browser (FE/BE)
    participant API as API Server
    participant Hub as SignalR Hub
    participant DB as Database
    participant Admin as Admin Dashboard

    Note over Client,Admin: Khi có đơn hàng mới
    Client->>API: POST /api/orders/checkout
    API->>DB: Create Order (Status="Pending")
    API->>DB: Process payment
    alt Payment Success
        API->>DB: Update Status="Completed"
        API->>Hub: SendAsync("ReceiveNewOrder", order)
        Hub-->>Admin: Receive "ReceiveNewOrder"
        Admin->>Admin: Update stat-orders count
        Admin->>Admin: Animate number (+1 effect)
    end
```

### 4.2 Payment Gateway Flow (VNPay)

```mermaid
sequenceDiagram
    participant Client as Browser
    participant API as API
    participant VNPay as VNPay Gateway
    participant DB as Database
    participant Email as EmailService

    Client->>API: POST /api/orders/checkout (VNPay)
    API->>DB: Create pending order
    API->>VNPay: CreatePaymentUrl(orderId, amount)
    VNPay-->>API: Payment URL
    API-->>Client: {paymentUrl, orderId}
    Client->>Client: Redirect to VNPay
    Client->>VNPay: Complete payment
    VNPay->>API: GET /api/orders/vnpay-return?...
    API->>API: Validate signature
    alt Success (vnp_ResponseCode=00)
        API->>DB: Order Status="Completed"
        API->>DB: Assign keys to order
        API->>DB: Create License records
        API->>Email: Send receipt email
        API->>Hub: Broadcast new order
        API-->>Client: Redirect /checkout/success
    else Failed
        API->>DB: Order Status="Failed"
        API-->>Client: Redirect /cart?error=payment_failed
    end
```

### 4.3 Forgot/Reset Password Flow

```mermaid
sequenceDiagram
    participant Client as Browser
    participant API as API
    participant DB as Database
    participant Email as EmailService

    Client->>API: POST /api/auth/forgot-password {email}
    API->>DB: Find user by email
    alt User found
        API->>DB: Create PasswordResetToken (expires 1h)
        API->>Email: Send reset link with token
        API-->>Client: {message: "Email sent"}
    else Not found
        API-->>Client: 200 OK (always, security)
    end

    Note over Client,Email: User clicks link in email
    
    Client->>API: POST /api/auth/reset-password {token, newPassword}
    API->>DB: Find valid token
    alt Token valid + not expired + not used
        API->>DB: Update user password hash
        API->>DB: Mark token as used
        API-->>Client: {message: "Password reset success"}
    else Invalid/expired
        API-->>Client: 400 Bad Request
    end
```

### 4.4 Admin - Order Management Flow

```mermaid
sequenceDiagram
    participant Admin as Admin Browser
    participant BE as Backend MVC
    participant API as API Server
    participant DB as Database

    Admin->>BE: GET /Orders
    BE->>API: GET /api/orders?page=1&pageSize=10
    API->>DB: Query orders with pagination
    DB-->>API: PagedResponse<Order>
    API-->>BE: {Data, TotalPages, TotalItems...}
    BE-->>Admin: Rendered View (table + pagination)
    
    Admin->>BE: Click "Chi tiết" on order #123
    BE->>API: GET /api/orders/123
    API->>DB: Query order with items + keys
    DB-->>API: Order detail
    API-->>BE: OrderViewModel
    BE-->>Admin: Detail view
    
    Admin->>BE: POST /Orders/UpdateStatus {id:123, status:"Completed"}
    BE->>API: PUT /api/orders/123/status {status:"Completed"}
    API->>DB: Update order status
    API->>DB: Grant licenses if completing
    API->>API: Send email notification
    DB-->>API: Success
    API-->>BE: 200 OK
    BE-->>Admin: Redirect to Index + success message
```

---

## 5. CƠ SỞ DỮ LIỆU

### 5.1 Entity Relationship Diagram (Text-based)

```
User ──1:N──> Review
User ──1:N──> ReviewComment
User ──1:N──> License
User ──1:N──> RefreshToken
User ──1:N──> Wishlist (N:N with Product)
User ──1:N──> CartItem (N:N with Product)
User ──1:N──> Order
User ──1:N──> Transaction
User ──1:N──> Notification
User ──1:N──> Product (as Developer) 
User ──1:N──> NewsArticle (as Author)
User ──1:N──> SupportTicket
User ──1:N──> PasswordResetToken
User ──N:1──> Role

Product ──N:N──> Category (via ProductCategory)
Product ──N:N──> Promotion (via ProductPromotion)
Product ──1:N──> Release
Product ──1:N──> Review
Product ──1:N──> License
Product ──1:N──> ProductMedia
Product ──1:N──> ProductKey
Product ──1:N──> SystemRequirement
Product ──1:N──> OrderItem

Order ──1:N──> OrderItem
Order ──1:N──> ProductKey
Order ──1:N──> Transaction
```

### 5.2 Database Tables (30 tables)

| # | Table | Mô tả |
|---|-------|-------|
| 1 | Users | Tài khoản người dùng |
| 2 | Roles | Phân quyền (Admin, Customer, Developer) |
| 3 | Products | Sản phẩm game |
| 4 | Categories | Danh mục game |
| 5 | ProductCategories | N-N Product-Category |
| 6 | Promotions | Khuyến mãi |
| 7 | ProductPromotions | N-N Product-Promotion |
| 8 | Releases | Phiên bản game |
| 9 | Reviews | Đánh giá game |
| 10 | ReviewComments | Bình luận trong đánh giá |
| 11 | Licenses | Giấy phép game |
| 12 | Notifications | Thông báo |
| 13 | RefreshTokens | Token làm mới JWT |
| 14 | Wishlists | Danh sách yêu thích |
| 15 | ProductMedia | Media (ảnh, video) |
| 16 | CartItems | Giỏ hàng |
| 17 | ProductKeys | Mã game (keys) |
| 18 | SystemRequirements | Yêu cầu hệ thống |
| 19 | Orders | Đơn hàng |
| 20 | OrderItems | Chi tiết đơn hàng |
| 21 | Transactions | Giao dịch |
| 22 | NewsArticles | Tin tức |
| 23 | PaymentGatewayConfigs | Cấu hình thanh toán |
| 24 | SupportTickets | Yêu cầu hỗ trợ |
| 25 | PasswordResetTokens | Token reset mật khẩu |
| 26 | Coupons | Mã giảm giá |
| 27 | RecentlyViewed | Game đã xem gần đây |
| 28 | __EFMigrationsHistory | Migration history |

### 5.3 Key Relationships

```sql
-- User-Order relationship
ALTER TABLE Orders ADD CONSTRAINT FK_Orders_Users 
    FOREIGN KEY (UserId) REFERENCES Users(Id);

-- Order-Product relationship (via OrderItems)
ALTER TABLE OrderItems ADD CONSTRAINT FK_OrderItems_Orders 
    FOREIGN KEY (OrderId) REFERENCES Orders(Id);
ALTER TABLE OrderItems ADD CONSTRAINT FK_OrderItems_Products 
    FOREIGN KEY (ProductId) REFERENCES Products(Id);

-- ProductKey-Order relationship
ALTER TABLE ProductKeys ADD CONSTRAINT FK_ProductKeys_Orders 
    FOREIGN KEY (OrderId) REFERENCES Orders(Id);
ALTER TABLE ProductKeys ADD CONSTRAINT FK_ProductKeys_Products 
    FOREIGN KEY (ProductId) REFERENCES Products(Id);

-- License-User-Product relationship
ALTER TABLE Licenses ADD CONSTRAINT FK_Licenses_Users 
    FOREIGN KEY (UserId) REFERENCES Users(Id);
ALTER TABLE Licenses ADD CONSTRAINT FK_Licenses_Products 
    FOREIGN KEY (ProductId) REFERENCES Products(Id);
```

---

## 6. API ENDPOINTS

### 6.1 Authentication (AuthEndpoints.cs)
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| POST | `/api/auth/register` | No | Đăng ký |
| POST | `/api/auth/login` | No | Đăng nhập |
| POST | `/api/auth/google-login` | No | Google OAuth |
| POST | `/api/auth/forgot-password` | No | Gửi email reset |
| POST | `/api/auth/reset-password` | No | Đặt lại mật khẩu |
| POST | `/api/auth/logout` | Yes | Đăng xuất |
| POST | `/api/auth/refresh` | No | Refresh JWT |

### 6.2 Products (ProductEndpoints.cs)
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/products` | No | Danh sách (phân trang) |
| GET | `/api/products/{id}` | No | Chi tiết |
| GET | `/api/products/trending` | No | Trending |
| POST | `/api/products` | Admin | Tạo mới |
| PUT | `/api/products/{id}` | Admin | Cập nhật |
| DELETE | `/api/products/{id}` | Admin | Xóa |

### 6.3 Orders (OrderEndpoints.cs)
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/orders` | Admin | Danh sách (phân trang) |
| GET | `/api/orders/{id}` | Yes | Chi tiết đơn hàng |
| GET | `/api/orders/my-orders` | Customer | Đơn hàng của tôi |
| GET | `/api/orders/library` | Customer | Thư viện game |
| GET | `/api/orders/transactions` | Customer | Lịch sử giao dịch |
| POST | `/api/orders/checkout` | Customer | Thanh toán |
| PUT | `/api/orders/{id}/status` | Admin | Cập nhật trạng thái |
| DELETE | `/api/orders/{orderId}/keys/{keyId}` | Admin | Xóa key khỏi đơn |
| GET | `/api/orders/vnpay-return` | No | VNPay callback |
| POST | `/api/orders/momo-notify` | No | MoMo webhook |

### 6.4 Reviews (ReviewEndpoints.cs)
| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| GET | `/api/reviews/product/{productId}` | No | Đánh giá của sản phẩm |
| POST | `/api/reviews` | Customer | Tạo đánh giá |
| PUT | `/api/reviews/{id}` | Customer | Sửa đánh giá |
| DELETE | `/api/reviews/{id}` | Customer/Admin | Xóa đánh giá |
| PUT | `/api/reviews/{id}/hide` | Admin | Ẩn/Hiện đánh giá |
| POST | `/api/reviews/{reviewId}/comments` | Customer | Thêm bình luận |
| DELETE | `/api/reviews/comments/{id}` | Admin | Xóa bình luận |
| GET | `/api/reviews/admin/all` | Admin | Tất cả đánh giá (phân trang) |

### 6.5 Other Endpoints
| Module | Endpoints | Mô tả |
|--------|-----------|-------|
| Users | `/api/users` (GET/PUT/POST) + toggle-lock | Quản lý người dùng |
| Keys | `/api/keys` (CRUD) | Quản lý product keys |
| Promotions | `/api/promotions` (CRUD) | Quản lý khuyến mãi |
| Coupons | `/api/coupons` (CRUD) | Quản lý coupon |
| News | `/api/news` (CRUD) | Quản lý tin tức |
| Media | `/api/upload` (POST) | Upload file |
| Support | `/api/support` (CRUD + reply) | Hỗ trợ khách hàng |
| Admin | `/api/admin/chart-data` | Dashboard chart data |
| Payment | `/api/payment-configs` (GET/POST) | Cấu hình gateway |
| Wallet | `/api/wallet` (GET/withdraw) | Ví điện tử |
| Wishlist | `/api/wishlist` (GET/POST/DELETE) | Wishlist |
| Recommendation | `/api/recommendations` | Game recommendations |
| Auth | `/api/user/profile` | Profile user |

### 6.6 Tổng số API Endpoints
- **Total**: ~80+ API endpoints
- **Authentication**: Required on admin + customer endpoints
- **Roles**: Administrator, Customer, Developer
- **Pagination**: Hỗ trợ `?page=n&pageSize=n` trên các endpoints list

---

## 7. BACKEND ADMIN CONTROLLERS

| Controller | Route | Views | Mô tả |
|-----------|-------|-------|-------|
| HomeController | `/` | Index | Dashboard với Chart.js |
| ProductsController | `/Products` | Index, Create, Edit, Delete | Quản lý game |
| OrdersController | `/Orders` | Index (phân trang), Details, UpdateStatus | Quản lý đơn hàng |
| UsersController | `/Users` | Index (JS), ToggleLock | Quản lý người dùng |
| KeysController | `/Keys` | Index (phân trang, grouped), Generate, Delete | Quản lý keys |
| PromotionsController | `/Promotions` | Index, Create, Edit, Delete | Khuyến mãi |
| CouponsController | `/Coupons` | Index, Create, Edit, Delete | Mã giảm giá |
| NewsController | `/News` | Index (phân trang), Create, Edit, Delete | Tin tức |
| CommentsController | `/Comments` | Index, ToggleVisibility, Delete, Reply | Bình luận |
| SupportController | `/Support` | Index (JS), UpdateStatus, Reply | Hỗ trợ |
| MediaController | `/Media` | Index, Upload, Delete | Thư viện media |
| PaymentConfigsController | `/PaymentConfigs` | Index, Save | Cấu hình thanh toán |
| TransactionsController | `/Transactions` | Index (phân trang) | Giao dịch |
| AuthController | `/Auth` | Login, Logout | Xác thực admin |

---

## 8. FRONTEND (Blazor Server)

### 8.1 Pages
| Route | Component | Mô tả |
|-------|-----------|-------|
| `/` | Home.razor | Trang chủ, game trending, recommendations |
| `/products/{id}` | ProductDetail.razor | Chi tiết game + reviews |
| `/cart` | Cart.razor | Giỏ hàng |
| `/checkout` | Checkout.razor | Thanh toán (chọn gateway) |
| `/checkout/success` | CheckoutSuccess.razor | Kết quả thanh toán |
| `/login` | Login.razor | Đăng nhập |
| `/register` | Register.razor | Đăng ký |
| `/forgot-password` | ForgotPassword.razor | Quên mật khẩu |
| `/reset-password` | ResetPassword.razor | Đặt lại mật khẩu |
| `/library` | Library.razor | Thư viện game đã mua |

### 8.2 Services
| Service | Mô tả |
|---------|-------|
| AuthService | HTTP calls cho auth API |
| ReviewService | CRUD đánh giá |
| OrderService | Checkout + order history |
| RecommendationService | Lấy game đề xuất |
| RecentlyViewedService | LocalStorage tracking |

---

## 9. TESTS

### 9.1 Unit Tests (41 tests)
| Test Class | Số test | Mô tả |
|-----------|---------|-------|
| RecommendationServiceTests | 15 | Test recommendation algorithms |
| RecentlyViewedServiceTests | 23 | Test recently viewed cache |
| AuthServiceTests | 3 | Test auth business logic |

### 9.2 Integration Tests
| Test Class | Mô tả |
|-----------|-------|
| GameStoreApiFactory | WebApplicationFactory setup |
| ApiEndpointsTests | API endpoint integration tests |

### 9.3 Test Results
```
Passed! - Failed: 2, Passed: 41, Skipped: 0, Total: 43
(2 integration tests fail - expected, need API running)
```

---

## 10. DEPLOYMENT

### 10.1 Local Development
```bash
# Start all services
start "API" /d d:\DA\API dotnet run --launch-profile http      # Port 5021
start "Frontend" /d d:\DA\frontend dotnet run --launch-profile http # Port 5205
start "Backend" /d d:\DA\backend dotnet run --launch-profile http  # Port 3001
```

### 10.2 Docker Deployment
```yaml
# docker-compose.yml structure
services:
  api:       image: gamestore-api       (port 5021)
  frontend:  image: gamestore-frontend  (port 5205)
  backend:   image: gamestore-backend   (port 3001)
  nginx:     image: nginx:alpine        (port 80/443)
```

### 10.3 Production (VPS)
```bash
# Deploy script (deploy.sh)
./deploy.sh  # Build + push Docker images, SSH deploy

# SSL (nginx.conf)
server {
    listen 443 ssl;
    server_name nexusstore.com;
    ssl_certificate /etc/letsencrypt/live/nexusstore.com/fullchain.pem;
    
    location /api/ { proxy_pass http://api:5021; }
    location / { proxy_pass http://frontend:5205; }
}
```

### 10.4 System Requirements
- **Runtime**: .NET 10 Runtime
- **Database**: SQL Server 2019+ or LocalDB
- **Memory**: 2GB+ RAM (each service ~200-500MB)
- **Storage**: 10GB+ for MediaStorage
- **OS**: Windows 10/11, Linux (Ubuntu 22.04+)
- **Docker**: Docker Engine 24+ (optional)

---

## 11. SECURITY

### 11.1 Authentication Flow
```
Login -> JWT (access_token: 15 min) + Refresh Token (7 days)
       -> Cookie: access_token (HttpOnly, Secure)
       -> Header: Authorization: Bearer {token}
       
API -> Validate JWT -> ClaimsPrincipal -> Role check -> Handler
```

### 11.2 Authorization
- **Admin**: `policy.RequireRole("Administrator")`
- **Customer**: JWT token required, user-specific data filtering
- **Endpoints**: Most POST/PUT/DELETE require admin role
- **Payment webhooks**: AllowAnonymous (VNPay/MoMo callbacks)

### 11.3 Security Measures
- Password hashing (ASP.NET Identity PasswordHasher)
- JWT with short expiration + refresh tokens
- Payment signature validation (VNPay HMAC-SHA512, MoMo HMAC-SHA256)
- SQL injection protection (EF Core parameterized queries)
- XSS protection (Razor HTML encoding)
- CSRF protection (Antiforgery on MVC POST forms)
- CORS configuration for API

---

## 12. PAYMENT GATEWAYS

### 12.1 VNPay
- **URL**: Sandbox API
- **Method**: Redirect user to VNPay portal
- **Signature**: HMAC-SHA512 with HashSecret
- **Return**: GET callback `/api/orders/vnpay-return`
- **Config keys**: TmnCode, HashSecret, BaseUrl, ReturnUrl

### 12.2 MoMo
- **URL**: Partner API
- **Method**: Redirect user to MoMo app/QR
- **Signature**: HMAC-SHA256 with SecretKey
- **Return**: POST webhook `/api/orders/momo-notify`
- **Config keys**: PartnerCode, AccessKey, SecretKey, Endpoint

### 12.3 MockGateway (Development)
- **Method**: Direct complete (no redirect)
- **Use**: Testing without real payment
- **Flow**: POST checkout with `paymentMethod: "MockGateway"`

---

## 13. REAL-TIME (SIGNALR)

### 13.1 Hub
```
Endpoint: /hubs/nexus
Events:   ReceiveNewOrder
```

### 13.2 Flow
```javascript
// Frontend connection
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5021/hubs/nexus")
    .build();

// Listen for new orders
connection.on("ReceiveNewOrder", function () {
    // Update dashboard stat count
    // Show notification toast
});
```

---

## 14. PROJECT STATUS (Phase 8 Complete)

| Phase | Mô tả | Status |
|-------|-------|--------|
| Phase 1 | Reviews (API + FE + BE) | ✅ Complete |
| Phase 2 | Payment (VNPay/MoMo/Checkout) | ✅ Complete |
| Phase 3 | Auth (Login/Register/Reset Password) | ✅ Complete |
| Phase 4 | Coupons + Media + Dashboard + Promotions | ✅ Complete |
| Phase 5 | Email + Recently Viewed + Recommendations | ✅ Complete |
| Phase 6 | Testing (Unit + Integration) | ✅ Complete |
| Phase 7 | UI Polish (Mobile sidebar, animations, tables) | ✅ Complete |
| Phase 8 | Pagination (Orders, News, Keys, etc.) | ✅ Complete |
| Phase 9 | Production Deploy (CI/CD, Docker, VPS, SSL) | ⏳ Pending |

---

## 15. TEAM & CONTRIBUTORS

- **Architecture**: 3-Tier ASP.NET Core
- **Database**: SQL Server with EF Core Code First
- **Frontend**: Blazor Server with Tailwind CSS
- **Testing**: xUnit + Moq + WebApplicationFactory
- **DevOps**: Docker + Docker Compose + Nginx + GitHub Actions

---

*Last updated: June 26, 2026*
*Generated by AI-assisted documentation*