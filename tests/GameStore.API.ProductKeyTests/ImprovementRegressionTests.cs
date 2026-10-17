using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.API.Endpoints;
using GameStore.API.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameStore.API.ProductKeyTests;

[Trait("Category", "Improvement")]
public class ImprovementRegressionTests
{
    [Fact]
    public async Task RefreshToken_LockedUser_IsRejected()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedUserAsync(api, isLocked: true);
        await api.WithDbAsync(async db =>
        {
            db.RefreshTokens.Add(NewRefreshToken("locked-user-token"));
            await db.SaveChangesAsync();
        });

        using var response = await api.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new { refreshToken = "locked-user-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_RevokesExistingRefreshTokens()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedUserAsync(api, password: "old-password");
        await api.WithDbAsync(async db =>
        {
            db.RefreshTokens.Add(NewRefreshToken("existing-session"));
            await db.SaveChangesAsync();
        });

        using var response = await api.CreateClient("Customer").PostAsJsonAsync(
            "/api/v1/users/me/password-changes",
            new { oldPassword = "old-password", newPassword = "new-password" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await api.WithDbAsync(db => db.RefreshTokens.CountAsync()));
    }

    [Fact]
    public async Task ResetPassword_RevokesExistingRefreshTokens()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedUserAsync(api, password: "old-password");
        await api.WithDbAsync(async db =>
        {
            db.RefreshTokens.Add(NewRefreshToken("existing-session"));
            db.PasswordResetTokens.Add(new PasswordResetToken
            {
                Id = 1,
                UserId = 1,
                Token = "reset-token",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            });
            await db.SaveChangesAsync();
        });

        using var response = await api.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/reset-password",
            new { token = "reset-token", newPassword = "new-password" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, await api.WithDbAsync(db => db.RefreshTokens.CountAsync()));
    }

    [Fact]
    public async Task Register_PublicRequestCannotCreateAdministrator()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedRolesAsync(api);

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
    }

    [Fact]
    public async Task UpdateProduct_NegativePrice_IsRejectedWithoutMutation()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedCatalogAsync(api);

        using var response = await api.CreateClient("Administrator").PutAsJsonAsync(
            "/api/v1/products/1",
            new
            {
                name = "Game",
                description = "Updated",
                type = "Game",
                pricing = "Paid",
                basePrice = -1,
                status = "Active",
                releaseDate = DateTime.UtcNow,
                imageUrl = "",
                media = Array.Empty<object>()
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(100_000m, await api.WithDbAsync(
            db => db.Products.Where(item => item.Id == 1).Select(item => item.BasePrice).SingleAsync()));
    }

    [Fact]
    public async Task UpdateCoupon_InvalidDiscount_IsRejectedWithoutMutation()
    {
        await using var api = await ApiTestHost.StartAsync();
        await api.WithDbAsync(async db =>
        {
            db.Coupons.Add(new Coupon
            {
                Id = 1,
                Code = "VALID10",
                DiscountPercent = 10,
                MaxUsage = 10,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IsActive = true
            });
            await db.SaveChangesAsync();
        });

        using var response = await api.CreateClient("Administrator").PutAsJsonAsync(
            "/api/v1/coupons/1",
            new CreateCouponDto
            {
                Code = "VALID10",
                DiscountPercent = 150,
                MaxUsage = 10,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IsActive = true
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(10m, await api.WithDbAsync(
            db => db.Coupons.Where(item => item.Id == 1).Select(item => item.DiscountPercent).SingleAsync()));
    }

    [Fact]
    public async Task UpdateProductKey_MissingTargetProduct_ReturnsNotFound()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedCatalogAsync(api, addKey: true);

        using var response = await api.CreateClient("Administrator").PutAsJsonAsync(
            "/api/v1/product-keys/1",
            new UpdateProductKeyDto { ProductId = 404, KeyCode = "MOVED-KEY" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, await api.WithDbAsync(
            db => db.ProductKeys.Where(item => item.Id == 1).Select(item => item.ProductId).SingleAsync()));
    }

    [Fact]
    public async Task PatchOrder_UnknownStatus_IsRejectedWithoutMutation()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedCatalogAsync(api);
        await api.WithDbAsync(async db =>
        {
            db.Orders.Add(new Order
            {
                Id = 1,
                UserId = 1,
                TotalAmount = 100_000,
                Status = "Pending",
                OrderDate = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        using var response = await api.CreateClient("Administrator").PatchAsJsonAsync(
            "/api/v1/orders/1",
            new { status = "MadeUpStatus" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Pending", await api.WithDbAsync(
            db => db.Orders.Where(item => item.Id == 1).Select(item => item.Status).SingleAsync()));
    }

    [Fact]
    public async Task PatchSupportTicket_UnknownStatus_IsRejectedWithoutMutation()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedUserAsync(api);
        await api.WithDbAsync(async db =>
        {
            db.SupportTickets.Add(new SupportTicket
            {
                Id = 1,
                UserId = 1,
                Topic = "Payment",
                Email = "customer@example.com",
                Content = "Help",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        using var response = await api.CreateClient("Administrator").PatchAsJsonAsync(
            "/api/v1/support-tickets/1",
            new { status = "AnythingGoes" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Pending", await api.WithDbAsync(
            db => db.SupportTickets.Where(item => item.Id == 1).Select(item => item.Status).SingleAsync()));
    }

    [Fact]
    public async Task ProductReviews_HiddenComments_AreNotExposedPublicly()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedCatalogAsync(api);
        await api.WithDbAsync(async db =>
        {
            db.Reviews.Add(new Review
            {
                Id = 1,
                ProductId = 1,
                UserId = 1,
                Content = "Visible review",
                IsRecommended = true,
                CreatedAt = DateTime.UtcNow
            });
            db.ReviewComments.Add(new ReviewComment
            {
                Id = 1,
                ReviewId = 1,
                UserId = 1,
                Content = "Moderated secret",
                IsHidden = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        using var response = await api.CreateClient().GetAsync("/api/v1/products/1/reviews");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var review = Assert.Single(json.RootElement.EnumerateArray());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(review.GetProperty("replies").EnumerateArray());
    }

    [Fact]
    public async Task Checkout_DuplicateProductIds_RequireOneDistinctKeyPerItem()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedCatalogAsync(api, addKey: true);

        using var response = await api.CreateClient("Customer").PostAsJsonAsync(
            "/api/v1/checkouts",
            new { productIds = new[] { 1, 1 }, paymentMethod = "MockGateway" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await api.WithDbAsync(db => db.Orders.CountAsync()));
        Assert.Equal(0, await api.WithDbAsync(db => db.Licenses.CountAsync()));
        Assert.False(await api.WithDbAsync(
            db => db.ProductKeys.Where(item => item.Id == 1).Select(item => item.IsSold).SingleAsync()));
    }

    private static RefreshToken NewRefreshToken(string token) => new()
    {
        UserId = 1,
        Token = token,
        CreatedAt = DateTime.UtcNow,
        Expires = DateTime.UtcNow.AddDays(1)
    };

    private static Task SeedRolesAsync(ApiTestHost api) => api.WithDbAsync(async db =>
    {
        db.Roles.AddRange(
            new Role { Id = 1, RoleName = "Customer" },
            new Role { Id = 2, RoleName = "Administrator" });
        await db.SaveChangesAsync();
    });

    private static async Task SeedUserAsync(
        ApiTestHost api,
        string password = "password1",
        bool isLocked = false)
    {
        await SeedRolesAsync(api);
        await api.WithDbAsync(async db =>
        {
            db.Users.Add(new User
            {
                Id = 1,
                Username = "customer",
                Email = "customer@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                RoleId = 1,
                IsLocked = isLocked,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });
    }

    private static async Task SeedCatalogAsync(ApiTestHost api, bool addKey = false)
    {
        await SeedUserAsync(api);
        await api.WithDbAsync(async db =>
        {
            db.Products.Add(new Product
            {
                Id = 1,
                Name = "Game",
                Description = "Test game",
                DeveloperId = 1,
                Type = "Game",
                Pricing = "Paid",
                BasePrice = 100_000,
                Status = "Active",
                ReleaseDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            if (addKey)
            {
                db.ProductKeys.Add(new ProductKey
                {
                    Id = 1,
                    ProductId = 1,
                    KeyCode = "ONLY-KEY",
                    IsSold = false
                });
            }
            await db.SaveChangesAsync();
        });
    }
}
