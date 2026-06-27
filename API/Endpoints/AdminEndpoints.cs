using Microsoft.EntityFrameworkCore;
using GameStore.API.Data;

namespace GameStore.API.Endpoints
{
    public static class AdminEndpoints
    {
        public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/admin").RequireAuthorization(policy => policy.RequireRole("Administrator"));

            group.MapGet("/dashboard", async (GameStoreDbContext db) =>
            {
                var totalRevenue = await db.Orders
                    .Where(o => o.Status == "Completed")
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                var totalOrders = await db.Orders.CountAsync();
                
                var customerCount = await db.Users
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName == "Customer")
                    .CountAsync();
                    
                var totalProducts = await db.Products.CountAsync();
                
                var availableKeys = await db.ProductKeys
                    .Where(k => !k.IsSold)
                    .CountAsync();
                    
                var pendingSupportTickets = await db.SupportTickets
                    .Where(t => t.Status == "Pending")
                    .CountAsync();

                var recentOrders = await db.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .Select(o => new
                    {
                        id = o.Id,
                        totalAmount = o.TotalAmount,
                        status = o.Status,
                        orderDate = o.OrderDate,
                        customerEmail = o.User.Email
                    })
                    .ToListAsync();

                var lowStockAlerts = await db.Products
                    .Select(p => new
                    {
                        productId = p.Id,
                        productName = p.Name,
                        keyCount = db.ProductKeys.Count(k => k.ProductId == p.Id && !k.IsSold)
                    })
                    .Where(p => p.keyCount < 5)
                    .ToListAsync();

                return Results.Ok(new
                {
                    totalRevenue,
                    totalOrders,
                    customerCount,
                    totalProducts,
                    availableKeys,
                    pendingSupportTickets,
                    recentOrders,
                    lowStockAlerts
                });
            });

            group.MapGet("/chart-data", async (GameStoreDbContext db) =>
            {
                var startDate = DateTime.UtcNow.AddDays(-30).Date;

                var data = await db.Orders
                    .Where(o => o.Status == "Completed" && o.OrderDate >= startDate)
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Revenue = g.Sum(o => o.TotalAmount),
                        OrdersCount = g.Count()
                    })
                    .OrderBy(g => g.Date)
                    .ToListAsync();

                // Fill missing days with 0
                var result = new List<object>();
                for (int i = 0; i < 30; i++)
                {
                    var currentDate = startDate.AddDays(i);
                    var dayData = data.FirstOrDefault(d => d.Date == currentDate);
                    
                    result.Add(new
                    {
                        Date = currentDate.ToString("dd/MM"),
                        Revenue = dayData?.Revenue ?? 0,
                        OrdersCount = dayData?.OrdersCount ?? 0
                    });
                }

                return Results.Ok(result);
            });

            // GET orders by status statistics
            group.MapGet("/orders-by-status", async (GameStoreDbContext db) =>
            {
                var stats = await db.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                return Results.Ok(stats);
            });

            // GET top selling products
            group.MapGet("/top-products", async (GameStoreDbContext db, int top = 5) =>
            {
                var products = await db.OrderItems
                    .Include(oi => oi.Product)
                    .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
                    .Select(g => new
                    {
                        ProductName = g.Key.Name,
                        TotalSold = g.Count(),
                        TotalRevenue = g.Sum(oi => oi.PriceAtPurchase)
                    })
                    .OrderByDescending(p => p.TotalSold)
                    .Take(top)
                    .ToListAsync();

                return Results.Ok(products);
            });

            // GET monthly revenue for last 12 months
            group.MapGet("/monthly-revenue", async (GameStoreDbContext db) =>
            {
                var startDate = DateTime.UtcNow.AddMonths(-11).Date;
                startDate = new DateTime(startDate.Year, startDate.Month, 1);

                var data = await db.Orders
                    .Where(o => o.Status == "Completed" && o.OrderDate >= startDate)
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        Revenue = g.Sum(o => o.TotalAmount),
                        OrdersCount = g.Count()
                    })
                    .OrderBy(g => g.Year).ThenBy(g => g.Month)
                    .ToListAsync();

                var result = new List<object>();
                for (int i = 0; i < 12; i++)
                {
                    var currentDate = startDate.AddMonths(i);
                    var monthData = data.FirstOrDefault(d => d.Year == currentDate.Year && d.Month == currentDate.Month);
                    
                    result.Add(new
                    {
                        Month = currentDate.ToString("MM/yyyy"),
                        Revenue = monthData?.Revenue ?? 0,
                        OrdersCount = monthData?.OrdersCount ?? 0
                    });
                }

                return Results.Ok(result);
            });
        }
    }
}
