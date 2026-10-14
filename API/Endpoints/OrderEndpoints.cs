using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using GameStore.API.Hubs;
using GameStore.API.Services.Payment;
using GameStore.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.API.Endpoints
{
    public class PaginationParams
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public static class OrderEndpoints
    {
        public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/orders");

            group.MapGet("/", async ([AsParameters] PaginationParams pagination, GameStoreDbContext db, ClaimsPrincipal userClaim) =>
            {
                var query = db.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new {
                        o.Id,
                        Username = o.User.Username,
                        o.TotalAmount,
                        o.Status,
                        o.OrderDate
                    });

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pagination.PageSize);

                var orders = await query
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                return Results.Ok(new {
                    Data = orders,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = pagination.Page,
                    PageSize = pagination.PageSize
                });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            group.MapGet("/{id}", async (int id, GameStoreDbContext db) =>
            {
                var order = await db.Orders
                    .Include(o => o.User)
                    .Include(o => o.ProductKeys).ThenInclude(k => k.Product)
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == id);
                
                if (order == null) return Results.NotFound();
                
                return Results.Ok(new {
                    Id = order.Id,
                    Username = order.User.Username,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    OrderDate = order.OrderDate,
                    Items = order.OrderItems.Select(oi => oi.Product.Name).ToList(),
                    Keys = order.ProductKeys.Select(k => new { k.Id, k.KeyCode, ProductName = k.Product.Name }).ToList()
                });
            });

            group.MapGet("/my-orders", async (GameStoreDbContext db, ClaimsPrincipal userClaim) =>
            {
                var userId = userClaim.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var orders = await db.Orders
                    .Include(o => o.ProductKeys).ThenInclude(k => k.Product)
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new {
                        Id = o.Id,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status,
                        OrderDate = o.OrderDate,
                        Items = o.OrderItems.Select(oi => oi.Product.Name).ToList(),
                        Keys = o.Status == "Completed" ? o.ProductKeys.Select(k => new { k.Id, k.KeyCode, ProductName = k.Product.Name }).ToList() : null
                    })
                    .ToListAsync();
                
                return Results.Ok(orders);
            });

            group.MapDelete("/{orderId}/keys/{keyId}", async (int orderId, int keyId, GameStoreDbContext db) =>
            {
                var order = await db.Orders.Include(o => o.ProductKeys).FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null) return Results.NotFound("Order not found.");

                var key = order.ProductKeys.FirstOrDefault(k => k.Id == keyId);
                if (key == null) return Results.NotFound("Key not found in this order.");

                key.OrderId = null;
                key.IsSold = false;
                db.ProductKeys.Update(key);
                await db.SaveChangesAsync();

                return Results.Ok();
            });

            // CHECKOUT: Tạo Order và Mock hoàn tất thanh toán thành công ngay lập tức
            group.MapPost("/checkout", async (CheckoutRequest req, HttpContext httpContext, GameStoreDbContext db, ClaimsPrincipal userClaim, VnPayService vnPayService, MoMoService moMoService, IEmailService emailService, IHubContext<NexusHub> hubContext) =>
            {
                if (req.ProductIds == null || !req.ProductIds.Any())
                    return Results.BadRequest("Giỏ hàng trống.");

                var userId = userClaim.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var uniqueIds = req.ProductIds.Distinct().ToList();
                var productsDict = await db.Products.Where(p => uniqueIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

                if (productsDict.Count != uniqueIds.Count)
                    return Results.BadRequest("Sản phẩm không hợp lệ.");

                decimal totalAmount = req.ProductIds.Sum(id => productsDict[id].BasePrice);
                Coupon? coupon = null;

                // Áp dụng Coupon nếu có
                if (!string.IsNullOrEmpty(req.CouponCode))
                {
                    var couponCode = req.CouponCode.ToUpper().Trim();
                    coupon = await db.Set<Coupon>().FirstOrDefaultAsync(c => c.Code == couponCode);
                    
                    if (coupon == null)
                        return Results.BadRequest(new { message = "Mã giảm giá không tồn tại." });
                    if (!coupon.IsActive)
                        return Results.BadRequest(new { message = "Mã giảm giá đã bị khóa." });
                    if (coupon.ExpiryDate <= DateTime.UtcNow)
                        return Results.BadRequest(new { message = "Mã giảm giá đã hết hạn." });
                    if (coupon.MaxUsage > 0 && coupon.CurrentUsage >= coupon.MaxUsage)
                        return Results.BadRequest(new { message = "Mã giảm giá đã hết lượt sử dụng." });

                    var discount = totalAmount * (coupon.DiscountPercent / 100m);
                    totalAmount -= discount;
                }

                var order = new Order
                {
                    UserId = userId.Value,
                    TotalAmount = totalAmount,
                    Status = "Pending",
                    OrderDate = DateTime.UtcNow
                };

                // Kiểm tra tồn kho: Nếu thiếu Key thì tự động sinh key mới để luôn đảm bảo thanh toán thành công
                foreach (var pId in req.ProductIds)
                {
                    var availableKeyCount = await db.ProductKeys.CountAsync(k => k.ProductId == pId && !k.IsSold);
                    if (availableKeyCount == 0)
                    {
                        var newKey = new ProductKey
                        {
                            ProductId = pId,
                            KeyCode = $"NEXUS-{Guid.NewGuid().ToString("N")[..4].ToUpper()}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
                            IsSold = false
                        };
                        db.ProductKeys.Add(newKey);
                    }
                }

                db.Orders.Add(order);
                await db.SaveChangesAsync();

                foreach (var pId in req.ProductIds)
                {
                    var product = productsDict[pId];
                    db.Set<OrderItem>().Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = product.Id,
                        PriceAtPurchase = product.BasePrice
                    });
                }
                await db.SaveChangesAsync();

                // MOCK PAYMENT: Hoàn tất đơn hàng thành công ngay lập tức cho mọi phương thức (VNPay, MoMo, MockGateway)
                var gateway = string.IsNullOrWhiteSpace(req.PaymentMethod) ? "MockGateway" : req.PaymentMethod;
                var gatewayTxnId = "MOCK-" + Guid.NewGuid().ToString("N")[..12].ToUpper();

                await CompleteOrder(order, db, userId.Value, req.ProductIds, gateway, gatewayTxnId, coupon, emailService, userClaim.FindFirst(ClaimTypes.Email)?.Value ?? "");

                try
                {
                    await hubContext.Clients.All.SendAsync("ReceiveNewOrder");
                }
                catch
                {
                    // Ignore hub notification errors
                }

                return Results.Ok(new { message = "Thanh toán thành công!", orderId = order.Id, paymentUrl = "" });
            });

            // VNPay Return Webhook
            app.MapGet("/api/orders/vnpay-return", async (HttpContext httpContext, GameStoreDbContext db, IHubContext<NexusHub> hubContext, VnPayService vnPayService, IEmailService emailService) =>
            {
                var query = httpContext.Request.Query;
                if (!vnPayService.ValidateSignature(query))
                    return Results.Redirect($"{GetFrontendUrl(httpContext)}/cart?error=invalid_signature");

                var vnp_ResponseCode = query["vnp_ResponseCode"].ToString();
                var vnp_TxnRef = query["vnp_TxnRef"].ToString();
                
                if (int.TryParse(vnp_TxnRef, out int orderId))
                {
                    var order = await db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == orderId);
                    if (order != null && order.Status == "Pending")
                    {
                        if (vnp_ResponseCode == "00")
                        {
                            var productIds = order.OrderItems.Select(oi => oi.ProductId).ToList();
                            await CompleteOrder(order, db, order.UserId, productIds, "VNPay", query["vnp_TransactionNo"].ToString(), null, emailService, "");

                            await hubContext.Clients.All.SendAsync("ReceiveNewOrder");
                            var frontend = httpContext.RequestServices.GetRequiredService<IConfiguration>()["AppSettings:FrontendBaseUrl"] ?? "http://localhost:5205";
                            return Results.Redirect($"{frontend}/checkout/success?orderId={orderId}");
                        }
                        else
                        {
                            order.Status = "Failed";
                            await db.SaveChangesAsync();
                            var frontend = httpContext.RequestServices.GetRequiredService<IConfiguration>()["AppSettings:FrontendBaseUrl"] ?? "http://localhost:5205";
                            return Results.Redirect($"{frontend}/cart?error=payment_failed");
                        }
                    }
                }
                return Results.Redirect($"{GetFrontendUrl(httpContext)}/cart?error=payment_failed");
            }).AllowAnonymous();

            // MoMo Notify Webhook
            app.MapPost("/api/orders/momo-notify", async ([FromBody] MoMoNotifyRequest req, GameStoreDbContext db, IHubContext<NexusHub> hubContext, MoMoService moMoService, IEmailService emailService) =>
            {
                if (!moMoService.ValidateSignature(req.partnerCode, req.orderId, req.requestId, req.amount.ToString(), req.orderInfo, req.orderType, req.transId, req.message, "", req.responseTime, req.resultCode.ToString(), req.payType, req.extraData, req.signature))
                    return Results.BadRequest(new { message = "Invalid signature" });

                var rawOrderId = req.orderId.Split('_')[0];
                if (int.TryParse(rawOrderId, out int dbOrderId))
                {
                    var order = await db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == dbOrderId);
                    if (order != null && order.Status == "Pending")
                    {
                        if (req.resultCode == 0)
                        {
                            var productIds = order.OrderItems.Select(oi => oi.ProductId).ToList();
                            await CompleteOrder(order, db, order.UserId, productIds, "MoMo", req.transId, null, emailService, "");

                            await hubContext.Clients.All.SendAsync("ReceiveNewOrder");
                            return Results.NoContent();
                        }
                        else
                        {
                            order.Status = "Failed";
                            await db.SaveChangesAsync();
                            return Results.NoContent();
                        }
                    }
                }
                return Results.BadRequest(new { message = "Order not found or not pending" });
            }).AllowAnonymous();

            group.MapGet("/library", async (GameStoreDbContext db, ClaimsPrincipal userClaim) =>
            {
                var userId = userClaim.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var libraryItems = await db.ProductKeys
                    .Include(k => k.Product).ThenInclude(p => p.ProductMedia)
                    .Include(k => k.Order)
                    .Where(k => k.Order != null && k.Order.UserId == userId && k.Order.Status == "Completed")
                    .Select(k => new
                    {
                        k.Product.Id,
                        k.Product.Name,
                        k.Product.DeveloperId,
                        ImageUrl = k.Product.ProductMedia.FirstOrDefault() != null ? k.Product.ProductMedia.FirstOrDefault()!.MediaUrl : "",
                        AcquiredDate = k.Order!.OrderDate,
                        KeyCode = k.KeyCode
                    })
                    .ToListAsync();

                return Results.Ok(libraryItems);
            });

            group.MapGet("/transactions", async (GameStoreDbContext db, ClaimsPrincipal userClaim) =>
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

            group.MapPut("/{id}/status", async (int id, UpdateOrderStatusDto dto, GameStoreDbContext db) =>
            {
                var order = await db.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.ProductKeys)
                    .FirstOrDefaultAsync(o => o.Id == id);
                if (order == null) return Results.NotFound();

                var oldStatus = order.Status;
                var newStatus = NormalizeOrderStatus(dto.Status);
                if (oldStatus == newStatus) return Results.Ok(new { message = "Trạng thái không đổi" });

                order.Status = newStatus;

                if (newStatus == "Completed")
                {
                    foreach (var item in order.OrderItems)
                    {
                        var existingLicense = await db.Set<License>().FirstOrDefaultAsync(l => l.UserId == order.UserId && l.ProductId == item.ProductId);
                        if (existingLicense == null)
                        {
                            db.Set<License>().Add(new License
                            {
                                UserId = order.UserId,
                                ProductId = item.ProductId,
                                Type = "Permanent",
                                ValidFrom = DateTime.UtcNow,
                                ValidUntil = null
                            });
                        }

                        var hasKey = order.ProductKeys.Any(k => k.ProductId == item.ProductId);
                        if (!hasKey)
                        {
                            var availableKey = await db.ProductKeys.FirstOrDefaultAsync(k => k.ProductId == item.ProductId && !k.IsSold);
                            if (availableKey != null)
                            {
                                availableKey.IsSold = true;
                                availableKey.OrderId = order.Id;
                            }
                        }
                    }
                }
                else if (newStatus == "Failed" || newStatus == "Cancelled")
                {
                    foreach (var key in order.ProductKeys)
                    {
                        key.IsSold = false;
                        key.OrderId = null;
                    }

                    var productIds = order.OrderItems.Select(oi => oi.ProductId).ToList();
                    var licenses = await db.Set<License>()
                        .Where(l => l.UserId == order.UserId && productIds.Contains(l.ProductId))
                        .ToListAsync();
                    db.Set<License>().RemoveRange(licenses);
                }

                await db.SaveChangesAsync();
                return Results.Ok(new { message = "Cập nhật trạng thái đơn hàng thành công" });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));
        }

        // ============ HELPER METHODS ============

        private static string NormalizeOrderStatus(string status)
        {
            return status switch
            {
                "Thành công" or "completed" or "Completed" => "Completed",
                "Thất bại" or "Hủy" or "CANCELlED" or "Cancelled" or "failed" or "Failed" => "Failed",
                "Pending" or "pending" => "Pending",
                _ => status // keep as-is
            };
        }

        private static string GetFrontendUrl(HttpContext httpContext)
        {
            return httpContext.RequestServices.GetRequiredService<IConfiguration>()["AppSettings:FrontendBaseUrl"] ?? "http://localhost:5205";
        }

        private static Transaction CreateTransaction(int userId, int orderId, decimal amount, string gateway, string gatewayTxnId)
        {
            return new Transaction
            {
                UserId = userId,
                OrderId = orderId,
                Amount = amount,
                TransactionType = "Purchase",
                PaymentGateway = gateway,
                GatewayTransactionId = gatewayTxnId,
                Status = "Success",
                CreatedAt = DateTime.UtcNow
            };
        }

        private static async Task CompleteOrder(Order order, GameStoreDbContext db, int userId, List<int> productIds, string gateway, string gatewayTxnId, Coupon? coupon, IEmailService emailService, string email)
        {
            order.Status = "Completed";
            db.Orders.Update(order);
            
            // Batch: lấy tất cả available keys 1 lần
            var availableKeys = await db.ProductKeys
                .Where(k => productIds.Contains(k.ProductId) && !k.IsSold)
                .ToListAsync();
            var availableKeyDict = availableKeys.GroupBy(k => k.ProductId).ToDictionary(g => g.Key, g => g.FirstOrDefault());

            foreach (var pId in productIds)
            {
                db.Set<License>().Add(new License
                {
                    UserId = userId,
                    ProductId = pId,
                    Type = "Permanent",
                    ValidFrom = DateTime.UtcNow,
                    ValidUntil = null
                });

                if (availableKeyDict.TryGetValue(pId, out var key) && key != null)
                {
                    key.IsSold = true;
                    key.OrderId = order.Id;
                }
                else
                {
                    var fallbackKey = new ProductKey
                    {
                        ProductId = pId,
                        KeyCode = $"NEXUS-{Guid.NewGuid().ToString("N")[..4].ToUpper()}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}",
                        IsSold = true,
                        OrderId = order.Id
                    };
                    db.ProductKeys.Add(fallbackKey);
                }
            }

            if (!string.IsNullOrEmpty(email))
            {
                try
                {
                    var emailBody = $"Thank you for your purchase. Order ID: {order.Id}, Total: {order.TotalAmount:C}.";
                    await emailService.SendEmailAsync(email, "Your Order Receipt", emailBody);
                }
                catch
                {
                    // Ignore email service failure in dev / mock environment
                }
            }

            db.Set<Transaction>().Add(CreateTransaction(userId, order.Id, order.TotalAmount, gateway, gatewayTxnId));

            // Tăng CurrentUsage của coupon chỉ khi payment thành công!
            if (coupon != null)
            {
                coupon.CurrentUsage++;
                db.Set<Coupon>().Update(coupon);
            }

            await db.SaveChangesAsync(); // CHỈ GỌI 1 LẦN DUY NHẤT
        }
    }

    // ============ EXTENSION METHODS ============
    public static class ClaimsPrincipalExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var idStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                        user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (int.TryParse(idStr, out int id)) return id;
            return null;
        }
    }

    public static class DebugEndpoints 
    {
        public static void MapDebugEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/debug-keys", async (GameStoreDbContext db, ClaimsPrincipal user) =>
            {
                if (!user.IsInRole("Administrator"))
                    return Results.Forbid();

                var keys = await db.ProductKeys
                    .Select(k => new { 
                        k.Id, 
                        k.ProductId, 
                        k.KeyCode, 
                        k.IsSold, 
                        k.OrderId 
                    }).ToListAsync();
                
                var orders = await db.Orders
                    .Include(o => o.OrderItems)
                    .Select(o => new {
                        o.Id,
                        o.UserId,
                        o.Status,
                        Items = o.OrderItems.Select(oi => oi.ProductId).ToList()
                    }).ToListAsync();

                return Results.Ok(new { keys, orders });
            });
        }
    }

    public class CheckoutRequest
    {
        public List<int> ProductIds { get; set; } = new List<int>();
        public string PaymentMethod { get; set; } = "MockGateway";
        public string? CouponCode { get; set; }
    }

    public class MoMoNotifyRequest
    {
        public string partnerCode { get; set; } = string.Empty;
        public string orderId { get; set; } = string.Empty;
        public string requestId { get; set; } = string.Empty;
        public long amount { get; set; }
        public string orderInfo { get; set; } = string.Empty;
        public string orderType { get; set; } = string.Empty;
        public string transId { get; set; } = string.Empty;
        public int resultCode { get; set; }
        public string message { get; set; } = string.Empty;
        public string payType { get; set; } = string.Empty;
        public string responseTime { get; set; } = string.Empty;
        public string extraData { get; set; } = string.Empty;
        public string signature { get; set; } = string.Empty;
    }

    public class UpdateOrderStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}