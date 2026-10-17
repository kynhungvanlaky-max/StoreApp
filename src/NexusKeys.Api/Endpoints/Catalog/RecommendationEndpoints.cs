using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using GameStore.API.Data;

namespace GameStore.API.Endpoints
{
    public static class RecommendationEndpoints
    {
        public static void MapRecommendationEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var group = app.MapGroup($"{apiPrefix}/recommendations");

            // GET recommendations based on category (ngẫu nhiên trong cùng category)
            group.MapGet("/", async (GameStoreDbContext db, [FromServices] IMemoryCache? cache, [FromQuery] int? categoryId = null, [FromQuery] int? excludeProductId = null, [FromQuery] int count = 6) =>
            {
                string cacheKey = $"rec_cat_v{CacheKeys.ProductVersion}_{categoryId}_{excludeProductId}_{count}";
                if (cache != null && cache.TryGetValue(cacheKey, out object? cachedResult) && cachedResult != null)
                {
                    return Results.Ok(cachedResult);
                }

                var query = db.Products.AsNoTracking()
                    .Where(p => p.Status == "Active")
                    .AsQueryable();

                if (excludeProductId.HasValue)
                {
                    query = query.Where(p => p.Id != excludeProductId.Value);
                }

                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.ProductCategories.Any(pc => pc.CategoryId == categoryId.Value));
                }

                var totalCount = await query.CountAsync();
                if (totalCount == 0) return Results.Ok(new List<object>());

                var random = new Random();
                var skip = random.Next(0, Math.Max(0, totalCount - count));

                var now = DateTime.UtcNow;
                var rawProducts = await query
                    .Include(p => p.ProductMedia)
                    .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
                    .Include(p => p.ProductPromotions).ThenInclude(pp => pp.Promotion)
                    .Skip(skip)
                    .Take(count)
                    .ToListAsync();

                var products = rawProducts.Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Pricing = p.Pricing,
                    BasePrice = p.BasePrice,
                    ImageUrl = p.GetCoverImageUrl(),
                    Categories = p.ProductCategories.Select(pc => pc.Category.Name).ToList(),
                    DiscountPercent = p.GetActiveDiscountPercent(now),
                    DiscountPrice = p.GetActiveDiscountPrice(now)
                }).ToList();

                cache?.Set(cacheKey, products, TimeSpan.FromSeconds(60));
                return Results.Ok(products);
            });

            // GET trending (sản phẩm có nhiều lượt mua nhất)
            group.MapGet("/trending", async (GameStoreDbContext db, [FromServices] IMemoryCache? cache, [FromQuery] int count = 6) =>
            {
                string cacheKey = $"rec_trending_v{CacheKeys.ProductVersion}_{count}";
                if (cache != null && cache.TryGetValue(cacheKey, out object? cachedTrending) && cachedTrending != null)
                {
                    return Results.Ok(cachedTrending);
                }

                var now = DateTime.UtcNow;
                var rawProducts = await db.Products.AsNoTracking()
                    .Include(p => p.ProductMedia)
                    .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
                    .Include(p => p.ProductPromotions).ThenInclude(pp => pp.Promotion)
                    .Include(p => p.OrderItems)
                    .Where(p => p.Status == "Active")
                    .OrderByDescending(p => p.OrderItems.Count)
                    .Take(count)
                    .ToListAsync();

                var products = rawProducts.Select(p => new
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Pricing = p.Pricing,
                    BasePrice = p.BasePrice,
                    ImageUrl = p.GetCoverImageUrl(),
                    Categories = p.ProductCategories.Select(pc => pc.Category.Name).ToList(),
                    DiscountPercent = p.GetActiveDiscountPercent(now),
                    DiscountPrice = p.GetActiveDiscountPrice(now),
                    SalesCount = p.OrderItems.Count
                }).ToList();

                cache?.Set(cacheKey, products, TimeSpan.FromMinutes(3));
                return Results.Ok(products);
            });
        }
    }
}
