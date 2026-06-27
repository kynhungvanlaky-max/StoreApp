using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using GameStore.API.Data;

namespace GameStore.API.Endpoints
{
    public static class RecommendationEndpoints
    {
        public static void MapRecommendationEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/recommendations");

            // GET recommendations based on category (ngẫu nhiên trong cùng category)
            group.MapGet("/", async (GameStoreDbContext db, [FromQuery] int? categoryId = null, [FromQuery] int? excludeProductId = null, [FromQuery] int count = 6) =>
            {
                var query = db.Products
                    .Include(p => p.Developer)
                    .Include(p => p.ProductMedia)
                    .Include(p => p.ProductCategories)
                        .ThenInclude(pc => pc.Category)
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
                var products = await query
                    .Skip(skip)
                    .Take(count)
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        BasePrice = p.BasePrice,
                        ImageUrl = p.GetCoverImageUrl(),
                        Categories = p.ProductCategories.Select(pc => pc.Category.Name).ToList(),
                        DiscountPercent = p.GetActiveDiscountPercent(now)
                    })
                    .ToListAsync();

                return Results.Ok(products);
            });

            // GET trending (sản phẩm có nhiều lượt mua nhất)
            group.MapGet("/trending", async (GameStoreDbContext db, [FromQuery] int count = 6) =>
            {
                var now = DateTime.UtcNow;
                var products = await db.Products
                    .Include(p => p.Developer)
                    .Include(p => p.ProductMedia)
                    .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
                    .Include(p => p.OrderItems)
                    .OrderByDescending(p => p.OrderItems.Count)
                    .Take(count)
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        BasePrice = p.BasePrice,
                        ImageUrl = p.GetCoverImageUrl(),
                        Categories = p.ProductCategories.Select(pc => pc.Category.Name).ToList(),
                        DiscountPercent = p.GetActiveDiscountPercent(now),
                        SalesCount = p.OrderItems.Count
                    })
                    .ToListAsync();

                return Results.Ok(products);
            });
        }
    }
}