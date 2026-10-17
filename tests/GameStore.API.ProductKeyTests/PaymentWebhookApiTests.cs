using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using GameStore.API.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameStore.API.ProductKeyTests;

public class PaymentWebhookApiTests
{
    [Fact]
    public async Task MomoWebhook_RepeatedSuccessfulCallback_IsIdempotent()
    {
        await using var api = await ApiTestHost.StartAsync();
        await api.WithDbAsync(async db =>
        {
            db.Roles.Add(new Role { Id = 1, RoleName = "Customer" });
            db.Users.Add(new User
            {
                Id = 1,
                Username = "buyer",
                Email = "buyer@example.com",
                PasswordHash = "hash",
                RoleId = 1,
                CreatedAt = DateTime.UtcNow
            });
            db.Products.Add(new Product
            {
                Id = 1,
                Name = "Webhook Game",
                Description = "Webhook test",
                DeveloperId = 1,
                Type = "Game",
                Pricing = "Paid",
                BasePrice = 100_000,
                Status = "Active",
                ReleaseDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            db.Orders.Add(new Order
            {
                Id = 1,
                UserId = 1,
                TotalAmount = 100_000,
                Status = "Pending",
                OrderDate = DateTime.UtcNow
            });
            db.OrderItems.Add(new OrderItem
            {
                Id = 1,
                OrderId = 1,
                ProductId = 1,
                PriceAtPurchase = 100_000
            });
            db.ProductKeys.Add(new ProductKey
            {
                Id = 1,
                ProductId = 1,
                KeyCode = "WEBHOOK-KEY",
                IsSold = false
            });
            await db.SaveChangesAsync();
        });

        var request = MomoRequest();
        using var client = api.CreateClient();
        using var first = await client.PostAsJsonAsync("/api/webhooks/momo", request);
        using var repeated = await client.PostAsJsonAsync("/api/webhooks/momo", request);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, repeated.StatusCode);
        var state = await api.WithDbAsync(async db => new
        {
            OrderStatus = await db.Orders.Select(item => item.Status).SingleAsync(),
            Key = await db.ProductKeys.SingleAsync(),
            Transactions = await db.Transactions.ToListAsync(),
            LicenseCount = await db.Licenses.CountAsync()
        });
        Assert.Equal("Completed", state.OrderStatus);
        Assert.True(state.Key.IsSold);
        Assert.Equal(1, state.Key.OrderId);
        Assert.Single(state.Transactions);
        Assert.Equal("MOMO-TRANSACTION-1", state.Transactions.Single().GatewayTransactionId);
        Assert.Equal(1, state.LicenseCount);
    }

    private static object MomoRequest()
    {
        const string partnerCode = "MOMO";
        const string orderId = "1_123";
        const string requestId = "REQUEST-1";
        const long amount = 100_000;
        const string orderInfo = "Thanh toan game";
        const string orderType = "momo_wallet";
        const string transId = "MOMO-TRANSACTION-1";
        const string message = "Success";
        const string responseTime = "20260826120000";
        const string payType = "qr";
        const string extraData = "";
        const int resultCode = 0;

        var rawHash = "accessKey=test-access-key"
            + $"&amount={amount}"
            + $"&extraData={extraData}"
            + $"&message={message}"
            + $"&orderId={orderId}"
            + $"&orderInfo={orderInfo}"
            + $"&orderType={orderType}"
            + $"&partnerCode={partnerCode}"
            + $"&payType={payType}"
            + $"&requestId={requestId}"
            + $"&responseTime={responseTime}"
            + $"&resultCode={resultCode}"
            + $"&transId={transId}";
        var signature = Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes("test-secret-key"), Encoding.UTF8.GetBytes(rawHash)));

        return new
        {
            partnerCode,
            orderId,
            requestId,
            amount,
            orderInfo,
            orderType,
            transId,
            resultCode,
            message,
            payType,
            responseTime,
            extraData,
            signature
        };
    }
}
