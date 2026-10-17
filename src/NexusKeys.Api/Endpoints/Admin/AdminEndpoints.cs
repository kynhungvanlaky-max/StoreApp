using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using GameStore.API.Data;

namespace GameStore.API.Endpoints
{
    public class DashboardSummarySql
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int CustomerCount { get; set; }
        public int TotalProducts { get; set; }
        public int AvailableKeys { get; set; }
        public int PendingSupportTickets { get; set; }
    }

    public static class AdminEndpoints
    {
        public static void MapAdminEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var group = app.MapGroup($"{apiPrefix}/admin").RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));

            group.MapGet("/dashboard", async (GameStoreDbContext db, [FromServices] IMemoryCache? cache) =>
            {
                string cacheKey = $"admin_dashboard_v{CacheKeys.DashboardVersion}";
                if (cache != null && cache.TryGetValue(cacheKey, out object? cachedDashboard) && cachedDashboard != null)
                {
                    return Results.Ok(cachedDashboard);
                }

                // Consolidated single-query aggregation
                var summaryList = await db.Database.SqlQueryRaw<DashboardSummarySql>(@"
                    SELECT 
                        COALESCE((SELECT SUM(""TotalAmount"") FROM ""Orders"" WHERE ""Status"" = 'Completed'), 0)::numeric AS ""TotalRevenue"",
                        (SELECT COUNT(*) FROM ""Orders"")::int AS ""TotalOrders"",
                        (SELECT COUNT(*) FROM ""Users"" u JOIN ""Roles"" r ON u.""RoleId"" = r.""Id"" WHERE r.""RoleName"" = 'Customer')::int AS ""CustomerCount"",
                        (SELECT COUNT(*) FROM ""Products"")::int AS ""TotalProducts"",
                        (SELECT COUNT(*) FROM ""ProductKeys"" WHERE NOT ""IsSold"")::int AS ""AvailableKeys"",
                        (SELECT COUNT(*) FROM ""SupportTickets"" WHERE ""Status"" = 'Pending')::int AS ""PendingSupportTickets""
                ").ToListAsync();

                var summary = summaryList.FirstOrDefault() ?? new DashboardSummarySql();

                var recentOrders = await db.Orders.AsNoTracking()
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

                var lowStockAlerts = await db.Products.AsNoTracking()
                    .Select(p => new
                    {
                        productId = p.Id,
                        productName = p.Name,
                        keyCount = db.ProductKeys.Count(k => k.ProductId == p.Id && !k.IsSold)
                    })
                    .Where(p => p.keyCount < 5)
                    .ToListAsync();

                var response = new
                {
                    totalRevenue = summary.TotalRevenue,
                    totalOrders = summary.TotalOrders,
                    customerCount = summary.CustomerCount,
                    totalProducts = summary.TotalProducts,
                    availableKeys = summary.AvailableKeys,
                    pendingSupportTickets = summary.PendingSupportTickets,
                    recentOrders,
                    lowStockAlerts
                };

                cache?.Set(cacheKey, response, TimeSpan.FromSeconds(30));
                return Results.Ok(response);
            });

            group.MapGet("/chart-data", async (GameStoreDbContext db, [FromServices] IMemoryCache? cache) =>
            {
                string cacheKey = $"admin_chart_data_v{CacheKeys.DashboardVersion}";
                if (cache != null && cache.TryGetValue(cacheKey, out object? cachedChart) && cachedChart != null)
                {
                    return Results.Ok(cachedChart);
                }

                var startDate = DateTime.UtcNow.AddDays(-30).Date;

                var data = await db.Orders.AsNoTracking()
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

                cache?.Set(cacheKey, result, TimeSpan.FromMinutes(2));
                return Results.Ok(result);
            });

            // GET orders by status statistics
            group.MapGet("/orders-by-status", async (GameStoreDbContext db) =>
            {
                var stats = await db.Orders.AsNoTracking()
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
                var products = await db.OrderItems.AsNoTracking()
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

                var data = await db.Orders.AsNoTracking()
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
