using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.API.Endpoints;
using GameStore.API.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameStore.API.ProductKeyTests;

public class CatalogAndCommerceApiTests
{
    [Fact]
    public async Task Products_SearchSortAndPagination_ReturnExpectedPage()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedCatalogAsync(api,
        [
            Product(1, "Alpha Game", 300_000),
            Product(2, "Alpha Deluxe", 100_000),
            Product(3, "Beta Game", 200_000)
        ]);

        using var response = await api.CreateClient().GetAsync(
            "/api/v1/products?search=Alpha&sortBy=price_asc&page=1&pageSize=1");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, json.RootElement.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, json.RootElement.GetProperty("totalPages").GetInt32());
        var product = Assert.Single(json.RootElement.GetProperty("data").EnumerateArray());
        Assert.Equal("Alpha Deluxe", product.GetProperty("name").GetString());
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Customer", HttpStatusCode.Forbidden)]
    public async Task CreateProduct_RequiresAdministrator(string? role, HttpStatusCode expected)
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedCatalogAsync(api, []);

        using var response = await api.CreateClient(role).PostAsJsonAsync(
            "/api/v1/products",
            ValidProductRequest("Protected Product"));

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(0, await api.WithDbAsync(db => db.Products.CountAsync()));
    }

    [Fact]
    public async Task CreateProduct_AsAdministrator_CreatesMediaAndLocation()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedCatalogAsync(api, []);

        using var response = await api.CreateClient("Administrator").PostAsJsonAsync(
            "/api/v1/products",
            ValidProductRequest("Created Product"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Matches(@"/api/v1/products/\d+$", response.Headers.Location?.ToString() ?? string.Empty);
        var created = await api.WithDbAsync(db => db.Products.Include(item => item.ProductMedia).SingleAsync());
        Assert.Equal("Created Product", created.Name);
        Assert.Single(created.ProductMedia);
        Assert.Equal("https://cdn.example.com/cover.png", created.ProductMedia.Single().MediaUrl);
    }

    [Fact]
    public async Task CreateProduct_NegativePrice_ReturnsProblemDetailsAndDoesNotPersist()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedCatalogAsync(api, []);
        var request = ValidProductRequest("Invalid Product") with { BasePrice = -1 };

        using var response = await api.CreateClient("Administrator").PostAsJsonAsync("/api/v1/products", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, await api.WithDbAsync(db => db.Products.CountAsync()));
    }

    [Fact]
    public async Task DeleteProduct_WhenAlreadySold_ReturnsConflictAndKeepsProduct()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedCatalogAsync(api, [Product(1, "Purchased Game", 100_000)]);
        await api.WithDbAsync(async db =>
        {
            db.Orders.Add(new Order
            {
                Id = 1,
                UserId = 1,
                TotalAmount = 100_000,
                Status = "Completed",
                OrderDate = DateTime.UtcNow
            });
            db.OrderItems.Add(new OrderItem
            {
                Id = 1,
                OrderId = 1,
                ProductId = 1,
                PriceAtPurchase = 100_000
            });
            await db.SaveChangesAsync();
        });

        using var response = await api.CreateClient("Administrator").DeleteAsync("/api/v1/products/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(await api.WithDbAsync(db => db.Products.AnyAsync(item => item.Id == 1)));
    }

    [Fact]
    public async Task CreateCoupon_NormalizesCodeAndReturnsCreated()
    {
        await using var api = await ApiTestHost.StartAsync();

        using var response = await api.CreateClient("Administrator").PostAsJsonAsync(
            "/api/v1/coupons",
            CouponRequest("summer20"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Matches(@"/api/v1/coupons/\d+$", response.Headers.Location?.ToString() ?? string.Empty);
        Assert.Equal("SUMMER20", await api.WithDbAsync(db => db.Coupons.Select(item => item.Code).SingleAsync()));
    }

    [Fact]
    public async Task CreateCoupon_DuplicateCode_ReturnsConflict()
    {
        await using var api = await ApiTestHost.StartAsync();
        await api.WithDbAsync(async db =>
        {
            db.Coupons.Add(new Coupon
            {
                Id = 1,
                Code = "SUMMER20",
                DiscountPercent = 20,
                MaxUsage = 10,
                ExpiryDate = DateTime.UtcNow.AddDays(1),
                IsActive = true
            });
            await db.SaveChangesAsync();
        });

        using var response = await api.CreateClient("Administrator").PostAsJsonAsync(
            "/api/v1/coupons",
            CouponRequest("summer20"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await api.WithDbAsync(db => db.Coupons.CountAsync()));
    }

    [Fact]
    public async Task ValidateCoupon_ActiveCoupon_ReturnsDiscount()
    {
        await using var api = await ApiTestHost.StartAsync();
        await api.WithDbAsync(async db =>
        {
            db.Coupons.Add(new Coupon
            {
                Id = 7,
                Code = "ACTIVE20",
                DiscountPercent = 20,
                MaxUsage = 10,
                CurrentUsage = 2,
                ExpiryDate = DateTime.UtcNow.AddDays(1),
                IsActive = true
            });
            await db.SaveChangesAsync();
        });

        using var response = await api.CreateClient().PostAsJsonAsync(
            "/api/v1/coupon-validations",
            new { code = "active20" });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(20, json.RootElement.GetProperty("discountPercent").GetDecimal());
        Assert.Equal(7, json.RootElement.GetProperty("couponId").GetInt32());
    }

    [Theory]
    [InlineData(false, 0, 10, 1)]
    [InlineData(true, 10, 10, 1)]
    [InlineData(true, 0, 10, -1)]
    public async Task ValidateCoupon_UnavailableCoupon_ReturnsBadRequest(
        bool isActive,
        int currentUsage,
        int maxUsage,
        int expiryDayOffset)
    {
        await using var api = await ApiTestHost.StartAsync();
        await api.WithDbAsync(async db =>
        {
            db.Coupons.Add(new Coupon
            {
                Id = 1,
                Code = "UNAVAILABLE",
                DiscountPercent = 10,
                CurrentUsage = currentUsage,
                MaxUsage = maxUsage,
                ExpiryDate = DateTime.UtcNow.AddDays(expiryDayOffset),
                IsActive = isActive
            });
            await db.SaveChangesAsync();
        });

        using var response = await api.CreateClient().PostAsJsonAsync(
            "/api/v1/coupon-validations",
            new { code = "UNAVAILABLE" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PromotionProductPut_IsIdempotent()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedCatalogAsync(api, [Product(1, "Promo Game", 100_000)]);
        await api.WithDbAsync(async db =>
        {
            db.Promotions.Add(new Promotion
            {
                Id = 1,
                Name = "Launch Sale",
                Description = "Sale",
                DiscountPercent = 15,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(1)
            });
            await db.SaveChangesAsync();
        });
        using var client = api.CreateClient("Administrator");

        using var first = await client.PutAsync("/api/v1/promotions/1/products/1", null);
        using var second = await client.PutAsync("/api/v1/promotions/1/products/1", null);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(1, await api.WithDbAsync(db => db.ProductPromotions.CountAsync()));
    }

    [Fact]
    public async Task PaymentConfigurationPut_UpsertsValues()
    {
        await using var api = await ApiTestHost.StartAsync();
        await api.WithDbAsync(async db =>
        {
            db.PaymentGatewayConfigs.Add(new PaymentGatewayConfig
            {
                Id = 1,
                GatewayName = "MoMo",
                ConfigKey = "PartnerCode",
                ConfigValue = "old",
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            });
            await db.SaveChangesAsync();
        });

        using var response = await api.CreateClient("Administrator").PutAsJsonAsync(
            "/api/v1/payment-configurations",
            new[]
            {
                new PaymentConfigDto { GatewayName = "MoMo", ConfigKey = "PartnerCode", ConfigValue = "new" },
                new PaymentConfigDto { GatewayName = "VNPay", ConfigKey = "TmnCode", ConfigValue = "tmn" }
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var configs = await api.WithDbAsync(db => db.PaymentGatewayConfigs.OrderBy(item => item.GatewayName).ToListAsync());
        Assert.Equal(2, configs.Count);
        Assert.Contains(configs, item => item.GatewayName == "MoMo" && item.ConfigValue == "new");
        Assert.Contains(configs, item => item.GatewayName == "VNPay" && item.ConfigValue == "tmn");
    }

    [Fact]
    public async Task Upload_WithoutMultipartContent_ReturnsProblemDetails()
    {
        await using var api = await ApiTestHost.StartAsync();

        using var response = await api.CreateClient("Administrator").PostAsJsonAsync(
            "/api/v1/media-files",
            new { file = "not-a-file" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static async Task SeedCatalogAsync(ApiTestHost api, IReadOnlyCollection<Product> products)
    {
        await api.WithDbAsync(async db =>
        {
            db.Roles.AddRange(
                new Role { Id = 1, RoleName = "Customer" },
                new Role { Id = 2, RoleName = "Administrator" });
            db.Users.Add(new User
            {
                Id = 1,
                Username = "developer",
                Email = "developer@example.com",
                PasswordHash = "hash",
                RoleId = 2,
                CreatedAt = DateTime.UtcNow
            });
            db.Products.AddRange(products);
            await db.SaveChangesAsync();
        });
    }

    private static Product Product(int id, string name, decimal price) => new()
    {
        Id = id,
        Name = name,
        Description = $"{name} description",
        DeveloperId = 1,
        Type = "Game",
        Pricing = "Paid",
        BasePrice = price,
        Status = "Active",
        ReleaseDate = DateTime.UtcNow.AddDays(-id),
        CreatedAt = DateTime.UtcNow.AddDays(-id)
    };

    private static ProductRequest ValidProductRequest(string name) => new(
        name,
        "Description",
        "Game",
        "Paid",
        150_000,
        "Active",
        DateTime.UtcNow,
        1,
        "https://cdn.example.com/cover.png");

    private static CreateCouponDto CouponRequest(string code) => new()
    {
        Code = code,
        DiscountPercent = 20,
        MaxUsage = 10,
        ExpiryDate = DateTime.UtcNow.AddDays(7),
        IsActive = true
    };

    private sealed record ProductRequest(
        string Name,
        string Description,
        string Type,
        string Pricing,
        decimal BasePrice,
        string Status,
        DateTime ReleaseDate,
        int DeveloperId,
        string ImageUrl);
}
