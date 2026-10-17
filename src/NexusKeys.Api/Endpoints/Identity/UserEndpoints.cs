using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GameStore.API.Interfaces;
using GameStore.API.Services;

namespace GameStore.API.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var isV1 = EndpointRouteConventions.IsV1(apiPrefix);
            var group = app.MapGroup($"{apiPrefix}/users");

            group.MapGet("/", async (GameStoreDbContext db, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);
                var query = db.Users.Include(u => u.Role).AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        Email = u.Email,
                        WalletBalance = u.WalletBalance,
                        RoleName = u.Role.RoleName,
                        CreatedAt = u.CreatedAt,
                        AvatarUrl = u.AvatarUrl,
                        IsLocked = u.IsLocked
                    })
                    .ToListAsync();

                return Results.Ok(new
                {
                    Data = users,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            if (!isV1)
            {
                group.MapPut("/{id:int}/toggle-lock", async (int id, GameStoreDbContext db) =>
                {
                    var user = await db.Users.FindAsync(id);
                    if (user == null) return Results.NotFound();

                    user.IsLocked = !user.IsLocked;
                    await db.SaveChangesAsync();

                    return Results.Ok(new { message = user.IsLocked ? "Đã khoá tài khoản." : "Đã mở khoá tài khoản.", isLocked = user.IsLocked });
                }).RequireAuthorization(policy => policy.RequireRole("Administrator"));
            }

            group.MapMethods("/{id:int}", [isV1 ? HttpMethods.Patch : HttpMethods.Put], async (int id, AdminUpdateUserDto dto, GameStoreDbContext db) =>
            {
                var user = await db.Users.FindAsync(id);
                if (user == null) return Results.NotFound();

                if (!string.IsNullOrEmpty(dto.Username)) user.Username = dto.Username;
                if (!string.IsNullOrEmpty(dto.Email)) user.Email = dto.Email;
                if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl;
                if (dto.IsLocked.HasValue) user.IsLocked = dto.IsLocked.Value;

                await db.SaveChangesAsync();
                return isV1
                    ? Results.NoContent()
                    : Results.Ok(new { message = "Cập nhật thành công!" });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            group.MapPut("/{id:int}/role", async (int id, ChangeRoleDto dto, GameStoreDbContext db) =>
            {
                var user = await db.Users.FindAsync(id);
                if (user == null) return Results.NotFound();

                var role = await db.Set<Role>().FirstOrDefaultAsync(r => r.RoleName == dto.RoleName);
                if (role == null) return Results.BadRequest(new { message = "Role không tồn tại." });

                user.RoleId = role.Id;
                await db.SaveChangesAsync();

                return Results.Ok(new { message = $"Đã thay đổi quyền thành {role.RoleName}." });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // Lấy thông tin cá nhân của User đang đăng nhập
            var profileRoute = isV1 ? "/me" : "/profile";
            group.MapGet(profileRoute, async (GameStoreDbContext db, System.Security.Claims.ClaimsPrincipal userClaim) =>
            {
                var userId = userClaim.GetUserId();
                if (userId == null)
                    return Results.Unauthorized();

                var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return Results.NotFound();

                return Results.Ok(new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    WalletBalance = user.WalletBalance,
                    RoleName = user.Role.RoleName,
                    CreatedAt = user.CreatedAt,
                    AvatarUrl = user.AvatarUrl,
                    IsLocked = user.IsLocked
                });
            }).RequireAuthorization();

            group.MapMethods(profileRoute, [isV1 ? HttpMethods.Patch : HttpMethods.Put], async (UpdateProfileDto dto, GameStoreDbContext db, System.Security.Claims.ClaimsPrincipal userClaim) =>
            {
                var userId = userClaim.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (user == null) return Results.NotFound();

                if (!string.IsNullOrEmpty(dto.Username)) user.Username = dto.Username;
                if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl;

                await db.SaveChangesAsync();
                return isV1
                    ? Results.NoContent()
                    : Results.Ok(new { message = "Cập nhật thành công!" });
            }).RequireAuthorization();

            group.MapPost(isV1 ? "/me/password-changes" : "/change-password", async (
                ChangePasswordDto dto,
                GameStoreDbContext db,
                System.Security.Claims.ClaimsPrincipal userClaim,
                IConfiguration config,
                IServiceProvider sp,
                CancellationToken ct) =>
            {
                var userId = userClaim.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
                if (user == null) return Results.NotFound();

                if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                    return Results.BadRequest("Mật khẩu mới phải có ít nhất 6 ký tự.");

                try
                {
                    if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
                        return Results.BadRequest("Mật khẩu cũ không chính xác.");
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    return Results.BadRequest("Mật khẩu cũ không chính xác.");
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                await db.SaveChangesAsync(ct);

                var tokenService = sp.GetService<ITokenService>() ?? new TokenService(config, db);
                await tokenService.RevokeUserTokensAsync(user.Id, ct);

                return isV1
                    ? Results.NoContent()
                    : Results.Ok(new { message = "Đổi mật khẩu thành công!" });
            }).RequireAuthorization();

            group.MapGet("/{id:int}", async (int id, GameStoreDbContext db) =>
            {
                var u = await db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == id);
                if (u == null) return Results.NotFound();
                return Results.Ok(new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    WalletBalance = u.WalletBalance,
                    RoleName = u.Role.RoleName,
                    CreatedAt = u.CreatedAt,
                    AvatarUrl = u.AvatarUrl,
                    IsLocked = u.IsLocked
                });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));
        }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal WalletBalance { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsLocked { get; set; }
    }

    public class UpdateProfileDto
    {
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class AdminUpdateUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool? IsLocked { get; set; }
    }

    public class ChangeRoleDto
    {
        public string RoleName { get; set; } = string.Empty;
    }
}
