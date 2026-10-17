using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Backend.Controllers;
using Backend.HttpHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Xunit;

namespace GameStore.API.ProductKeyTests;

public class AdminBridgeTests
{
    [Fact]
    public async Task Bridge_WithoutToken_RedirectsToLoginWithoutCallingApi()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("API must not be called."));
        var controller = CreateController(handler, out _);

        var result = Assert.IsType<RedirectToActionResult>(await controller.Bridge(string.Empty));

        Assert.Equal("Login", result.ActionName);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Bridge_WithExpiredToken_RedirectsToLoginAndDoesNotCreateCookies()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var controller = CreateController(handler, out var context);

        var result = Assert.IsType<RedirectToActionResult>(await controller.Bridge("expired-token"));

        Assert.Equal("Login", result.ActionName);
        Assert.False(context.Response.Headers.ContainsKey("Set-Cookie"));
    }

    [Fact]
    public async Task Bridge_WithCustomerToken_RejectsAccessAndDoesNotCreateCookies()
    {
        var handler = JsonHandler("""
            {"username":"customer","email":"customer@example.com","roleName":"Customer"}
            """);
        var controller = CreateController(handler, out var context);

        var result = Assert.IsType<RedirectToActionResult>(await controller.Bridge("customer-token"));

        Assert.Equal("Login", result.ActionName);
        Assert.False(context.Response.Headers.ContainsKey("Set-Cookie"));
    }

    [Fact]
    public async Task Bridge_WithMalformedProfileResponse_FailsClosed()
    {
        var handler = JsonHandler("not-json");
        var controller = CreateController(handler, out var context);

        var result = Assert.IsType<RedirectToActionResult>(await controller.Bridge("otherwise-valid-token"));

        Assert.Equal("Login", result.ActionName);
        Assert.False(context.Response.Headers.ContainsKey("Set-Cookie"));
    }

    [Fact]
    public async Task Bridge_WithAdministratorToken_CreatesHttpOnlySessionAndRedirectsHome()
    {
        var handler = JsonHandler("""
            {"username":"admin","email":"admin@example.com","roleName":"Administrator"}
            """);
        var controller = CreateController(handler, out var context);

        var result = Assert.IsType<RedirectToActionResult>(await controller.Bridge("admin-token"));

        Assert.Equal("Index", result.ActionName);
        Assert.Equal("Home", result.ControllerName);
        Assert.Equal("Bearer", handler.Authorization?.Scheme);
        Assert.Equal("admin-token", handler.Authorization?.Parameter);

        var cookies = context.Response.Headers.SetCookie.Select(cookie => cookie!).ToArray();
        Assert.Equal(3, cookies.Length);
        Assert.Contains(cookies, cookie =>
            cookie.StartsWith("access_token=admin-token;", StringComparison.Ordinal) &&
            cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        Assert.All(cookies, cookie => Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TokenHandler_DoesNotOverwriteExplicitBridgeAuthorization()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = "access_token=stale-cookie-token";
        var capture = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var tokenHandler = new TokenHandler(new HttpContextAccessor { HttpContext = context })
        {
            InnerHandler = capture
        };
        using var client = new HttpClient(tokenHandler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/v1/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "submitted-token");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("submitted-token", capture.Authorization?.Parameter);
    }

    private static StubHandler JsonHandler(string json) =>
        new(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

    private static AuthController CreateController(StubHandler handler, out DefaultHttpContext context)
    {
        context = new DefaultHttpContext();
        var controller = new AuthController(new StubHttpClientFactory(handler))
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, new MemoryTempDataProvider())
        };
        return controller;
    }

    private sealed class StubHttpClientFactory(StubHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("http://api.local/")
        };
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public AuthenticationHeaderValue? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Authorization = request.Headers.Authorization;
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        private IDictionary<string, object> _values = new Dictionary<string, object>();

        public IDictionary<string, object> LoadTempData(HttpContext context) => _values;

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _values = new Dictionary<string, object>(values);
        }
    }
}
