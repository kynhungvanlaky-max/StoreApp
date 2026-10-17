using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameStore.API.Endpoints
{
    public class CreateSupportTicketDto
    {
        public string Topic { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? OrderId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class UpdateTicketStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public static class SupportEndpoints
    {
        public static void MapSupportEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var isV1 = EndpointRouteConventions.IsV1(apiPrefix);
            var group = app.MapGroup(isV1 ? $"{apiPrefix}/support-tickets" : $"{apiPrefix}/support");
            var currentUserGroup = isV1
                ? app.MapGroup($"{apiPrefix}/users/me")
                : group;

            // Lấy danh sách ticket của user hiện tại
            currentUserGroup.MapGet(isV1 ? "/support-tickets" : "/my-tickets", async (HttpContext httpContext, GameStoreDbContext db) =>
            {
                var userId = httpContext.User.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var tickets = await db.SupportTickets
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.Id,
                        t.Topic,
                        t.Email,
                        t.OrderId,
                        t.Content,
                        t.Status,
                        t.CreatedAt,
                        t.UpdatedAt
                    })
                    .ToListAsync();

                return Results.Ok(tickets);
            });

            // Gửi yêu cầu hỗ trợ mới
            group.MapPost("/", async (CreateSupportTicketDto dto, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var userId = httpContext.User.GetUserId();
                if (userId == null) return Results.Unauthorized();

                if (string.IsNullOrWhiteSpace(dto.Topic))
                    return Results.BadRequest(new { message = "Chủ đề không được để trống." });
                if (string.IsNullOrWhiteSpace(dto.Content))
                    return Results.BadRequest(new { message = "Nội dung không được để trống." });
                if (string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains("@"))
                    return Results.BadRequest(new { message = "Email không hợp lệ." });

                var ticket = new SupportTicket
                {
                    UserId = userId.Value,
                    Topic = dto.Topic,
                    Email = dto.Email,
                    OrderId = dto.OrderId,
                    Content = dto.Content,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                db.SupportTickets.Add(ticket);
                await db.SaveChangesAsync();

                return isV1
                    ? Results.Created($"{EndpointRouteConventions.PublicPrefix(apiPrefix)}/support-tickets/{ticket.Id}", new { ticket.Id })
                    : Results.Ok(new { message = "Yêu cầu đã được gửi thành công!" });
            });

            // Admin lấy toàn bộ yêu cầu (phân trang)
            group.MapGet(isV1 ? "/" : "/admin/all", async (GameStoreDbContext db, int page = 1, int pageSize = 10) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);
                var query = db.SupportTickets
                    .Include(t => t.User)
                    .OrderByDescending(t => t.CreatedAt);

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var tickets = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        t.Id,
                        Username = t.User.Username,
                        t.Topic,
                        t.Email,
                        t.OrderId,
                        t.Content,
                        t.Status,
                        t.CreatedAt,
                        t.UpdatedAt,
                        t.AdminReply,
                        t.RepliedAt
                    })
                    .ToListAsync();

                return Results.Ok(new
                {
                    Data = tickets,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));

            if (!isV1)
            {
                // Legacy admin commands.
                group.MapPut("/admin/{id:int}/status", async (int id, UpdateTicketStatusDto dto, HttpContext httpContext, GameStoreDbContext db) =>
                {
                    var ticket = await db.SupportTickets.FindAsync(id);
                    if (ticket == null) return Results.NotFound();

                    var normalizedStatus = NormalizeTicketStatus(dto.Status);
                    if (normalizedStatus == null)
                    {
                        return Results.BadRequest(new { message = "Trạng thái không hợp lệ." });
                    }

                    ticket.Status = normalizedStatus;
                    ticket.UpdatedAt = DateTime.UtcNow;

                    await db.SaveChangesAsync();

                    return Results.Ok(new { message = "Cập nhật trạng thái thành công" });
                }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));

                group.MapPut("/admin/{id:int}/reply", async (int id, AdminReplyDto dto, HttpContext httpContext, GameStoreDbContext db) =>
                {
                    var ticket = await db.SupportTickets.FindAsync(id);
                    if (ticket == null) return Results.NotFound();

                    ticket.AdminReply = dto.ReplyContent;
                    ticket.Status = "Resolved";
                    ticket.RepliedAt = DateTime.UtcNow;
                    ticket.UpdatedAt = DateTime.UtcNow;

                    await db.SaveChangesAsync();
                    return Results.Ok(new { message = "Gửi phản hồi thành công" });
                }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));
            }
            else
            {
                group.MapPatch("/{id:int}", async (int id, UpdateSupportTicketDto dto, GameStoreDbContext db) =>
                {
                    var ticket = await db.SupportTickets.FindAsync(id);
                    if (ticket == null) return Results.NotFound();

                    if (!string.IsNullOrWhiteSpace(dto.Status))
                    {
                        var normalizedStatus = NormalizeTicketStatus(dto.Status);
                        if (normalizedStatus == null)
                        {
                            return Results.BadRequest(new { message = "Trạng thái không hợp lệ." });
                        }
                        ticket.Status = normalizedStatus;
                    }
                    if (dto.AdminReply is not null)
                    {
                        ticket.AdminReply = dto.AdminReply;
                        ticket.RepliedAt = DateTime.UtcNow;
                        if (string.IsNullOrWhiteSpace(dto.Status))
                        {
                            ticket.Status = "Resolved";
                        }
                    }

                    ticket.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                    return Results.NoContent();
                }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));
            }
        }

        private static string? NormalizeTicketStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return null;
            return status.Trim() switch
            {
                "Pending" or "pending" => "Pending",
                "InProgress" or "inprogress" or "In Progress" => "InProgress",
                "Resolved" or "resolved" => "Resolved",
                "Closed" or "closed" => "Closed",
                _ => null
            };
        }
    }

    public class AdminReplyDto
    {
        public string ReplyContent { get; set; } = string.Empty;
    }

    public class UpdateSupportTicketDto
    {
        public string? Status { get; set; }
        public string? AdminReply { get; set; }
    }
}
