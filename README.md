# DATN - NexusKeys Game Store

Hệ thống thương mại điện tử phân phối game bản quyền và key kích hoạt chính hãng NexusKeys, được xây dựng theo kiến trúc **Clean Architecture nhiều tầng**.

## 🏗️ Cấu trúc Hệ thống (Clean Architecture)

```text
datn/
├── src/
│   ├── NexusKeys.Domain/            # Entities, Enums, Extensions theo module
│   ├── NexusKeys.Infrastructure/    # DbContext, Migrations, Payments, BackgroundTasks, External Services
│   ├── NexusKeys.Api/               # REST API v1, SignalR Hubs, Endpoints theo Features
│   ├── NexusKeys.Admin/             # Admin Management Portal (MVC/Blazor)
│   └── NexusKeys.Storefront/        # Customer Storefront Web App (Blazor Server)
├── tests/
│   └── GameStore.API.ProductKeyTests/ # 72 Unit, Contract & Integration Tests
├── docs/                            # Tài liệu API & Hệ thống giao diện Nexus UI
└── GameStore.slnx                   # Solution file hợp nhất
```

## 🚀 Khởi chạy Hệ thống

### 1. Build toàn bộ Solution
```powershell
dotnet build GameStore.slnx
```

### 2. Chạy Kiểm thử (Tests)
```powershell
dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj
```

### 3. Chạy các Dịch vụ

- **Backend REST API** (Cổng 5021):
```powershell
dotnet run --project src/NexusKeys.Api/NexusKeys.Api.csproj --launch-profile http
```

- **Admin Management Portal** (Cổng 3001):
```powershell
dotnet run --project src/NexusKeys.Admin/NexusKeys.Admin.csproj --launch-profile http
```

- **Customer Storefront** (Cổng 5205):
```powershell
dotnet run --project src/NexusKeys.Storefront/NexusKeys.Storefront.csproj --launch-profile http
```

## 📚 Tài liệu
- REST API Documentation: [docs/REST_API_V1.md](docs/REST_API_V1.md)
- Nexus UI Design System: [docs/NEXUS_UI.md](docs/NEXUS_UI.md)
