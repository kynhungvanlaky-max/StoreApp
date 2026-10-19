using Microsoft.EntityFrameworkCore;
using GameStore.API.Data;
using GameStore.API.Endpoints;
using GameStore.API.Hubs;
using GameStore.API.Services;
using GameStore.API.Interfaces;
using GameStore.API.BackgroundTasks;
using GameStore.API.Infrastructure;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.RateLimiting;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Keep local/server logging portable and avoid requiring Windows Event Log privileges.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Helper function to normalize connection strings (handles Render/Heroku/Supabase postgresql:// URI format)
static string NormalizePostgresConnectionString(string? connStr)
{
    if (string.IsNullOrWhiteSpace(connStr)) return string.Empty;
    if (connStr.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        connStr.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var uri = new Uri(connStr);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var database = uri.AbsolutePath.TrimStart('/');
            var port = uri.Port > 0 ? uri.Port : 5432;
            return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true;Pooling=true;";
        }
        catch
        {
            return connStr;
        }
    }
    return connStr;
}

// Add services to the container.
builder.Services.AddMemoryCache();
var effectiveConnectionString = NormalizePostgresConnectionString(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"]);

builder.Services.AddDbContext<GameStoreDbContext>(options =>
    options.UseNpgsql(effectiveConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3);
        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }));

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    jwtKey = "NexusKeysDefaultSecretKeyForJwtAuthentication2026!@#$%^&*";
    builder.Configuration["Jwt:Key"] = jwtKey;

    if (!builder.Environment.IsDevelopment())
    {
        Console.WriteLine("[WARNING] Chưa cấu hình 'Jwt:Key' hoặc 'Jwt__Key' trên môi trường Production. Đang sử dụng fallback key tạm thời để tránh crash deploy.");
    }
}


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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10;
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("coupon", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 15;
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("chat", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 20;
        opt.QueueLimit = 0;
    });
});

// Configure CORS - đọc origins từ config
var corsOrigins = builder.Configuration.GetSection("AppSettings:CorsOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5205", "http://localhost:5000", "http://localhost:3001" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextjs", policy =>
    {
        policy.WithOrigins(corsOrigins)
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
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<GeminiService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<V1ProblemDetailsFilter>();
builder.Services.AddSignalR();

// Register Background Tasks
builder.Services.AddHostedService<OrderCleanupService>();
builder.Services.AddHostedService<PromotionCleanupService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;
    var problemDetailsService = statusCodeContext.HttpContext.RequestServices
        .GetRequiredService<IProblemDetailsService>();

    await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
    {
        HttpContext = statusCodeContext.HttpContext,
        ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = response.StatusCode,
            Title = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(response.StatusCode)
        }
    });
});
app.UseMiddleware<LegacyApiDeprecationMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowNextjs");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

// MediaStorage - đọc path từ config
var mediaStoragePath = app.Configuration["AppSettings:MediaStoragePath"]
    ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "MediaStorage");
mediaStoragePath = Path.GetFullPath(mediaStoragePath);

if (Directory.Exists(mediaStoragePath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(mediaStoragePath),
        RequestPath = "/media",
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
            ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        }
    });
}

// Map Endpoints
app.MapGet("/", () => "NexusKeys API is running successfully!");
app.MapAuthEndpoints();
app.MapProductEndpoints();
app.MapUserEndpoints();
app.MapOrderEndpoints();
app.MapProductKeyEndpoints();
app.MapUploadEndpoints();
app.MapAssistantEndpoints();
app.MapPaymentGatewayEndpoints();
app.MapNewsEndpoints();
app.MapReviewEndpoints();
app.MapWishlistEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapDebugEndpoints();
}
app.MapSupportEndpoints();
app.MapWalletEndpoints();
app.MapPromotionEndpoints();
app.MapCouponEndpoints();
app.MapAdminEndpoints();
app.MapAdminAuditEndpoints();
app.MapRecommendationEndpoints();
app.MapCategoryEndpoints();

// Versioned API contract. Legacy /api routes remain available during migration.
const string v1Prefix = "/api/v1";
app.MapGet(v1Prefix, () => Results.Ok(new
{
    name = "NexusKeys API",
    version = "v1",
    status = "available"
}));
var v1 = app.MapGroup(v1Prefix)
    .AddEndpointFilter<V1ProblemDetailsFilter>();
v1.MapAuthEndpoints(string.Empty);
v1.MapProductEndpoints(string.Empty);
v1.MapUserEndpoints(string.Empty);
v1.MapOrderEndpoints(string.Empty, mapPaymentCallbacks: false);
v1.MapProductKeyEndpoints(string.Empty);
v1.MapUploadEndpoints(string.Empty);
v1.MapAssistantEndpoints(string.Empty);
v1.MapPaymentGatewayEndpoints(string.Empty);
v1.MapNewsEndpoints(string.Empty);
v1.MapReviewEndpoints(string.Empty);
v1.MapWishlistEndpoints(string.Empty);
v1.MapSupportEndpoints(string.Empty);
v1.MapWalletEndpoints(string.Empty);
v1.MapPromotionEndpoints(string.Empty);
v1.MapCouponEndpoints(string.Empty);
v1.MapAdminEndpoints(string.Empty);
v1.MapAdminAuditEndpoints(string.Empty);
v1.MapRecommendationEndpoints(string.Empty);
v1.MapCategoryEndpoints(string.Empty);
app.MapHub<NexusHub>("/hubs/nexus");

// Đảm bảo database đã được tạo và seed dữ liệu đầy đủ nếu chưa có bảng/dữ liệu
if (!string.IsNullOrWhiteSpace(effectiveConnectionString))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
    try
    {
        var databaseCreator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>(db.Database);
        if (databaseCreator != null && !databaseCreator.HasTables())
        {
            try
            {
                databaseCreator.CreateTables();
                Console.WriteLine("[DB INIT] PostgreSQL tables created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB INIT] CreateTables note: {ex.Message}");
            }
        }

        if (!db.Products.Any())
        {
            GameStore.API.Data.DbInitializer.Seed(db);
            Console.WriteLine("[DB INIT] Initial seed data inserted successfully.");
        }
        else
        {
            GameStore.API.Data.DbInitializer.EnsureSeedRolesAndUsers(db);
            Console.WriteLine("[DB INIT] Database is ready and roles/users ensured.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB INIT] Note: {ex.Message}");
    }
}

app.Run();
