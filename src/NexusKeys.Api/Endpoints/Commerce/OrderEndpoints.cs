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
using System.Collections.Concurrent;

namespace GameStore.API.Endpoints
{
    public class PaginationParams
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public static class OrderEndpoints
    {
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> PaymentCompletionLocks = new();

        public static void MapOrderEndpoints(
            this IEndpointRouteBuilder app,
            string apiPrefix = "/api",
            bool mapPaymentCallbacks = true)
        {
            var isV1 = EndpointRouteConventions.IsV1(apiPrefix);
            var group = app.MapGroup($"{apiPrefix}/orders");
            var currentUserGroup = isV1
                ? app.MapGroup($"{apiPrefix}/users/me")
                : group;

            group.MapGet("/", async (GameStoreDbContext db, ClaimsPrincipal userClaim, [FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);
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
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var orders = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Results.Ok(new {
                    Data = orders,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));

            group.MapGet("/{id:int}", async (int id, GameStoreDbContext db, ClaimsPrincipal userClaim) =>
            {
                var userId = userClaim.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var order = await db.Orders
                    .Include(o => o.User)
                    .Include(o => o.ProductKeys).ThenInclude(k => k.Product)
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == id);
                
                if (order == null) return Results.NotFound();

                // Chỉ chủ sở hữu hoặc Admin/Staff/Dev mới xem được
                var isStaffOrAdmin = userClaim.IsInRole("Administrator") || userClaim.IsInRole("Developer") || userClaim.IsInRole("Staff");
                if (order.UserId != userId && !isStaffOrAdmin) return Results.Forbid();
                
                return Results.Ok(new {
                    Id = order.Id,
                    Username = order.User.Username,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    OrderDate = order.OrderDate,
                    Items = order.OrderItems.Select(oi => oi.Product.Name).ToList(),
                    Keys = order.ProductKeys.Select(k => new { k.Id, k.KeyCode, ProductName = k.Product.Name }).ToList()
                });
            }).RequireAuthorization();

            currentUserGroup.MapGet(isV1 ? "/orders" : "/my-orders", async (GameStoreDbContext db, ClaimsPrincipal userClaim) =>
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
            }).RequireAuthorization();

            group.MapDelete(isV1 ? "/{orderId:int}/product-keys/{keyId:int}" : "/{orderId:int}/keys/{keyId:int}", async (int orderId, int keyId, GameStoreDbContext db) =>
            {
                var order = await db.Orders.Include(o => o.ProductKeys).FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null) return Results.NotFound("Order not found.");

                var key = order.ProductKeys.FirstOrDefault(k => k.Id == keyId);
                if (key == null) return Results.NotFound("Key not found in this order.");

                key.OrderId = null;
                key.IsSold = false;
                db.ProductKeys.Update(key);
                await db.SaveChangesAsync();

                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // CHECKOUT: Tạo Order trạng thái Pending và trả về Payment URL
            var checkoutGroup = isV1 ? app.MapGroup(apiPrefix) : group;
            checkoutGroup.MapPost(isV1 ? "/checkouts" : "/checkout", async (CheckoutRequest req, HttpContext httpContext, GameStoreDbContext db, ClaimsPrincipal userClaim, VnPayService vnPayService, MoMoService moMoService, IEmailService emailService) =>
            {
                if (req.ProductIds == null || !req.ProductIds.Any())
                    return Results.BadRequest("Giỏ hàng trống.");

                var userId = userClaim.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var uniqueIds = req.ProductIds.Distinct().ToList();
                var productsDict = await db.Products
                    .Include(p => p.ProductPromotions).ThenInclude(pp => pp.Promotion)
                    .Where(p => uniqueIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                if (productsDict.Count != uniqueIds.Count)
                    return Results.BadRequest("Sản phẩm không hợp lệ.");

                var now = DateTime.UtcNow;
                // Tính giá có discount từ Promotion
                decimal totalAmount = req.ProductIds.Sum(id => {
                    var product = productsDict[id];
                    return product.GetActiveDiscountPrice(now) ?? product.BasePrice;
                });
                Coupon? coupon = null;
                string? appliedCouponCode = null;

                // Áp dụng Coupon nếu có - KHÔNG tăng CurrentUsage ở đây
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
                    appliedCouponCode = couponCode;
                }

                // Sử dụng execution strategy cùng transaction để hỗ trợ retry an toàn trên PostgreSQL
                var strategy = db.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await db.Database.BeginTransactionAsync();
                    try
                    {
                        // Kiểm tra tồn kho trước khi tạo đơn hàng
                        foreach (var group in req.ProductIds.GroupBy(id => id))
                        {
                            var needed = group.Count();
                            var availableKeyCount = await db.ProductKeys.CountAsync(k => k.ProductId == group.Key && !k.IsSold);
                            if (availableKeyCount < needed)
                            {
                                var productName = productsDict[group.Key].Name;
                                await transaction.RollbackAsync();
                                return Results.BadRequest($"Sản phẩm '{productName}' không đủ số lượng Key khả dụng.");
                            }
                        }

                        var order = new Order
                        {
                            UserId = userId.Value,
                            TotalAmount = totalAmount,
                            Status = "Pending",
                            OrderDate = DateTime.UtcNow,
                            CouponCode = appliedCouponCode
                        };

                        db.Orders.Add(order);
                        await db.SaveChangesAsync();

                        foreach (var pId in req.ProductIds)
                        {
                            var product = productsDict[pId];
                            var actualPrice = product.GetActiveDiscountPrice(now) ?? product.BasePrice;
                            db.Set<OrderItem>().Add(new OrderItem
                            {
                                OrderId = order.Id,
                                ProductId = product.Id,
                                PriceAtPurchase = actualPrice
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
                            // MockGateway: complete immediately
                            await CompleteOrder(order, db, userId.Value, req.ProductIds, "MockGateway", Guid.NewGuid().ToString(), coupon, emailService, userClaim.FindFirst(ClaimTypes.Email)?.Value ?? "");
                            await transaction.CommitAsync();
                            var completedResponse = new { message = "Thanh toán thành công!", orderId = order.Id, paymentUrl = "" };
                            return isV1
                                ? Results.Created($"{EndpointRouteConventions.PublicPrefix(apiPrefix)}/orders/{order.Id}", completedResponse)
                                : Results.Ok(completedResponse);
                        }

                        await transaction.CommitAsync();
                        var checkoutResponse = new { message = "Chuyển hướng thanh toán", orderId = order.Id, paymentUrl = paymentUrl };
                        return isV1
                            ? Results.Created($"{EndpointRouteConventions.PublicPrefix(apiPrefix)}/orders/{order.Id}", checkoutResponse)
                            : Results.Ok(checkoutResponse);
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        return Results.StatusCode(500);
                    }
                });
            }).RequireAuthorization();

            // Re-create a payment URL for an existing pending order. This does
            // not create a second order or reserve additional product keys.
            group.MapPost(isV1 ? "/{id:int}/payment-attempts" : "/{id:int}/payment", async (int id, RetryPaymentRequest req, HttpContext httpContext, GameStoreDbContext db, ClaimsPrincipal userClaim, VnPayService vnPayService, MoMoService moMoService, IEmailService emailService) =>
            {
                var userId = userClaim.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var order = await db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);
                if (order == null) return Results.NotFound(new { message = "Không tìm thấy đơn hàng." });
                if (order.UserId != userId.Value && !userClaim.IsInRole("Administrator")) return Results.Forbid();
                if (!string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                    return Results.BadRequest(new { message = "Chỉ có thể thanh toán lại đơn đang chờ thanh toán." });

                // Pending orders do not reserve keys. Re-check inventory before
                // redirecting to a provider so a paid order can still be fulfilled.
                foreach (var itemGroup in order.OrderItems.GroupBy(item => item.ProductId))
                {
                    var availableKeyCount = await db.ProductKeys.CountAsync(key =>
                        key.ProductId == itemGroup.Key && !key.IsSold);

                    if (availableKeyCount < itemGroup.Count())
                        return Results.BadRequest(new { message = "Một sản phẩm trong đơn đã hết key. Vui lòng tạo đơn hàng mới." });
                }

                var paymentMethod = req.PaymentMethod?.Trim();
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                string paymentUrl;

                if (string.Equals(paymentMethod, "VNPay", StringComparison.OrdinalIgnoreCase))
                {
                    paymentUrl = vnPayService.CreatePaymentUrl(order.Id, order.TotalAmount, ipAddress);
                }
                else if (string.Equals(paymentMethod, "MoMo", StringComparison.OrdinalIgnoreCase))
                {
                    paymentUrl = await moMoService.CreatePaymentUrlAsync(order.Id, order.TotalAmount, "Thanh toan game");
                }
                else if (string.Equals(paymentMethod, "MockGateway", StringComparison.OrdinalIgnoreCase) || string.Equals(paymentMethod, "Mock", StringComparison.OrdinalIgnoreCase))
                {
                    var productIds = order.OrderItems.Select(oi => oi.ProductId).ToList();
                    Coupon? coupon = null;
                    if (!string.IsNullOrEmpty(order.CouponCode))
                    {
                        coupon = await db.Set<Coupon>().FirstOrDefaultAsync(c => c.Code == order.CouponCode);
                    }
                    await CompleteOrder(order, db, order.UserId, productIds, "MockGateway", Guid.NewGuid().ToString(), coupon, emailService, userClaim.FindFirst(ClaimTypes.Email)?.Value ?? "");
                    return Results.Ok(new { message = "Thanh toán thành công!", orderId = order.Id, paymentUrl = "" });
                }
                else
                {
                    return Results.BadRequest(new { message = "Phương thức thanh toán không được hỗ trợ." });
                }

                return Results.Ok(new { message = "Chuyển hướng thanh toán", orderId = order.Id, paymentUrl });
            }).RequireAuthorization();

            if (mapPaymentCallbacks)
            {
                // Legacy provider callbacks. These are integration adapters, not REST resources.
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
                            // Load coupon từ Order nếu có
                            Coupon? coupon = null;
                            if (!string.IsNullOrEmpty(order.CouponCode))
                            {
                                coupon = await db.Set<Coupon>().FirstOrDefaultAsync(c => c.Code == order.CouponCode);
                            }
                            await CompleteOrder(order, db, order.UserId, productIds, "VNPay", query["vnp_TransactionNo"].ToString(), coupon, emailService, "");

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
                            // Load coupon từ Order nếu có
                            Coupon? moMoCoupon = null;
                            if (!string.IsNullOrEmpty(order.CouponCode))
                            {
                                moMoCoupon = await db.Set<Coupon>().FirstOrDefaultAsync(c => c.Code == order.CouponCode);
                            }
                            await CompleteOrder(order, db, order.UserId, productIds, "MoMo", req.transId, moMoCoupon, emailService, "");

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

                app.MapGet("/api/integrations/vnpay/return", HandleVnPayIntegrationReturn)
                    .AllowAnonymous();
                app.MapPost("/api/webhooks/momo", HandleMomoWebhook)
                    .AllowAnonymous();
            }

            currentUserGroup.MapGet(isV1 ? "/library-items" : "/library", async (GameStoreDbContext db, ClaimsPrincipal userClaim) =>
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
            }).RequireAuthorization();

            if (!isV1)
            {
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
                }).RequireAuthorization();
            }

            group.MapMethods(isV1 ? "/{id:int}" : "/{id:int}/status", [isV1 ? HttpMethods.Patch : HttpMethods.Put], async (int id, UpdateOrderStatusDto dto, GameStoreDbContext db) =>
            {
                var order = await db.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.ProductKeys)
                    .FirstOrDefaultAsync(o => o.Id == id);
                if (order == null) return Results.NotFound();

                var oldStatus = order.Status;
                var newStatus = NormalizeOrderStatus(dto.Status);
                if (newStatus == null)
                {
                    return Results.BadRequest(new { message = "Trạng thái không hợp lệ." });
                }
                if (oldStatus == newStatus)
                {
                    return isV1
                        ? Results.NoContent()
                        : Results.Ok(new { message = "Trạng thái không đổi" });
                }

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
                return isV1
                    ? Results.NoContent()
                    : Results.Ok(new { message = "Cập nhật trạng thái đơn hàng thành công" });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));
        }

        // ============ HELPER METHODS ============

        private static async Task<IResult> HandleVnPayIntegrationReturn(
            HttpContext httpContext,
            GameStoreDbContext db,
            IHubContext<NexusHub> hubContext,
            VnPayService vnPayService,
            IEmailService emailService)
        {
            var query = httpContext.Request.Query;
            if (!vnPayService.ValidateSignature(query))
            {
                return Results.Redirect($"{GetFrontendUrl(httpContext)}/cart?error=invalid_signature");
            }

            var responseCode = query["vnp_ResponseCode"].ToString();
            if (!int.TryParse(query["vnp_TxnRef"], out var orderId))
            {
                return Results.Redirect($"{GetFrontendUrl(httpContext)}/cart?error=payment_failed");
            }

            var order = await db.Orders
                .Include(item => item.OrderItems)
                .FirstOrDefaultAsync(item => item.Id == orderId);
            if (order is null)
            {
                return Results.Redirect($"{GetFrontendUrl(httpContext)}/cart?error=payment_failed");
            }

            if (string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Redirect($"{GetFrontendUrl(httpContext)}/checkout/success?orderId={orderId}");
            }

            if (!string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase) || responseCode != "00")
            {
                order.Status = "Failed";
                await db.SaveChangesAsync();
                return Results.Redirect($"{GetFrontendUrl(httpContext)}/cart?error=payment_failed");
            }

            var coupon = string.IsNullOrEmpty(order.CouponCode)
                ? null
                : await db.Coupons.FirstOrDefaultAsync(item => item.Code == order.CouponCode);
            var productIds = order.OrderItems.Select(item => item.ProductId).ToList();
            await CompleteOrder(
                order,
                db,
                order.UserId,
                productIds,
                "VNPay",
                query["vnp_TransactionNo"].ToString(),
                coupon,
                emailService,
                string.Empty);

            await hubContext.Clients.All.SendAsync("ReceiveNewOrder");
            return Results.Redirect($"{GetFrontendUrl(httpContext)}/checkout/success?orderId={orderId}");
        }

        private static async Task<IResult> HandleMomoWebhook(
            [FromBody] MoMoNotifyRequest request,
            GameStoreDbContext db,
            IHubContext<NexusHub> hubContext,
            MoMoService moMoService,
            IEmailService emailService)
        {
            var isValid = moMoService.ValidateSignature(
                request.partnerCode,
                request.orderId,
                request.requestId,
                request.amount.ToString(),
                request.orderInfo,
                request.orderType,
                request.transId,
                request.message,
                string.Empty,
                request.responseTime,
                request.resultCode.ToString(),
                request.payType,
                request.extraData,
                request.signature);
            if (!isValid)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid webhook signature");
            }

            if (!int.TryParse(request.orderId.Split('_')[0], out var orderId))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Order not found");
            }

            var order = await db.Orders
                .Include(item => item.OrderItems)
                .FirstOrDefaultAsync(item => item.Id == orderId);
            if (order is null)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Order not found");
            }

            if (string.Equals(order.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                return Results.NoContent();
            }

            if (!string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Order is not pending");
            }

            if (request.resultCode != 0)
            {
                order.Status = "Failed";
                await db.SaveChangesAsync();
                return Results.NoContent();
            }

            var coupon = string.IsNullOrEmpty(order.CouponCode)
                ? null
                : await db.Coupons.FirstOrDefaultAsync(item => item.Code == order.CouponCode);
            var productIds = order.OrderItems.Select(item => item.ProductId).ToList();
            await CompleteOrder(
                order,
                db,
                order.UserId,
                productIds,
                "MoMo",
                request.transId,
                coupon,
                emailService,
                string.Empty);

            await hubContext.Clients.All.SendAsync("ReceiveNewOrder");
            return Results.NoContent();
        }

        private static string? NormalizeOrderStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return null;
            return status.Trim() switch
            {
                "Thành công" or "completed" or "Completed" => "Completed",
                "Thất bại" or "Hủy" or "CANCELlED" or "Cancelled" or "failed" or "Failed" => "Failed",
                "Pending" or "pending" => "Pending",
                _ => null
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
            var completionLock = PaymentCompletionLocks.GetOrAdd(order.Id, _ => new SemaphoreSlim(1, 1));
            await completionLock.WaitAsync();

            try
            {
            await db.Entry(order).ReloadAsync();
            if (!string.Equals(order.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var transactionAlreadyProcessed = await db.Transactions.AnyAsync(transaction =>
                transaction.PaymentGateway == gateway
                && transaction.GatewayTransactionId == gatewayTxnId);
            if (transactionAlreadyProcessed)
            {
                return;
            }

            order.Status = "Completed";
            db.Orders.Update(order);
            
            // Batch: lấy tất cả available keys 1 lần
            var availableKeys = await db.ProductKeys
                .Where(k => productIds.Contains(k.ProductId) && !k.IsSold)
                .ToListAsync();
            var keyQueueDict = availableKeys.GroupBy(k => k.ProductId).ToDictionary(g => g.Key, g => new Queue<ProductKey>(g));

            foreach (var pId in productIds)
            {
                var hasLicense = await db.Set<License>().AnyAsync(l => l.UserId == userId && l.ProductId == pId);
                if (!hasLicense)
                {
                    db.Set<License>().Add(new License
                    {
                        UserId = userId,
                        ProductId = pId,
                        Type = "Permanent",
                        ValidFrom = DateTime.UtcNow,
                        ValidUntil = null
                    });
                }

                if (keyQueueDict.TryGetValue(pId, out var queue) && queue.Count > 0)
                {
                    var key = queue.Dequeue();
                    key.IsSold = true;
                    key.OrderId = order.Id;
                }
            }

            if (string.IsNullOrEmpty(email))
            {
                var u = await db.Users.FindAsync(userId);
                if (u != null) email = u.Email;
            }

            if (!string.IsNullOrEmpty(email))
            {
                try
                {
                    var emailBody = $"Thank you for your purchase. Order ID: {order.Id}, Total: {order.TotalAmount:C}.";
                    await emailService.SendEmailAsync(email, "Your Order Receipt", emailBody);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
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
            CacheKeys.InvalidateProducts();
            CacheKeys.InvalidateDashboard();
            }
            finally
            {
                completionLock.Release();
            }
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
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));
        }
    }

    public class CheckoutRequest
    {
        public List<int> ProductIds { get; set; } = new List<int>();
        public string PaymentMethod { get; set; } = "MockGateway";
        public string? CouponCode { get; set; }
    }

    public class RetryPaymentRequest
    {
        public string PaymentMethod { get; set; } = "VNPay";
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
