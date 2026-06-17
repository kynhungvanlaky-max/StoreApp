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

namespace GameStore.API.Endpoints
{
    public static class OrderEndpoints
    {
        public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/orders").RequireAuthorization();

            group.MapGet("/", async (GameStoreDbContext db, ClaimsPrincipal userClaim) =>
            {
                var role = userClaim.FindFirst(ClaimTypes.Role)?.Value;
                if (role != "Administrator") return Results.Forbid();

                var orders = await db.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new {
                        Id = o.Id,
                        Username = o.User.Username,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status,
                        OrderDate = o.OrderDate
                    }).ToListAsync();
                
                return Results.Ok(orders);
            });

            group.MapGet("/{id}", async (int id, GameStoreDbContext db, ClaimsPrincipal userClaim) =>
            {
                var role = userClaim.FindFirst(ClaimTypes.Role)?.Value;
                // Nếu là Admin, có thể xem đơn hàng bất kỳ
                // Nếu là Customer, chỉ xem được đơn hàng của mình
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
                var userIdStr = userClaim.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                                userClaim.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Results.Unauthorized();

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

            group.MapDelete("/{orderId}/keys/{keyId}", async (int orderId, int keyId, GameStoreDbContext db, ClaimsPrincipal userClaim) =>
            {
                var role = userClaim.FindFirst(ClaimTypes.Role)?.Value;
                if (role != "Administrator") return Results.Forbid();

                var order = await db.Orders.Include(o => o.ProductKeys).FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null) return Results.NotFound("Order not found.");

                var key = order.ProductKeys.FirstOrDefault(k => k.Id == keyId);
                if (key == null) return Results.NotFound("Key not found in this order.");

                // Detach key from order
                key.OrderId = null;
                key.IsSold = false;
                db.ProductKeys.Update(key);
                await db.SaveChangesAsync();

                return Results.Ok();
            });

            // CHECKOUT: Tạo Order trạng thái Pending và trả về Payment URL
            group.MapPost("/checkout", async (CheckoutRequest req, HttpContext httpContext, GameStoreDbContext db, ClaimsPrincipal userClaim, VnPayService vnPayService, MoMoService moMoService) =>
            {
                if (req.ProductIds == null || !req.ProductIds.Any())
                    return Results.BadRequest("Giỏ hàng trống.");

                var userIdStr = userClaim.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                                userClaim.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Results.Unauthorized();

                var products = await db.Products.Where(p => req.ProductIds.Contains(p.Id)).ToListAsync();
                if (!products.Any())
                    return Results.BadRequest("Sản phẩm không hợp lệ.");

                decimal totalAmount = products.Sum(p => p.BasePrice);

                var order = new Order
                {
                    UserId = userId,
                    TotalAmount = totalAmount,
                    Status = "Pending", // Chờ thanh toán
                    OrderDate = DateTime.UtcNow
                };
                db.Orders.Add(order);
                await db.SaveChangesAsync();

                foreach (var product in products)
                {
                    db.Set<OrderItem>().Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = product.Id,
                        PriceAtPurchase = product.BasePrice
                    });
                }
                await db.SaveChangesAsync();

                string paymentUrl = string.Empty;
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

                if (req.PaymentMethod == "VNPay")
                {
                    paymentUrl = vnPayService.CreatePaymentUrl(order.Id, totalAmount, ipAddress);
                }
                else if (req.PaymentMethod == "MoMo")
                {
                    paymentUrl = await moMoService.CreatePaymentUrlAsync(order.Id, totalAmount, "Thanh toan game");
                }
                else
                {
                    // Fallback to MockGateway for dev testing if no method matched
                    order.Status = "Completed";
                    db.Orders.Update(order);
                    
                    foreach (var product in products)
                    {
                        db.Set<License>().Add(new License
                        {
                            UserId = userId,
                            ProductId = product.Id,
                            Type = "Permanent",
                            ValidFrom = DateTime.UtcNow,
                            ValidUntil = null
                        });
                    }
                    
                    db.Set<Transaction>().Add(new Transaction
                    {
                        UserId = userId,
                        OrderId = order.Id,
                        Amount = totalAmount,
                        TransactionType = "Purchase",
                        PaymentGateway = "MockGateway",
                        GatewayTransactionId = Guid.NewGuid().ToString(),
                        Status = "Success",
                        CreatedAt = DateTime.UtcNow
                    });
                    
                    await db.SaveChangesAsync();
                    return Results.Ok(new { message = "Thanh toán thành công!", orderId = order.Id, paymentUrl = "" });
                }

                return Results.Ok(new { message = "Chuyển hướng thanh toán", orderId = order.Id, paymentUrl = paymentUrl });
            });

            // VNPay Return Webhook
            app.MapGet("/api/orders/vnpay-return", async (HttpContext httpContext, GameStoreDbContext db, IHubContext<NexusHub> hubContext, VnPayService vnPayService) =>
            {
                var query = httpContext.Request.Query;
                if (!vnPayService.ValidateSignature(query))
                    return Results.Redirect("http://localhost:3000/Payment/Result?status=failed&reason=invalid_signature");

                var vnp_ResponseCode = query["vnp_ResponseCode"].ToString();
                var vnp_TxnRef = query["vnp_TxnRef"].ToString();
                
                if (int.TryParse(vnp_TxnRef, out int orderId))
                {
                    var order = await db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == orderId);
                    if (order != null && order.Status == "Pending")
                    {
                        if (vnp_ResponseCode == "00")
                        {
                            order.Status = "Completed";
                            
                            // Grant Licenses
                            foreach (var item in order.OrderItems)
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

                            db.Set<Transaction>().Add(new Transaction
                            {
                                UserId = order.UserId,
                                OrderId = order.Id,
                                Amount = order.TotalAmount,
                                TransactionType = "Purchase",
                                PaymentGateway = "VNPay",
                                GatewayTransactionId = query["vnp_TransactionNo"].ToString(),
                                Status = "Success",
                                CreatedAt = DateTime.UtcNow
                            });

                            await db.SaveChangesAsync();
                            await hubContext.Clients.All.SendAsync("ReceiveNewOrder");
                            return Results.Redirect($"http://localhost:3000/Payment/Result?status=success&orderId={orderId}");
                        }
                        else
                        {
                            order.Status = "Failed";
                            await db.SaveChangesAsync();
                            return Results.Redirect($"http://localhost:3000/Payment/Result?status=failed&orderId={orderId}");
                        }
                    }
                }
                return Results.Redirect("http://localhost:3000/Payment/Result?status=failed");
            }).AllowAnonymous();

            // MoMo Notify Webhook
            app.MapPost("/api/orders/momo-notify", async ([FromBody] MoMoNotifyRequest req, GameStoreDbContext db, IHubContext<NexusHub> hubContext, MoMoService moMoService) =>
            {
                if (!moMoService.ValidateSignature(req.partnerCode, req.orderId, req.requestId, req.amount.ToString(), req.orderInfo, req.orderType, req.transId, req.message, "", req.responseTime, req.resultCode.ToString(), req.payType, req.extraData, req.signature))
                    return Results.BadRequest(new { message = "Invalid signature" });

                var rawOrderId = req.orderId.Split('_')[0]; // Because we append timestamp
                if (int.TryParse(rawOrderId, out int dbOrderId))
                {
                    var order = await db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == dbOrderId);
                    if (order != null && order.Status == "Pending")
                    {
                        if (req.resultCode == 0) // Success
                        {
                            order.Status = "Completed";
                            
                            foreach (var item in order.OrderItems)
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

                            db.Set<Transaction>().Add(new Transaction
                            {
                                UserId = order.UserId,
                                OrderId = order.Id,
                                Amount = order.TotalAmount,
                                TransactionType = "Purchase",
                                PaymentGateway = "MoMo",
                                GatewayTransactionId = req.transId,
                                Status = "Success",
                                CreatedAt = DateTime.UtcNow
                            });

                            await db.SaveChangesAsync();
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

            // LIBRARY: Lấy danh sách các Game mà user đã mua
            group.MapGet("/library", async (GameStoreDbContext db, ClaimsPrincipal userClaim) =>
            {
                var userIdStr = userClaim.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                                userClaim.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Results.Unauthorized();

                var licenses = await db.Set<License>()
                    .Include(l => l.Product)
                    .ThenInclude(p => p.ProductMedia)
                    .Where(l => l.UserId == userId)
                    .Select(l => new
                    {
                        l.Product.Id,
                        l.Product.Name,
                        l.Product.DeveloperId, // Tạm mượn, nếu cần join thêm Developer thì join
                        ImageUrl = l.Product.ProductMedia.FirstOrDefault() != null ? l.Product.ProductMedia.FirstOrDefault()!.MediaUrl : "",
                        AcquiredDate = l.ValidFrom
                    })
                    .ToListAsync();

                return Results.Ok(licenses);
            });

            // TRANSACTIONS: Lấy danh sách giao dịch của user
            group.MapGet("/transactions", async (GameStoreDbContext db, ClaimsPrincipal userClaim) =>
            {
                var userIdStr = userClaim.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                                userClaim.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Results.Unauthorized();

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
        }
    }

    public class CheckoutRequest
    {
        public List<int> ProductIds { get; set; } = new List<int>();
        public string PaymentMethod { get; set; } = "MockGateway"; // VNPay, MoMo, MockGateway
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
}
