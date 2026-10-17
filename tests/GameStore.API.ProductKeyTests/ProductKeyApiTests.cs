using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GameStore.API.Data;
using GameStore.API.Endpoints;
using GameStore.API.Entities;
using GameStore.API.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameStore.API.ProductKeyTests;

public class ProductKeyApiTests
{
    [Fact]
    public async Task GetKeys_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var api = await ProductKeyTestApi.StartAsync();

        using var response = await api.CreateClient().GetAsync("/api/keys/");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetKeys_WithNonAdministratorRole_ReturnsForbidden()
    {
        await using var api = await ProductKeyTestApi.StartAsync();

        using var response = await api.CreateClient("Customer").GetAsync("/api/keys/");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetKeys_PaginatesByProduct_AndKeepsKeysOfSameProductTogether()
    {
        await using var api = await ProductKeyTestApi.StartAsync();
        await api.SeedAsync(
            products:
            [
                Product(1, "Alpha"),
                Product(2, "Beta"),
                Product(3, "Gamma")
            ],
            keys:
            [
                ProductKey(1, 1, "ALPHA-001"),
                ProductKey(2, 1, "ALPHA-002"),
                ProductKey(3, 2, "BETA-001"),
                ProductKey(4, 3, "GAMMA-001")
            ]);

        var result = await api.CreateClient("Administrator")
            .GetFromJsonAsync<KeyListResponse>("/api/keys/?page=1&pageSize=2");

        Assert.NotNull(result);
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(["ALPHA-001", "ALPHA-002", "BETA-001"], result.Data.Select(k => k.KeyCode));
    }

    [Fact]
    public async Task GetKeys_SearchesByTrimmedProductNameOrKeyCode()
    {
        await using var api = await ProductKeyTestApi.StartAsync();
        await api.SeedAsync(
            products: [Product(1, "Alpha"), Product(2, "Beta")],
            keys: [ProductKey(1, 1, "FIRST-KEY"), ProductKey(2, 2, "SPECIAL-KEY")]);

        var byProduct = await api.CreateClient("Administrator")
            .GetFromJsonAsync<KeyListResponse>("/api/keys/?page=1&pageSize=10&search=%20Alpha%20");
        var byCode = await api.CreateClient("Administrator")
            .GetFromJsonAsync<KeyListResponse>("/api/keys/?page=1&pageSize=10&search=SPECIAL");

        Assert.NotNull(byProduct);
        Assert.Single(byProduct.Data);
        Assert.Equal("FIRST-KEY", byProduct.Data[0].KeyCode);
        Assert.NotNull(byCode);
        Assert.Single(byCode.Data);
        Assert.Equal("SPECIAL-KEY", byCode.Data[0].KeyCode);
    }

    [Fact]
    public async Task CreateKey_WhenProductDoesNotExist_ReturnsBadRequest()
    {
        await using var api = await ProductKeyTestApi.StartAsync();
        var request = new CreateProductKeyDto
        {
            ProductId = 999,
            KeyCode = "UNKNOWN-PRODUCT"
        };

        using var response = await api.CreateClient("Administrator")
            .PostAsJsonAsync("/api/keys/", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await api.CountKeysAsync());
    }

    [Fact]
    public async Task CreateKey_WithOrderId_ForcesKeyToSold()
    {
        await using var api = await ProductKeyTestApi.StartAsync();
        await api.SeedAsync(products: [Product(1, "Alpha")]);
        var request = new CreateProductKeyDto
        {
            ProductId = 1,
            KeyCode = "CREATED-KEY",
            IsSold = false,
            OrderId = 42
        };

        using var response = await api.CreateClient("Administrator")
            .PostAsJsonAsync("/api/keys/", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdId = await response.Content.ReadFromJsonAsync<int>();
        var created = await api.FindKeyAsync(createdId);
        Assert.NotNull(created);
        Assert.True(created.IsSold);
        Assert.Equal(42, created.OrderId);
        Assert.Equal("CREATED-KEY", created.KeyCode);
    }

    [Fact]
    public async Task UpdateKey_UpdatesFields_AndDerivesSoldStateFromOrderId()
    {
        await using var api = await ProductKeyTestApi.StartAsync();
        await api.SeedAsync(
            products: [Product(1, "Alpha"), Product(2, "Beta")],
            keys: [ProductKey(1, 1, "OLD-KEY")]);
        var request = new UpdateProductKeyDto
        {
            ProductId = 2,
            KeyCode = "UPDATED-KEY",
            OrderId = 77
        };

        using var response = await api.CreateClient("Administrator")
            .PutAsJsonAsync("/api/keys/1", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await api.FindKeyAsync(1);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.ProductId);
        Assert.Equal("UPDATED-KEY", updated.KeyCode);
        Assert.Equal(77, updated.OrderId);
        Assert.True(updated.IsSold);
    }

    [Fact]
    public async Task UpdateKey_WhenKeyDoesNotExist_ReturnsNotFound()
    {
        await using var api = await ProductKeyTestApi.StartAsync();
        var request = new UpdateProductKeyDto
        {
            ProductId = 1,
            KeyCode = "MISSING"
        };

        using var response = await api.CreateClient("Administrator")
            .PutAsJsonAsync("/api/keys/404", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteKey_WhenKeyIsSold_ReturnsConflictAndKeepsKey()
    {
        await using var api = await ProductKeyTestApi.StartAsync();
        await api.SeedAsync(
            products: [Product(1, "Alpha")],
            keys: [ProductKey(1, 1, "SOLD-KEY", isSold: true)]);

        using var response = await api.CreateClient("Administrator")
            .DeleteAsync("/api/keys/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(await api.FindKeyAsync(1));
    }

    [Fact]
    public async Task DeleteKey_WhenKeyIsAvailable_ReturnsNoContentAndRemovesKey()
    {
        await using var api = await ProductKeyTestApi.StartAsync();
        await api.SeedAsync(
            products: [Product(1, "Alpha")],
            keys: [ProductKey(1, 1, "AVAILABLE-KEY")]);

        using var response = await api.CreateClient("Administrator")
            .DeleteAsync("/api/keys/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await api.FindKeyAsync(1));
    }

    [Fact]
    public async Task V1_GetKeys_UsesCanonicalRouteAndDefaultPagination()
    {
        await using var api = await ProductKeyTestApi.StartAsync();
        await api.SeedAsync(
            products: [Product(1, "Alpha")],
            keys: [ProductKey(1, 1, "V1-KEY")]);

        using var response = await api.CreateClient("Administrator")
            .GetAsync("/api/v1/product-keys");
        var result = await response.Content.ReadFromJsonAsync<KeyListResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task V1_ErrorResponse_UsesProblemDetails()
    {
        await using var api = await ProductKeyTestApi.StartAsync();
        var request = new CreateProductKeyDto
        {
            ProductId = 999,
            KeyCode = "UNKNOWN-PRODUCT"
        };

        using var response = await api.CreateClient("Administrator")
            .PostAsJsonAsync("/api/v1/product-keys/", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task LegacyRoute_ReturnsDeprecationHeaders()
    {
        await using var api = await ProductKeyTestApi.StartAsync();

        using var response = await api.CreateClient("Administrator")
            .GetAsync("/api/keys/?page=1&pageSize=10");

        Assert.True(response.Headers.TryGetValues("Deprecation", out var values));
        Assert.Contains("true", values);
        Assert.True(response.Headers.Contains("Sunset"));
    }

    private static Product Product(int id, string name) => new()
    {
        Id = id,
        Name = name,
        Description = $"{name} test product",
        DeveloperId = 1,
        Type = "Game",
        Pricing = "Paid",
        BasePrice = 100_000,
        Status = "Published",
        ReleaseDate = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow
    };

    private static ProductKey ProductKey(
        int id,
        int productId,
        string keyCode,
        bool isSold = false,
        int? orderId = null) => new()
    {
        Id = id,
        ProductId = productId,
        KeyCode = keyCode,
        IsSold = isSold,
        OrderId = orderId
    };

    private sealed class KeyListResponse
    {
        public List<ProductKeyDto> Data { get; set; } = [];
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}

internal sealed class ProductKeyTestApi : IAsyncDisposable
{
    private readonly WebApplication _app;

    private ProductKeyTestApi(WebApplication app, Uri baseAddress)
    {
        _app = app;
        BaseAddress = baseAddress;
    }

    private Uri BaseAddress { get; }

    public static async Task<ProductKeyTestApi> StartAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });

        var databaseName = $"product-key-tests-{Guid.NewGuid():N}";

        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        builder.Services.AddDbContext<GameStoreDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        builder.Services
            .AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthenticationHandler.SchemeName,
                _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<V1ProblemDetailsFilter>();

        var app = builder.Build();
        app.UseMiddleware<LegacyApiDeprecationMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapProductKeyEndpoints();
        var v1 = app.MapGroup("/api/v1")
            .AddEndpointFilter<V1ProblemDetailsFilter>();
        v1.MapProductKeyEndpoints(string.Empty);

        await app.StartAsync();

        var addresses = app.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()?
            .Addresses;
        var address = Assert.Single(addresses ?? []);

        return new ProductKeyTestApi(app, new Uri(address));
    }

    public HttpClient CreateClient(string? role = null)
    {
        var client = new HttpClient { BaseAddress = BaseAddress };
        if (role is not null)
        {
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, role);
        }

        return client;
    }

    public async Task SeedAsync(
        IReadOnlyCollection<Product>? products = null,
        IReadOnlyCollection<ProductKey>? keys = null)
    {
        await WithDbContextAsync(async db =>
        {
            if (products is not null)
            {
                db.Products.AddRange(products);
            }

            if (keys is not null)
            {
                db.ProductKeys.AddRange(keys);
            }

            await db.SaveChangesAsync();
        });
    }

    public Task<int> CountKeysAsync() =>
        WithDbContextAsync(db => db.ProductKeys.CountAsync());

    public Task<ProductKey?> FindKeyAsync(int id) =>
        WithDbContextAsync(db => db.ProductKeys.AsNoTracking().SingleOrDefaultAsync(key => key.Id == id));

    private async Task WithDbContextAsync(Func<GameStoreDbContext, Task> action)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();
        await action(db);
    }

    private async Task<T> WithDbContextAsync<T>(Func<GameStoreDbContext, Task<T>> action)
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

internal sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ProductKeyTests";
    public const string RoleHeader = "X-Test-Role";
    public const string UserIdHeader = "X-Test-User-Id";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var role) || string.IsNullOrWhiteSpace(role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var userId = Request.Headers.TryGetValue(UserIdHeader, out var requestedUserId)
            && int.TryParse(requestedUserId, out var parsedUserId)
                ? parsedUserId
                : 1;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "api-test-user"),
            new Claim(ClaimTypes.Role, role.ToString())
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
