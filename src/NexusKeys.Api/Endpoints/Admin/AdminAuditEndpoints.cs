using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameStore.API.Endpoints
{
    public static class AdminAuditEndpoints
    {
        public static void MapAdminAuditEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var isV1 = EndpointRouteConventions.IsV1(apiPrefix);
            var route = isV1 ? $"{apiPrefix}/audit-logs" : $"{apiPrefix}/admin/audit";
            var group = app.MapGroup(route).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // GET audit logs with pagination
            group.MapGet("/", async (GameStoreDbContext db, int page = 1, int pageSize = 20) =>
            {
                var query = db.AuditLogs.OrderByDescending(a => a.CreatedAt);
                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var logs = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new
                    {
                        a.Id,
                        a.AdminId,
                        a.AdminUsername,
                        a.Action,
                        a.EntityType,
                        a.EntityId,
                        a.Details,
                        a.CreatedAt
                    })
                    .ToListAsync();

                return Results.Ok(new
                {
                    Data = logs,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                });
            });

            if (!isV1)
            {
                // Legacy only. V1 audit logs are written by server-side business operations.
                group.MapPost("/", async (CreateAuditLogDto dto, HttpContext httpContext, GameStoreDbContext db) =>
                {
                    var adminIdStr = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? httpContext.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                    if (string.IsNullOrEmpty(adminIdStr) || !int.TryParse(adminIdStr, out int adminId))
                        return Results.Unauthorized();

                    var admin = await db.Users.FindAsync(adminId);
                    if (admin == null) return Results.NotFound();

                    var log = new AuditLog
                    {
                        AdminId = adminId,
                        AdminUsername = admin.Username,
                        Action = dto.Action,
                        EntityType = dto.EntityType,
                        EntityId = dto.EntityId,
                        Details = dto.Details,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.AuditLogs.Add(log);
                    await db.SaveChangesAsync();

                    return Results.Created($"{EndpointRouteConventions.PublicPrefix(apiPrefix)}/admin/audit/{log.Id}", log.Id);
                });
            }
        }
    }

    public class CreateAuditLogDto
    {
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}
