using Microsoft.EntityFrameworkCore;
using GameStore.API.Data;
using GameStore.API.Entities;
using GameStore.API.Endpoints;
using GameStore.API.Hubs;
using GameStore.API.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<GameStoreDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextjs", policy =>
    {
        policy.WithOrigins("http://localhost:5205", "http://localhost:5000", "http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Payment Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<GameStore.API.Services.Payment.VnPayService>();
builder.Services.AddScoped<GameStore.API.Services.Payment.MoMoService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();
builder.Services.AddSignalR();

var app = builder.Build();

// URL Rewrite Middleware for v1 and compatibility routes
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path != null)
    {
        if (path.StartsWith("/api/v1/", StringComparison.OrdinalIgnoreCase))
        {
            path = "/api/" + path.Substring("/api/v1/".Length);
        }

        if (path.Equals("/api/users/me", StringComparison.OrdinalIgnoreCase))
        {
            path = "/api/users/profile";
        }
        else if (path.Equals("/api/payment-configurations", StringComparison.OrdinalIgnoreCase))
        {
            path = "/api/payment-configs";
        }
        else if (path.StartsWith("/api/audit-logs", StringComparison.OrdinalIgnoreCase))
        {
            path = "/api/admin/audit" + path.Substring("/api/audit-logs".Length);
        }
        else if (path.Equals("/api/transactions", StringComparison.OrdinalIgnoreCase))
        {
            path = "/api/wallet/admin/all";
        }
        else if (path.Equals("/api/support-tickets", StringComparison.OrdinalIgnoreCase))
        {
            path = "/api/support/admin/all";
        }
        else if (path.StartsWith("/api/support-tickets/", StringComparison.OrdinalIgnoreCase))
        {
            path = "/api/support/admin/" + path.Substring("/api/support-tickets/".Length);
        }

        context.Request.Path = path;
    }
    await next();
});

app.UseRouting();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
// Không ép buộc HTTPS redirection trong Docker/Local HTTP container
app.UseCors("AllowNextjs");
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

var mediaDir = Path.Combine(app.Environment.ContentRootPath, "..", "MediaStorage");
if (Directory.Exists(mediaDir))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.GetFullPath(mediaDir)),
        RequestPath = "/media",
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        }
    });
}
else if (System.IO.Directory.Exists(@"D:\DA\MediaStorage"))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(@"D:\DA\MediaStorage"),
        RequestPath = "/media",
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        }
    });
}

// Map Endpoints
app.MapGet("/", () => "Nexus Store API is running successfully!");
app.MapAuthEndpoints();
app.MapProductEndpoints();
app.MapCategoryEndpoints();
app.MapUserEndpoints();
app.MapOrderEndpoints();
app.MapProductKeyEndpoints();
app.MapUploadEndpoints();
app.MapAssistantEndpoints();
app.MapPaymentGatewayEndpoints();
app.MapNewsEndpoints();
app.MapReviewEndpoints();
app.MapWishlistEndpoints();
app.MapDebugEndpoints();
app.MapSupportEndpoints();
app.MapWalletEndpoints();
app.MapPromotionEndpoints();
app.MapCouponEndpoints();
app.MapAdminEndpoints();
app.MapAdminAuditEndpoints();
app.MapRecommendationEndpoints();
app.MapHub<NexusHub>("/hubs/nexus");

if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInit");
        var db = services.GetRequiredService<GameStoreDbContext>();

        if (db.Database.IsRelational())
        {
            // Retry loop chờ SQL Server khởi động hoàn tất (đặc biệt khi chạy Docker lần đầu)
            int maxRetries = 10;
            for (int retry = 1; retry <= maxRetries; retry++)
            {
                try
                {
                    logger.LogInformation("Đang kiểm tra kết nối và cập nhật Database migrations (Lần {Retry}/{Max})...", retry, maxRetries);
                    await db.Database.MigrateAsync();
                    logger.LogInformation("Database migration thành công!");
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Chưa kết nối được Database: {Message}. Thử lại sau 3s...", ex.Message);
                    if (retry == maxRetries)
                    {
                        logger.LogError(ex, "Không thể kết nối đến Database sau nhiều lần thử.");
                        // Không crash app ngay để cho phép API endpoint "/" vẫn trả về trạng thái
                    }
                    else
                    {
                        await Task.Delay(3000);
                    }
                }
            }

            try {
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Reviews ADD CreatedAt datetime2 NOT NULL DEFAULT GETDATE();");
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE Reviews ADD UpdatedAt datetime2 NULL;");
            } catch {}
            try {
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE SupportTickets ADD AdminReply nvarchar(max) NULL;");
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE SupportTickets ADD RepliedAt datetime2 NULL;");
            } catch {}
            try {
                await db.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Coupons')
                    CREATE TABLE Coupons (
                        Id int NOT NULL IDENTITY,
                        Code nvarchar(max) NOT NULL,
                        DiscountPercent decimal(18,2) NOT NULL,
                        MaxUsage int NOT NULL,
                        CurrentUsage int NOT NULL,
                        ExpiryDate datetime2 NOT NULL,
                        IsActive bit NOT NULL,
                        CreatedAt datetime2 NOT NULL,
                        CONSTRAINT PK_Coupons PRIMARY KEY (Id)
                    );
                ");
            } catch {}
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        // Seed Data: Roles
        try
        {
            if (!await db.Roles.AnyAsync())
            {
                var adminRole = new Role { RoleName = "Administrator" };
                var customerRole = new Role { RoleName = "Customer" };
                var developerRole = new Role { RoleName = "Developer" };
                db.Roles.AddRange(adminRole, customerRole, developerRole);
                await db.SaveChangesAsync();
                logger.LogInformation("Đã khởi tạo các vai trò mặc định (Roles).");
            }

            // Seed Data: Admin User
            var adminRoleEntity = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Administrator");
            if (adminRoleEntity != null && !await db.Users.AnyAsync(u => u.RoleId == adminRoleEntity.Id))
            {
                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@nexusstore.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    RoleId = adminRoleEntity.Id,
                    WalletBalance = 10000000,
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(adminUser);

                var testAdmin = new User
                {
                    Username = "testadmin",
                    Email = "testadmin@nexusstore.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    RoleId = adminRoleEntity.Id,
                    WalletBalance = 5000000,
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(testAdmin);
                await db.SaveChangesAsync();
                logger.LogInformation("Đã tạo tài khoản Admin mặc định (admin/admin123 và testadmin/123456).");
            }

            // Seed Data: Categories nếu chưa có
            if (!await db.Categories.AnyAsync())
            {
                db.Categories.AddRange(
                    new Category { Name = "Hành động", Description = "Các trò chơi hành động kịch tính và gay cấn." },
                    new Category { Name = "Nhập vai (RPG)", Description = "Thế giới mở, cốt truyện sâu sắc và phát triển nhân vật." },
                    new Category { Name = "Chiến thuật", Description = "Tư duy chiến thuật thời gian thực hoặc theo lượt." },
                    new Category { Name = "Phiêu lưu", Description = "Khám phá thế giới kì thú và giải đố hấp dẫn." },
                    new Category { Name = "Mô phỏng", Description = "Mô phỏng đời sống, xây dựng và lái xe chân thực." },
                    new Category { Name = "Ứng dụng & Tiện ích", Description = "Phần mềm công cụ, chỉnh sửa và năng suất." }
                );
                await db.SaveChangesAsync();
                logger.LogInformation("Đã khởi tạo các danh mục mẫu (Categories).");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Không thể seed dữ liệu mặc định: {Message}", ex.Message);
        }
    }
}

app.Run();

namespace GameStore.API
{
    public class ApiMarker { }
}
