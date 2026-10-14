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
        public static void MapSupportEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/support");

            // Lấy danh sách ticket của user hiện tại
            group.MapGet("/my-tickets", async (HttpContext httpContext, GameStoreDbContext db) =>
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

                return Results.Ok(new { message = "Yêu cầu đã được gửi thành công!" });
            });

            // Admin lấy toàn bộ yêu cầu (phân trang)
            var getAllSupportTicketsHandler = async (GameStoreDbContext db, int page = 1, int pageSize = 10) =>
            {
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
            };

            group.MapGet("/admin/all", getAllSupportTicketsHandler).RequireAuthorization(policy => policy.RequireRole("Administrator"));
            app.MapGet("/api/v1/support-tickets", getAllSupportTicketsHandler).RequireAuthorization(policy => policy.RequireRole("Administrator"));
            app.MapGet("/api/support-tickets", getAllSupportTicketsHandler).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // Admin cập nhật trạng thái
            group.MapPut("/admin/{id}/status", async (int id, UpdateTicketStatusDto dto, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var ticket = await db.SupportTickets.FindAsync(id);
                if (ticket == null) return Results.NotFound();

                ticket.Status = dto.Status;
                ticket.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Cập nhật trạng thái thành công" });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // Admin phản hồi yêu cầu hỗ trợ
            group.MapPut("/admin/{id}/reply", async (int id, AdminReplyDto dto, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var ticket = await db.SupportTickets.FindAsync(id);
                if (ticket == null) return Results.NotFound();

                ticket.AdminReply = dto.ReplyContent;
                ticket.Status = "Resolved"; // Tự động chuyển thành Resolved
                ticket.RepliedAt = DateTime.UtcNow;
                ticket.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Gửi phản hồi thành công" });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));
        }
    }

    public class AdminReplyDto
    {
        public string ReplyContent { get; set; } = string.Empty;
    }
}
