using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using GameStore.API.Data;

namespace GameStore.API.Endpoints
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }

    public static class CategoryEndpoints
    {
        public static void MapCategoryEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var group = app.MapGroup($"{apiPrefix}/categories");

            // GET all categories with product counts
            group.MapGet("/", async (GameStoreDbContext db, [FromServices] IMemoryCache? cache) =>
            {
                string cacheKey = $"categories_list_v{CacheKeys.ProductVersion}";
                if (cache != null && cache.TryGetValue(cacheKey, out List<CategoryDto>? cachedList) && cachedList != null)
                {
                    return Results.Ok(cachedList);
                }

                var categories = await db.Categories.AsNoTracking()
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        ProductCount = c.ProductCategories.Count(pc => pc.Product.Status == "Active")
                    })
                    .OrderByDescending(c => c.ProductCount)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                cache?.Set(cacheKey, categories, TimeSpan.FromMinutes(5));
                return Results.Ok(categories);
            });

            // GET category by id
            group.MapGet("/{id:int}", async (int id, GameStoreDbContext db) =>
            {
                var c = await db.Categories.AsNoTracking()
                    .Where(c => c.Id == id)
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        ProductCount = c.ProductCategories.Count(pc => pc.Product.Status == "Active")
                    })
                    .FirstOrDefaultAsync();

                if (c == null) return Results.NotFound();
                return Results.Ok(c);
            });
        }
    }
}
