using System.Net;
using System.Net.Http.Json;
using GameStore.API.Endpoints;
using GameStore.API.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameStore.API.ProductKeyTests;

public class AuthAndUserApiTests
{
    [Theory]
    [InlineData("ab", "valid@example.com", "secret1")]
    [InlineData("valid-user", "invalid-email", "secret1")]
    [InlineData("valid-user", "valid@example.com", "123")]
    public async Task Register_InvalidInput_ReturnsProblemDetails(
        string username,
        string email,
        string password)
    {
        await using var api = await ApiTestHost.StartAsync();

        using var response = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new
        {
            username,
            email,
            password,
            secretCode = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, await api.WithDbAsync(db => db.Users.CountAsync()));
    }

    [Fact]
    public async Task Register_ValidCustomer_CreatesUserAndReturnsLocation()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedRolesAsync(api);

        using var response = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Username = "new-customer",
            Email = "new@example.com",
            Password = "secret1"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var user = await api.WithDbAsync(db => db.Users.Include(item => item.Role).SingleAsync());
        Assert.Equal("Customer", user.Role.RoleName);
        Assert.True(BCrypt.Net.BCrypt.Verify("secret1", user.PasswordHash));
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokensAndPersistsRefreshToken()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedUserAsync(api, password: "correct-password");

        using var response = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "customer@example.com",
            Password = "correct-password"
        });
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.Equal("Customer", body.Role);
        Assert.Equal(1, await api.WithDbAsync(db => db.RefreshTokens.CountAsync()));
    }

    [Fact]
    public async Task Login_LockedUser_ReturnsBadRequest()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedUserAsync(api, password: "correct-password", isLocked: true);

        using var response = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "customer@example.com",
            Password = "correct-password"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0, await api.WithDbAsync(db => db.RefreshTokens.CountAsync()));
    }

    [Fact]
    public async Task Refresh_ValidToken_RotatesRefreshToken()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedUserAsync(api);
        await api.WithDbAsync(async db =>
        {
            db.RefreshTokens.Add(new RefreshToken
            {
                Id = 1,
                UserId = 1,
                Token = "old-refresh-token",
                CreatedAt = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddHours(1)
            });
            await db.SaveChangesAsync();
        });

        using var response = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest
        {
            RefreshToken = "old-refresh-token"
        });
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.NotEqual("old-refresh-token", body.RefreshToken);
        var tokens = await api.WithDbAsync(db => db.RefreshTokens.Select(item => item.Token).ToListAsync());
        Assert.DoesNotContain("old-refresh-token", tokens);
        Assert.Contains(body.RefreshToken, tokens);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ChangesPasswordAndConsumesToken()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedUserAsync(api, password: "old-password");
        await api.WithDbAsync(async db =>
        {
            db.PasswordResetTokens.Add(new PasswordResetToken
            {
                Id = 1,
                UserId = 1,
                Token = "valid-reset-token",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            });
            await db.SaveChangesAsync();
        });

        using var response = await api.CreateClient().PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest
        {
            Token = "valid-reset-token",
            NewPassword = "new-password"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var state = await api.WithDbAsync(async db => new
        {
            User = await db.Users.SingleAsync(),
            Token = await db.PasswordResetTokens.SingleAsync()
        });
        Assert.True(BCrypt.Net.BCrypt.Verify("new-password", state.User.PasswordHash));
        Assert.True(state.Token.IsUsed);
    }

    [Fact]
    public async Task CurrentUserProfile_UsesAuthenticatedUserId()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedTwoUsersAsync(api);

        using var response = await api.CreateClient("Customer", userId: 2).GetAsync("/api/v1/users/me");
        var profile = await response.Content.ReadFromJsonAsync<UserDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(profile);
        Assert.Equal(2, profile.Id);
        Assert.Equal("second@example.com", profile.Email);
    }

    [Fact]
    public async Task ChangePassword_ValidOldPassword_ReturnsNoContentAndUpdatesHash()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedUserAsync(api, password: "old-password");

        using var response = await api.CreateClient("Customer").PostAsJsonAsync(
            "/api/v1/users/me/password-changes",
            new { oldPassword = "old-password", newPassword = "new-password" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var passwordHash = await api.WithDbAsync(db => db.Users.Select(item => item.PasswordHash).SingleAsync());
        Assert.True(BCrypt.Net.BCrypt.Verify("new-password", passwordHash));
    }

    [Fact]
    public async Task AdminPatchUser_SetsExplicitLockState()
    {
        await using var api = await ApiTestHost.StartAsync();
        await SeedUserAsync(api);

        using var response = await api.CreateClient("Administrator").PatchAsJsonAsync(
            "/api/v1/users/1",
            new { isLocked = true });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(await api.WithDbAsync(db => db.Users.Select(item => item.IsLocked).SingleAsync()));
    }

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
                CreatedAt = DateTime.UtcNow,
                IsLocked = isLocked
            });
            await db.SaveChangesAsync();
        });
    }

    private static async Task SeedTwoUsersAsync(ApiTestHost api)
    {
        await SeedRolesAsync(api);
        await api.WithDbAsync(async db =>
        {
            db.Users.AddRange(
                new User
                {
                    Id = 1,
                    Username = "first",
                    Email = "first@example.com",
                    PasswordHash = "hash",
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
            await db.SaveChangesAsync();
        });
    }
}
