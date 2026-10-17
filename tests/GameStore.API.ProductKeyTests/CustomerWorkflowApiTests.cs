using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GameStore.API.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameStore.API.ProductKeyTests;

public class CustomerWorkflowApiTests
{
    [Fact]
    public async Task WishlistPut_IsIdempotent_AndDeleteRemovesItem()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedStoreAsync(api);
        using var client = api.CreateClient("Customer");

        using var first = await client.PutAsync("/api/v1/users/me/wishlist-items/1", null);
        using var second = await client.PutAsync("/api/v1/users/me/wishlist-items/1", null);
        using var stateResponse = await client.GetAsync("/api/v1/users/me/wishlist-items/1");
        using var state = JsonDocument.Parse(await stateResponse.Content.ReadAsStringAsync());
        using var deleted = await client.DeleteAsync("/api/v1/users/me/wishlist-items/1");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.True(state.RootElement.GetProperty("isWishlisted").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(0, await api.WithDbAsync(db => db.Wishlists.CountAsync()));
    }

    [Fact]
    public async Task WishlistPut_MissingProduct_ReturnsNotFoundAndDoesNotPersist()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedStoreAsync(api);

        using var response = await api.CreateClient("Customer")
            .PutAsync("/api/v1/users/me/wishlist-items/404", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, await api.WithDbAsync(db => db.Wishlists.CountAsync()));
    }

    [Fact]
    public async Task SupportTicket_CreateScopesToOwner_AndAdminCanResolve()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedStoreAsync(api);

        using var created = await api.CreateClient("Customer", userId: 2).PostAsJsonAsync(
            "/api/v1/support-tickets",
            new
            {
                topic = "Payment",
                email = "second@example.com",
                orderId = "ORDER-2",
                content = "Please check my payment"
            });
        using var ownerResponse = await api.CreateClient("Customer", userId: 2)
            .GetAsync("/api/v1/users/me/support-tickets");
        using var otherResponse = await api.CreateClient("Customer", userId: 1)
            .GetAsync("/api/v1/users/me/support-tickets");
        var ownerTickets = await ownerResponse.Content.ReadFromJsonAsync<List<SupportTicketView>>();
        var otherTickets = await otherResponse.Content.ReadFromJsonAsync<List<SupportTicketView>>();
        using var patched = await api.CreateClient("Administrator").PatchAsJsonAsync(
            "/api/v1/support-tickets/1",
            new { adminReply = "Resolved by admin" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Single(ownerTickets ?? []);
        Assert.Empty(otherTickets ?? []);
        Assert.Equal(HttpStatusCode.NoContent, patched.StatusCode);
        var ticket = await api.WithDbAsync(db => db.SupportTickets.SingleAsync());
        Assert.Equal("Resolved", ticket.Status);
        Assert.Equal("Resolved by admin", ticket.AdminReply);
    }

    [Fact]
    public async Task SupportTicket_InvalidEmail_ReturnsProblemDetails()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedStoreAsync(api);

        using var response = await api.CreateClient("Customer").PostAsJsonAsync(
            "/api/v1/support-tickets",
            new { topic = "Help", email = "invalid", content = "Need help" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, await api.WithDbAsync(db => db.SupportTickets.CountAsync()));
    }

    [Fact]
    public async Task WalletDeposit_ValidAmount_UpdatesBalanceAndCreatesTransaction()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedStoreAsync(api);

        using var response = await api.CreateClient("Customer").PostAsJsonAsync(
            "/api/v1/users/me/wallet-deposits",
            new { amount = 250_000m });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var state = await api.WithDbAsync(async db => new
        {
            Balance = await db.Users.Where(item => item.Id == 1).Select(item => item.WalletBalance).SingleAsync(),
            Transaction = await db.Transactions.SingleAsync()
        });
        Assert.Equal(350_000m, state.Balance);
        Assert.Equal("Deposit", state.Transaction.TransactionType);
        Assert.Equal(250_000m, state.Transaction.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000001)]
    public async Task WalletDeposit_InvalidAmount_DoesNotMutateBalance(decimal amount)
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedStoreAsync(api);

        using var response = await api.CreateClient("Customer").PostAsJsonAsync(
            "/api/v1/users/me/wallet-deposits",
            new { amount });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(100_000m, await api.WithDbAsync(
            db => db.Users.Where(item => item.Id == 1).Select(item => item.WalletBalance).SingleAsync()));
        Assert.Equal(0, await api.WithDbAsync(db => db.Transactions.CountAsync()));
    }

    [Fact]
    public async Task OrderDetail_NonOwnerCustomer_ReturnsForbidden()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedStoreAsync(api);
        await SeedCompletedOrderAsync(api, orderId: 1, userId: 1);

        using var response = await api.CreateClient("Customer", userId: 2).GetAsync("/api/v1/orders/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_MockGateway_CompletesOrderAndAssignsKeyOnce()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedStoreAsync(api, addAvailableKey: true);

        using var response = await api.CreateClient("Customer").PostAsJsonAsync(
            "/api/v1/checkouts",
            new { productIds = new[] { 1 }, paymentMethod = "MockGateway" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var state = await api.WithDbAsync(async db => new
        {
            Order = await db.Orders.SingleAsync(),
            Key = await db.ProductKeys.SingleAsync(),
            LicenseCount = await db.Licenses.CountAsync(),
            TransactionCount = await db.Transactions.CountAsync()
        });
        Assert.Equal("Completed", state.Order.Status);
        Assert.True(state.Key.IsSold);
        Assert.Equal(state.Order.Id, state.Key.OrderId);
        Assert.Equal(1, state.LicenseCount);
        Assert.Equal(1, state.TransactionCount);
    }

    [Fact]
    public async Task Checkout_OutOfStock_ReturnsBadRequestWithoutCreatingOrder()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedStoreAsync(api, addAvailableKey: false);

        using var response = await api.CreateClient("Customer").PostAsJsonAsync(
            "/api/v1/checkouts",
            new { productIds = new[] { 1 }, paymentMethod = "MockGateway" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await api.WithDbAsync(db => db.Orders.CountAsync()));
        Assert.Equal(0, await api.WithDbAsync(db => db.Transactions.CountAsync()));
    }

    [Fact]
    public async Task Review_CreateRequiresPurchase_ThenRejectsDuplicate()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedStoreAsync(api);
        using var client = api.CreateClient("Customer");
        var request = new { content = "Great game", isRecommended = true };

        using var beforePurchase = await client.PostAsJsonAsync("/api/v1/products/1/reviews", request);
        await SeedCompletedOrderAsync(api, orderId: 1, userId: 1);
        using var created = await client.PostAsJsonAsync("/api/v1/products/1/reviews", request);
        using var duplicate = await client.PostAsJsonAsync("/api/v1/products/1/reviews", request);

        Assert.Equal(HttpStatusCode.Forbidden, beforePurchase.StatusCode);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(1, await api.WithDbAsync(db => db.Reviews.CountAsync()));
    }

    [Fact]
    public async Task ReviewPatch_NonOwnerForbidden_OwnerCanUpdate()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedStoreAsync(api);
        await api.WithDbAsync(async db =>
        {
            db.Reviews.Add(new Review
            {
                Id = 1,
                ProductId = 1,
                UserId = 1,
                Content = "Original",
                IsRecommended = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        using var forbidden = await api.CreateClient("Customer", userId: 2).PatchAsJsonAsync(
            "/api/v1/reviews/1",
            new { content = "Tampered" });
        using var updated = await api.CreateClient("Customer", userId: 1).PatchAsJsonAsync(
            "/api/v1/reviews/1",
            new { content = "Updated", isRecommended = false });

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        var review = await api.WithDbAsync(db => db.Reviews.SingleAsync());
        Assert.Equal("Updated", review.Content);
        Assert.False(review.IsRecommended);
    }

    [Fact]
    public async Task News_AdminCanCreateAndPublicCanRead()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedStoreAsync(api);

        using var created = await api.CreateClient("Administrator").PostAsJsonAsync(
            "/api/v1/news",
            new { title = "Release", summary = "Summary", content = "Content", coverImageUrl = "cover.png" });
        using var read = await api.CreateClient().GetAsync("/api/v1/news/1");

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        using var json = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
        Assert.Equal("Release", json.RootElement.GetProperty("title").GetString());
    }

    private static async Task SeedStoreAsync(ApiTestHost api, bool addAvailableKey = false)
    {
        await api.WithDbAsync(async db =>
        {
            db.Roles.AddRange(
                new Role { Id = 1, RoleName = "Customer" },
                new Role { Id = 2, RoleName = "Administrator" });
            db.Users.AddRange(
                new User
                {
                    Id = 1,
                    Username = "first",
                    Email = "first@example.com",
                    PasswordHash = "hash",
                    WalletBalance = 100_000,
                    RoleId = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 2,
                    Username = "second",
                    Email = "second@example.com",
                    PasswordHash = "hash",
                    RoleId = 1,
                    CreatedAt = DateTime.UtcNow
                });
            db.Products.Add(new Product
            {
                Id = 1,
                Name = "Workflow Game",
                Description = "Workflow test product",
                DeveloperId = 1,
                Type = "Game",
                Pricing = "Paid",
                BasePrice = 100_000,
                Status = "Active",
                ReleaseDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            if (addAvailableKey)
            {
                db.ProductKeys.Add(new ProductKey
                {
                    Id = 1,
                    ProductId = 1,
                    KeyCode = "WORKFLOW-KEY",
                    IsSold = false
                });
            }
            await db.SaveChangesAsync();
        });
    }

    private static Task SeedCompletedOrderAsync(ApiTestHost api, int orderId, int userId) =>
        api.WithDbAsync(async db =>
        {
            db.Orders.Add(new Order
            {
                Id = orderId,
                UserId = userId,
                TotalAmount = 100_000,
                Status = "Completed",
                OrderDate = DateTime.UtcNow
            });
            db.OrderItems.Add(new OrderItem
            {
                Id = orderId,
                OrderId = orderId,
                ProductId = 1,
                PriceAtPurchase = 100_000
            });
            await db.SaveChangesAsync();
        });

    private sealed class SupportTicketView
    {
        public int Id { get; set; }
        public string Topic { get; set; } = string.Empty;
    }
}
