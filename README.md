# 🎮 GameStore Nexus - Nền Tảng Phân Phối Trò Chơi & Ứng Dụng Kỹ Thuật Số

[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Blazor Server](https://img.shields.io/badge/Frontend-Blazor%20Server-blue.svg)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![ASP.NET MVC](https://img.shields.io/badge/Admin-ASP.NET%20Core%20MVC-blueviolet.svg)](https://dotnet.microsoft.com/apps/aspnet/mvc)
[![Docker](https://img.shields.io/badge/Docker-All--in--One-2496ED.svg?logo=docker)](https://www.docker.com/)
[![SQL Server](https://img.shields.io/badge/Database-SQL%20Server%202022-CC292B.svg?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)

**GameStore Nexus** là nền tảng thương mại điện tử phân phối game và ứng dụng bản quyền hiện đại, áp dụng kiến trúc 3-Tier hiệu năng cao với .NET 10, Blazor Server tương tác thời gian thực, MVC Admin quản trị toàn diện và hệ thống RESTful Minimal API mạnh mẽ.

---

## 🚀 Khởi Chạy Nhanh Bằng Docker (All-in-One)

Hệ thống đã được đóng gói toàn bộ (Database, Backend API, Frontend Client, Admin Portal, Nginx Reverse Proxy) trong một file `docker-compose.yml`. Database sẽ **tự động chạy Migration và Seed sẵn tài khoản Admin, vai trò, danh mục** khi khởi động.

### 1. Yêu Cầu Tiên Quyết
- Đã cài đặt [Docker Desktop](https://www.docker.com/products/docker-desktop/) (hỗ trợ Docker Compose).

### 2. Khởi Chạy Toàn Bộ Hệ Thống Với 1 Lệnh Duy Nhất

Mở Terminal tại thư mục gốc dự án và chạy:

```bash
docker compose up -d --build
```

> 💡 **Lưu ý:** Lần đầu chạy, Docker sẽ tải image và build các service. Khi hoàn tất, toàn bộ hệ thống sẽ sẵn sàng phục vụ!

### 3. Cổng & Địa Chỉ Truy Cập

| Dịch Vụ | Địa Chỉ Truy Cập | Cổng Expose | Ghi Chú |
|:---|:---|:---|:---|
| **🌐 Cổng Chung (Nginx)** | [http://localhost](http://localhost) | `80` | Định tuyến toàn bộ hệ thống qua 1 domain duy nhất |
| **🕹️ Khách Hàng (Frontend Blazor)** | [http://localhost:5205](http://localhost:5205) hoặc [http://localhost](http://localhost) | `5205` | Giao diện mua sắm game/app, giỏ hàng, thanh toán |
| **🛡️ Quản Trị (Admin Dashboard)** | [http://localhost:3001](http://localhost:3001) hoặc [http://localhost/admin](http://localhost/admin) | `3001` | Quản lý sản phẩm, đơn hàng, người dùng, doanh thu |
| **⚡ Backend API (.NET 10)** | [http://localhost:5021](http://localhost:5021) hoặc [http://localhost/api/](http://localhost/api/) | `5021` | Minimal API REST endpoints |
| **🗄️ SQL Server 2022** | `localhost,1433` | `1433` | Database engine lưu trữ dữ liệu |

### 4. Tài Khoản Quản Trị Mặc Định

Khi Docker khởi động lần đầu, hệ thống tự động khởi tạo các tài khoản quản trị sau:

- **Tài khoản chính:**
  - **Tên đăng nhập:** `admin` (hoặc Email: `admin@nexusstore.com`)
  - **Mật khẩu:** `admin123`
- **Tài khoản thử nghiệm:**
  - **Tên đăng nhập:** `testadmin` (hoặc Email: `testadmin@nexusstore.com`)
  - **Mật khẩu:** `123456`

---

## 🛠️ Khởi Chạy Local Cho Lập Trình Viên (.NET SDK)

Nếu bạn muốn chạy trực tiếp bằng source code mà không qua Docker:

### 1. Yêu Cầu
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server LocalDB hoặc SQL Server 2019/2022

### 2. Cập Nhật Chuỗi Kết Nối
Chỉnh sửa file `API/appsettings.json` trỏ về SQL Server của máy bạn:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=GameStoreDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
}
```

### 3. Chạy Từng Service

Mở 3 cửa sổ Terminal riêng biệt:

**Terminal 1 - Khởi chạy API:**
```bash
cd API
dotnet run
# Lắng nghe tại: http://localhost:5021
```

**Terminal 2 - Khởi chạy Admin Portal:**
```bash
cd backend
dotnet run
# Lắng nghe tại: http://localhost:3001
```

**Terminal 3 - Khởi chạy Frontend Blazor:**
```bash
cd frontend
dotnet run
# Lắng nghe tại: http://localhost:5205
```

---

## 🌟 Các Tính Năng Nổi Bật

- **🛒 Trải Nghiệm Mua Sắm Game & App Toàn Diện:**
  - Phân loại rõ ràng giữa **Game** (Huy hiệu tím) và **Ứng dụng & Tiện ích** (Huy hiệu xanh).
  - Khám phá các mục: Sản phẩm nổi bật, Đang giảm giá, Ứng dụng nổi bật, Đã xem gần đây.
  - Gợi ý sản phẩm thông minh dựa trên hành vi duyệt web và sở thích.
- **💳 Hệ Thống Thanh Toán Linh Hoạt:**
  - Tích hợp cổng thanh toán Sandbox **VNPay** và **MoMo**.
  - Tích hợp chế độ **Mock Gateway**: Thanh toán thành công tức thì trong môi trường kiểm thử mà không cần thẻ thật.
  - Tự động tạo License bản quyền và sinh Product Key an toàn ngay sau khi hoàn tất đơn hàng.
- **🛡️ Cổng Quản Trị Admin Chuyên Sâu:**
  - Thống kê doanh thu, số lượng đơn hàng, người dùng bằng biểu đồ trực quan.
  - Quản lý danh mục, sản phẩm, kho Product Key tự động kích hoạt.
  - Duyệt và quản lý bình luận/đánh giá, mã giảm giá (Coupon), hỗ trợ Support Tickets.
- **⚡ Công Nghệ Tối Tân:**
  - **Blazor Server Interactive:** Giao diện mượt mà, phản hồi ngay lập tức, không bị giật layout khi tải dữ liệu.
  - **SignalR Real-time:** Bắn thông báo đơn hàng mới tức thì cho quản trị viên.
  - **Auto Database Migration & Seeding:** Tự động tạo bảng và nạp dữ liệu mẫu ngay khi container khởi động.

---

## 📁 Cấu Trúc Dự Án

```
StoreApp/
├── API/                       # Minimal API backend (.NET 10)
│   ├── Data/                  # DbContext & cấu hình Entity Framework
│   ├── Endpoints/             # Định nghĩa các REST API Endpoints
│   ├── Entities.cs            # Các Entity Model dữ liệu
│   ├── Migrations/            # EF Core Migrations
│   └── Program.cs             # Khởi tạo DI, Pipeline, Auto-Migration & Seeder
├── frontend/                  # Blazor Server Web App
│   ├── Components/            # Razor Components, Pages, Layouts
│   ├── Services/              # Client API Service Callers
│   └── ViewModels/            # State & Data ViewModels
├── backend/                   # Admin Portal (ASP.NET Core MVC)
│   ├── Controllers/           # Admin MVC Controllers
│   ├── Middleware/            # Admin Auth & Token Handlers
│   └── Views/                 # Razor Views & Admin Templates
├── MediaStorage/              # Thư mục lưu trữ hình ảnh & tệp tin media
├── Dockerfile.api             # Multi-stage Dockerfile cho API
├── Dockerfile.frontend        # Multi-stage Dockerfile cho Blazor Frontend
├── Dockerfile.backend         # Multi-stage Dockerfile cho MVC Admin
├── docker-compose.yml         # File điều phối toàn bộ container All-in-One
├── nginx.conf                 # Cấu hình Reverse Proxy Nginx
├── project.md                 # Tài liệu đặc tả kỹ thuật chi tiết
└── README.md                  # Hướng dẫn tổng quan dự án
```

---

## 📞 Hỗ Trợ & Đóng Góp

- Xem tài liệu kỹ thuật chi tiết tại [project.md](project.md).
- Báo cáo lỗi hoặc yêu cầu tính năng qua GitHub Issues.
