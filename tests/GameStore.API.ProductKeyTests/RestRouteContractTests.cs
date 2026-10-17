using System.Net;
using GameStore.API.Data;
using GameStore.API.Endpoints;
using GameStore.API.Entities;
using GameStore.API.Services;
using GameStore.API.Services.Payment;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameStore.API.ProductKeyTests;

public class RestRouteContractTests
{
    [Fact]
    public async Task V1_ExposesCanonicalResourceRoutesAndMethods()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddDbContext<GameStoreDbContext>(options =>
            options.UseInMemoryDatabase($"all-routes-{Guid.NewGuid():N}"));
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<VnPayService>();
        builder.Services.AddScoped<MoMoService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<GeminiService>();
        builder.Services.AddScoped<V1ProblemDetailsFilter>();
        builder.Services.AddProblemDetails();
        builder.Services.AddSignalR();
        builder.Services
            .AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName,
                _ => { });
        builder.Services.AddAuthorization();

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapOrderEndpoints();
        var v1 = app.MapGroup("/api/v1")
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

        await app.StartAsync();
        try
        {
            var routes = app.Services.GetRequiredService<EndpointDataSource>()
                .Endpoints
                .OfType<RouteEndpoint>()
                .SelectMany(endpoint => endpoint.Metadata
                    .GetMetadata<HttpMethodMetadata>()?
                    .HttpMethods
                    .Select(method => new
                    {
                        Method = method,
                        Route = Normalize(endpoint.RoutePattern.RawText)
                    }) ?? [])
                .ToHashSet();

            Assert.Contains(routes, route => route.Method == "PATCH" && route.Route == "/api/v1/users/{id:int}");
            Assert.Contains(routes, route => route.Method == "POST" && route.Route == "/api/v1/auth/register");
            Assert.Contains(routes, route => route.Method == "POST" && route.Route == "/api/v1/auth/login");
            Assert.Contains(routes, route => route.Method == "GET" && route.Route == "/api/v1/products");
            Assert.Contains(routes, route => route.Method == "POST" && route.Route == "/api/v1/products");
            Assert.Contains(routes, route => route.Method == "POST" && route.Route == "/api/v1/checkouts");
            Assert.Contains(routes, route => route.Method == "GET" && route.Route == "/api/v1/users/me/orders");
            Assert.Contains(routes, route => route.Method == "PUT" && route.Route == "/api/v1/users/me/wishlist-items/{productId:int}");
            Assert.Contains(routes, route => route.Method == "PATCH" && route.Route == "/api/v1/support-tickets/{id:int}");
            Assert.Contains(routes, route => route.Method == "PUT" && route.Route == "/api/v1/payment-configurations");
            Assert.Contains(routes, route => route.Method == "GET" && route.Route == "/api/v1/products/{productId:int}/reviews");
            Assert.Contains(routes, route => route.Method == "POST" && route.Route == "/api/v1/users/me/password-changes");
            Assert.Contains(routes, route => route.Method == "GET" && route.Route == "/api/v1/product-keys");
            Assert.Contains(routes, route => route.Method == "POST" && route.Route == "/api/v1/media-files");
            Assert.Contains(routes, route => route.Method == "POST" && route.Route == "/api/v1/assistant/chat");
            Assert.Contains(routes, route => route.Method == "GET" && route.Route == "/api/v1/news");
            Assert.Contains(routes, route => route.Method == "GET" && route.Route == "/api/v1/users/me/transactions");
            Assert.Contains(routes, route => route.Method == "PUT" && route.Route == "/api/v1/promotions/{id:int}/products/{productId:int}");
            Assert.Contains(routes, route => route.Method == "POST" && route.Route == "/api/v1/coupon-validations");
            Assert.Contains(routes, route => route.Method == "GET" && route.Route == "/api/v1/admin/dashboard");
            Assert.Contains(routes, route => route.Method == "GET" && route.Route == "/api/v1/audit-logs");
            Assert.Contains(routes, route => route.Method == "GET" && route.Route == "/api/v1/recommendations");
            Assert.Contains(routes, route => route.Method == "GET" && route.Route == "/api/integrations/vnpay/return");
            Assert.Contains(routes, route => route.Method == "POST" && route.Route == "/api/webhooks/momo");
            Assert.DoesNotContain(routes, route => route.Route.StartsWith("/api/v1", StringComparison.Ordinal)
                && route.Route.Contains("toggle-lock", StringComparison.Ordinal));
            Assert.DoesNotContain(routes, route => route.Route.StartsWith("/api/v1", StringComparison.Ordinal)
                && route.Route.Contains("vnpay-return", StringComparison.Ordinal));
            Assert.DoesNotContain(routes, route => route.Route == "/api/v1/auth/change-password");

            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
            var transactionType = db.Model.FindEntityType(typeof(Transaction));
            var uniqueGatewayIndex = transactionType?.GetIndexes().SingleOrDefault(index =>
                index.Properties.Select(property => property.Name)
                    .SequenceEqual([nameof(Transaction.PaymentGateway), nameof(Transaction.GatewayTransactionId)]));
            Assert.NotNull(uniqueGatewayIndex);
            Assert.True(uniqueGatewayIndex.IsUnique);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static string Normalize(string? route) =>
        string.IsNullOrEmpty(route) ? "/" : route.TrimEnd('/');
}
