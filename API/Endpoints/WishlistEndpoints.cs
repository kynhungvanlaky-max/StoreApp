using GameStore.API;
using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameStore.API.Endpoints
{
    public static class WishlistEndpoints
    {
        public static void MapWishlistEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/wishlist").RequireAuthorization();

            group.MapGet("/", async (GameStoreDbContext db, ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                    return Results.Unauthorized();

                var wishlists = await db.Wishlists
                    .Include(w => w.Product)
                    .ThenInclude(p => p.ProductMedia)
                    .Where(w => w.UserId == userId)
                    .Select(w => new
                    {
                        w.ProductId,
                        w.AddedDate,
                        Product = new
                        {
                            w.Product.Id,
                            w.Product.Name,
                            w.Product.BasePrice,
                            ImageUrl = w.Product.ProductMedia.OrderBy(m => m.DisplayOrder).FirstOrDefault() != null 
                                ? w.Product.ProductMedia.OrderBy(m => m.DisplayOrder).First().MediaUrl 
                                : null
                        }
                    })
                    .ToListAsync();

                return Results.Ok(wishlists);
            });

            group.MapPost("/{productId}", async (int productId, GameStoreDbContext db, ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                    return Results.Unauthorized();

                var exists = await db.Wishlists.AnyAsync(w => w.UserId == userId && w.ProductId == productId);
                if (exists) return Results.BadRequest("Product already in wishlist.");

                var productExists = await db.Products.AnyAsync(p => p.Id == productId);
                if (!productExists) return Results.NotFound("Product not found.");

                var wishlist = new Wishlist
                {
                    UserId = userId,
                    ProductId = productId,
                    AddedDate = DateTime.UtcNow
                };

                db.Wishlists.Add(wishlist);
                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Added to wishlist" });
            });

            group.MapDelete("/{productId}", async (int productId, GameStoreDbContext db, ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                    return Results.Unauthorized();

                var wishlist = await db.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
                if (wishlist == null) return Results.NotFound("Not in wishlist.");

                db.Wishlists.Remove(wishlist);
                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Removed from wishlist" });
            });

            group.MapGet("/check/{productId}", async (int productId, GameStoreDbContext db, ClaimsPrincipal user) =>
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                    return Results.Unauthorized();

                var exists = await db.Wishlists.AnyAsync(w => w.UserId == userId && w.ProductId == productId);
                return Results.Ok(new { isWishlisted = exists });
            });
        }
    }
}
