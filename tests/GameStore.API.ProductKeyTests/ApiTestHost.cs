using System.Net;
using GameStore.API.Data;
using GameStore.API.Endpoints;
using GameStore.API.Infrastructure;
using GameStore.API.Services;
using GameStore.API.Services.Payment;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameStore.API.ProductKeyTests;

internal sealed class ApiTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private ApiTestHost(WebApplication app, Uri baseAddress)
    {
        _app = app;
        BaseAddress = baseAddress;
    }

    private Uri BaseAddress { get; }

    public static async Task<ApiTestHost> StartAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });

        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Configuration["Jwt:Key"] = "integration-test-key-with-at-least-thirty-two-characters";
        builder.Configuration["Jwt:Issuer"] = "GameStore.Tests";
        builder.Configuration["Jwt:Audience"] = "GameStore.Tests";
        builder.Configuration["AppSettings:FrontendBaseUrl"] = "http://localhost:5205";
        builder.Configuration["MoMo:AccessKey"] = "test-access-key";
        builder.Configuration["MoMo:SecretKey"] = "test-secret-key";

        var databaseName = $"api-tests-{Guid.NewGuid():N}";
        builder.Services.AddDbContext<GameStoreDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<VnPayService>();
        builder.Services.AddScoped<MoMoService>();
        builder.Services.AddSingleton<IEmailService, FakeEmailService>();
        builder.Services.AddScoped<V1ProblemDetailsFilter>();
        builder.Services.AddProblemDetails();
        builder.Services.AddSignalR();
        builder.Services
            .AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName,
                _ => { });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();

        // Map legacy order routes once because this is where provider callbacks
        // and the canonical integration adapters are registered.
        app.MapOrderEndpoints();

        var v1 = app.MapGroup("/api/v1")
            .AddEndpointFilter<V1ProblemDetailsFilter>();
        v1.MapAuthEndpoints(string.Empty);
        v1.MapProductEndpoints(string.Empty);
        v1.MapUserEndpoints(string.Empty);
        v1.MapOrderEndpoints(string.Empty, mapPaymentCallbacks: false);
        v1.MapProductKeyEndpoints(string.Empty);
        v1.MapUploadEndpoints(string.Empty);
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

        var addresses = app.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()?
            .Addresses;
        var address = Assert.Single(addresses ?? []);

        return new ApiTestHost(app, new Uri(address));
    }

    public HttpClient CreateClient(string? role = null, int userId = 1)
    {
        var client = new HttpClient { BaseAddress = BaseAddress };
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, role);
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId.ToString());
        }

        return client;
    }

    public async Task WithDbAsync(Func<GameStoreDbContext, Task> action)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        await action(db);
    }

    public async Task<T> WithDbAsync<T>(Func<GameStoreDbContext, Task<T>> action)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        return await action(db);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}

internal sealed class FakeEmailService : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string htmlBody) => Task.CompletedTask;
}
