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
        public static void MapWalletEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var isV1 = EndpointRouteConventions.IsV1(apiPrefix);
            var group = app.MapGroup(isV1 ? $"{apiPrefix}/users/me" : $"{apiPrefix}/wallet");

            // Lấy danh sách giao dịch của user
            group.MapGet("/transactions", async (ClaimsPrincipal userClaim, GameStoreDbContext db) =>
            {
                var userId = userClaim.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var transactions = await db.Transactions
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.Id,
                        t.OrderId,
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

            // Nạp tiền vào ví (CHỈ DÙNG CHO DEV/TEST - Production cần tích hợp payment gateway)
            group.MapPost(isV1 ? "/wallet-deposits" : "/deposit", async (DepositDto dto, ClaimsPrincipal userClaim, GameStoreDbContext db, IWebHostEnvironment env) =>
            {
                // Chỉ cho phép deposit trực tiếp trong Development
                if (!env.IsDevelopment())
                    return Results.BadRequest(new { message = "Chức năng nạp tiền trực tiếp chỉ khả dụng trong môi trường phát triển." });

                if (dto.Amount <= 0) return Results.BadRequest("Số tiền phải lớn hơn 0");
                if (dto.Amount > 10_000_000) return Results.BadRequest("Số tiền tối đa mỗi lần nạp là 10,000,000 VNĐ");

                var userId = userClaim.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var user = await db.Users.FindAsync(userId);
                if (user == null) return Results.NotFound();

                // Nạp tiền
                user.WalletBalance += dto.Amount;

                // Ghi lại lịch sử giao dịch
                var transaction = new Transaction
                {
                    UserId = userId.Value,
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
            var adminTransactions = isV1
                ? app.MapGroup($"{apiPrefix}/transactions")
                : group;
            adminTransactions.MapGet(isV1 ? "/" : "/admin/all", async (GameStoreDbContext db, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);
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
