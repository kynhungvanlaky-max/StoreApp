using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameStore.API.Endpoints
{
    public static class WishlistEndpoints
    {
        public static void MapWishlistEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var isV1 = EndpointRouteConventions.IsV1(apiPrefix);
            var route = isV1 ? $"{apiPrefix}/users/me/wishlist-items" : $"{apiPrefix}/wishlist";
            var group = app.MapGroup(route).RequireAuthorization();

            group.MapGet("/", async (GameStoreDbContext db, ClaimsPrincipal user) =>
            {
                var userId = user.GetUserId();
                if (userId == null) return Results.Unauthorized();

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
                            w.Product.Type,
                            w.Product.BasePrice,
                            ImageUrl = w.Product.ProductMedia.OrderBy(m => m.DisplayOrder).FirstOrDefault() != null 
                                ? w.Product.ProductMedia.OrderBy(m => m.DisplayOrder).First().MediaUrl 
                                : null,
                            Categories = w.Product.ProductCategories.Select(pc => pc.Category.Name).ToList()
                        }
                    })
                    .ToListAsync();

                return Results.Ok(wishlists);
            });

            group.MapMethods("/{productId:int}", [isV1 ? HttpMethods.Put : HttpMethods.Post], async (int productId, GameStoreDbContext db, ClaimsPrincipal user) =>
            {
                var userId = user.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var exists = await db.Wishlists.AnyAsync(w => w.UserId == userId && w.ProductId == productId);
                if (exists)
                {
                    return isV1
                        ? Results.NoContent()
                        : Results.BadRequest("Product already in wishlist.");
                }

                var productExists = await db.Products.AnyAsync(p => p.Id == productId);
                if (!productExists) return Results.NotFound("Product not found.");

                var wishlist = new Wishlist
                {
                    UserId = userId.Value,
                    ProductId = productId,
                    AddedDate = DateTime.UtcNow
                };

                db.Wishlists.Add(wishlist);
                await db.SaveChangesAsync();

                return isV1
                    ? Results.NoContent()
                    : Results.Ok(new { message = "Added to wishlist" });
            });

            group.MapDelete("/{productId:int}", async (int productId, GameStoreDbContext db, ClaimsPrincipal user) =>
            {
                var userId = user.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var wishlist = await db.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
                if (wishlist == null) return Results.NotFound("Not in wishlist.");

                db.Wishlists.Remove(wishlist);
                await db.SaveChangesAsync();

                return Results.NoContent();
            });

            var checkRoute = isV1 ? "/{productId:int}" : "/check/{productId:int}";
            group.MapGet(checkRoute, async (int productId, GameStoreDbContext db, ClaimsPrincipal user) =>
            {
                var userId = user.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var exists = await db.Wishlists.AnyAsync(w => w.UserId == userId && w.ProductId == productId);
                return Results.Ok(new { isWishlisted = exists });
            });
        }
    }
}
