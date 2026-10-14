using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace GameStore.API.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/users");

            group.MapGet("/", async (GameStoreDbContext db, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null) =>
            {
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

            group.MapPut("/{id}/toggle-lock", async (int id, GameStoreDbContext db) =>
            {
                var user = await db.Users.FindAsync(id);
                if (user == null) return Results.NotFound();

                user.IsLocked = !user.IsLocked;
                await db.SaveChangesAsync();

                return Results.Ok(new { message = user.IsLocked ? "Đã khoá tài khoản." : "Đã mở khoá tài khoản.", isLocked = user.IsLocked });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            group.MapPut("/{id}", async (int id, AdminUpdateUserDto dto, GameStoreDbContext db) =>
            {
                var user = await db.Users.FindAsync(id);
                if (user == null) return Results.NotFound();

                if (!string.IsNullOrEmpty(dto.Username)) user.Username = dto.Username;
                if (!string.IsNullOrEmpty(dto.Email)) user.Email = dto.Email;
                if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl;

                await db.SaveChangesAsync();
                return Results.Ok(new { message = "Cập nhật thành công!" });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            group.MapPut("/{id}/role", async (int id, ChangeRoleDto dto, GameStoreDbContext db) =>
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
            group.MapGet("/profile", async (GameStoreDbContext db, System.Security.Claims.ClaimsPrincipal userClaim) =>
            {
                var userIdStr = userClaim.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? 
                                userClaim.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
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

            // Cập nhật thông tin cá nhân (Tên, Avatar)
            group.MapPut("/profile", async (UpdateProfileDto dto, GameStoreDbContext db, System.Security.Claims.ClaimsPrincipal userClaim) =>
            {
                var userId = userClaim.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (user == null) return Results.NotFound();

                if (!string.IsNullOrEmpty(dto.Username)) user.Username = dto.Username;
                if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl; // Allow null to keep current

                await db.SaveChangesAsync();
                return Results.Ok(new { message = "Cập nhật thành công!" });
            }).RequireAuthorization();

            // Đổi mật khẩu
            group.MapPost("/change-password", async (ChangePasswordDto dto, GameStoreDbContext db, System.Security.Claims.ClaimsPrincipal userClaim) =>
            {
                var userId = userClaim.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (user == null) return Results.NotFound();

                if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
                    return Results.BadRequest("Mật khẩu cũ không chính xác.");

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Đổi mật khẩu thành công!" });
            }).RequireAuthorization();

            group.MapGet("/{id}", async (int id, GameStoreDbContext db) =>
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
                    AvatarUrl = u.AvatarUrl
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
    }

    public class ChangeRoleDto
    {
        public string RoleName { get; set; } = string.Empty;
    }
}
