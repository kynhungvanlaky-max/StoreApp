using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GameStore.API.Endpoints
{
    public class PromotionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive => DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;
        public List<int> ProductIds { get; set; } = new();
    }

    public class CreatePromotionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public static class PromotionEndpoints
    {
        public static void MapPromotionEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var isV1 = EndpointRouteConventions.IsV1(apiPrefix);
            var group = app.MapGroup($"{apiPrefix}/promotions");

            // GET all promotions
            group.MapGet("/", async (GameStoreDbContext db) =>
            {
                var promos = await db.Promotions.AsNoTracking()
                    .Include(p => p.ProductPromotions)
                    .OrderByDescending(p => p.StartDate)
                    .Select(p => new PromotionDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        DiscountPercent = p.DiscountPercent,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        ProductIds = p.ProductPromotions.Select(pp => pp.ProductId).ToList()
                    })
                    .ToListAsync();
                
                return Results.Ok(promos);
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // GET active promotions
            group.MapGet("/active", async (GameStoreDbContext db, [FromServices] IMemoryCache? cache) =>
            {
                string cacheKey = $"promos_active_v{CacheKeys.PromotionVersion}";
                if (cache != null && cache.TryGetValue(cacheKey, out object? cachedPromos) && cachedPromos != null)
                {
                    return Results.Ok(cachedPromos);
                }

                var now = DateTime.UtcNow;
                var promos = await db.Promotions.AsNoTracking()
                    .Include(p => p.ProductPromotions)
                    .Where(p => p.StartDate <= now && p.EndDate >= now)
                    .OrderByDescending(p => p.DiscountPercent)
                    .Select(p => new PromotionDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        DiscountPercent = p.DiscountPercent,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        ProductIds = p.ProductPromotions.Select(pp => pp.ProductId).ToList()
                    })
                    .ToListAsync();

                cache?.Set(cacheKey, promos, TimeSpan.FromMinutes(3));
                return Results.Ok(promos);
            });

            // GET single promotion
            group.MapGet("/{id:int}", async (int id, GameStoreDbContext db) =>
            {
                var p = await db.Promotions.AsNoTracking()
                    .Include(pr => pr.ProductPromotions)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (p == null) return Results.NotFound();

                return Results.Ok(new PromotionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    DiscountPercent = p.DiscountPercent,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    ProductIds = p.ProductPromotions.Select(pp => pp.ProductId).ToList()
                });
            });

            // POST create promotion
            group.MapPost("/", async (CreatePromotionDto dto, GameStoreDbContext db) =>
            {
                var promo = new Promotion
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    DiscountPercent = dto.DiscountPercent,
                    StartDate = dto.StartDate.ToUniversalTime(),
                    EndDate = dto.EndDate.ToUniversalTime()
                };

                db.Promotions.Add(promo);
                await db.SaveChangesAsync();
                CacheKeys.InvalidatePromotions();

                return Results.Created($"{EndpointRouteConventions.PublicPrefix(apiPrefix)}/promotions/{promo.Id}", promo);
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // PUT update promotion
            group.MapPut("/{id:int}", async (int id, CreatePromotionDto dto, GameStoreDbContext db) =>
            {
                var promo = await db.Promotions.FindAsync(id);
                if (promo == null) return Results.NotFound();

                promo.Name = dto.Name;
                promo.Description = dto.Description;
                promo.DiscountPercent = dto.DiscountPercent;
                promo.StartDate = dto.StartDate.ToUniversalTime();
                promo.EndDate = dto.EndDate.ToUniversalTime();

                await db.SaveChangesAsync();
                CacheKeys.InvalidatePromotions();
                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // DELETE promotion
            group.MapDelete("/{id:int}", async (int id, GameStoreDbContext db) =>
            {
                var promo = await db.Promotions.Include(p => p.ProductPromotions).FirstOrDefaultAsync(p => p.Id == id);
                if (promo == null) return Results.NotFound();

                // Xóa ProductPromotions trước để tránh FK violation
                if (promo.ProductPromotions.Any())
                {
                    db.Set<ProductPromotion>().RemoveRange(promo.ProductPromotions);
                }

                db.Promotions.Remove(promo);
                await db.SaveChangesAsync();
                CacheKeys.InvalidatePromotions();
                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // POST add product to promotion
            group.MapMethods("/{id:int}/products/{productId:int}", [isV1 ? HttpMethods.Put : HttpMethods.Post], async (int id, int productId, GameStoreDbContext db) =>
            {
                var promo = await db.Promotions.FindAsync(id);
                var product = await db.Products.FindAsync(productId);

                if (promo == null || product == null) return Results.NotFound();

                var existing = await db.Set<ProductPromotion>().FirstOrDefaultAsync(pp => pp.PromotionId == id && pp.ProductId == productId);
                if (existing != null)
                {
                    return isV1
                        ? Results.NoContent()
                        : Results.BadRequest("Product already in promotion.");
                }

                var pp = new ProductPromotion
                {
                    PromotionId = id,
                    ProductId = productId
                };

                db.Set<ProductPromotion>().Add(pp);
                await db.SaveChangesAsync();
                CacheKeys.InvalidatePromotions();
                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // DELETE remove product from promotion
            group.MapDelete("/{id:int}/products/{productId:int}", async (int id, int productId, GameStoreDbContext db) =>
            {
                var existing = await db.Set<ProductPromotion>().FirstOrDefaultAsync(pp => pp.PromotionId == id && pp.ProductId == productId);
                if (existing == null) return Results.NotFound();

                db.Set<ProductPromotion>().Remove(existing);
                await db.SaveChangesAsync();
                CacheKeys.InvalidatePromotions();
                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));
        }
    }
}
