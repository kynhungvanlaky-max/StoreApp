using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameStore.API.Endpoints
{
    public class DepositDto
    {
        public decimal Amount { get; set; }
    }

    public static class WalletEndpoints
    {
        public static void MapWalletEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/wallet");

            // Lấy danh sách giao dịch của user
            group.MapGet("/transactions", async (HttpContext httpContext, GameStoreDbContext db) =>
            {
                var userIdStr = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdStr, out int userId)) return Results.Unauthorized();

                var transactions = await db.Transactions
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.Id,
                        t.Amount,
                        t.TransactionType,
                        t.PaymentGateway,
                        t.GatewayTransactionId,
                        t.Status,
                        t.CreatedAt
                    })
                    .ToListAsync();

                return Results.Ok(transactions);
            });

            // Giả lập nạp tiền vào ví
            group.MapPost("/deposit", async (DepositDto dto, HttpContext httpContext, GameStoreDbContext db) =>
            {
                if (dto.Amount <= 0) return Results.BadRequest("Số tiền phải lớn hơn 0");

                var userIdStr = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdStr, out int userId)) return Results.Unauthorized();

                var user = await db.Users.FindAsync(userId);
                if (user == null) return Results.NotFound();

                // Nạp tiền
                user.WalletBalance += dto.Amount;

                // Ghi lại lịch sử giao dịch
                var transaction = new Transaction
                {
                    UserId = userId,
                    Amount = dto.Amount,
                    TransactionType = "Deposit",
                    PaymentGateway = "Internal", // Mock payment
                    GatewayTransactionId = Guid.NewGuid().ToString(),
                    Status = "Success",
                    CreatedAt = DateTime.UtcNow
                };

                db.Transactions.Add(transaction);
                await db.SaveChangesAsync();

                return Results.Ok(new 
                { 
                    message = "Nạp tiền thành công",
                    newBalance = user.WalletBalance
                });
            });

            // Admin lấy toàn bộ giao dịch
            group.MapGet("/admin/all", async (HttpContext httpContext, GameStoreDbContext db, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
            {
                var query = db.Transactions.Include(t => t.User).AsQueryable();

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var transactions = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        t.Id,
                        Username = t.User.Username,
                        t.Amount,
                        t.TransactionType,
                        t.PaymentGateway,
                        t.GatewayTransactionId,
                        t.Status,
                        t.CreatedAt
                    })
                    .ToListAsync();

                return Results.Ok(new
                {
                    Data = transactions,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));
        }
    }
}
