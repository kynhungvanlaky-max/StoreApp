using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameStore.API.Endpoints
{
    public static class ProductKeyEndpoints
    {
        public static void MapProductKeyEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/keys").RequireAuthorization(policy => policy.RequireRole("Administrator"));

            group.MapGet("/", async ([AsParameters] PaginationParams pagination, GameStoreDbContext db) =>
            {
                var query = db.ProductKeys
                    .Include(k => k.Product)
                    .Select(k => new ProductKeyDto
                    {
                        Id = k.Id,
                        KeyCode = k.KeyCode,
                        IsSold = k.IsSold,
                        ProductId = k.ProductId,
                        ProductName = k.Product.Name,
                        OrderId = k.OrderId
                    });

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pagination.PageSize);

                var keys = await query
                    .Skip((pagination.Page - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                return Results.Ok(new
                {
                    Data = keys,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = pagination.Page,
                    PageSize = pagination.PageSize
                });
            });

            group.MapPost("/", async (CreateProductKeyDto dto, GameStoreDbContext db) =>
            {
                var product = await db.Products.FindAsync(dto.ProductId);
                if (product == null) return Results.BadRequest("Product not found");

                var key = new ProductKey
                {
                    ProductId = dto.ProductId,
                    KeyCode = dto.KeyCode,
                    IsSold = dto.IsSold,
                    OrderId = dto.OrderId
                };

                // Auto update IsSold if OrderId is provided
                if (key.OrderId.HasValue)
                {
                    key.IsSold = true;
                }

                db.ProductKeys.Add(key);
                await db.SaveChangesAsync();

                return Results.Created($"/api/keys/{key.Id}", key.Id);
            });

            group.MapPut("/{id}", async (int id, UpdateProductKeyDto dto, GameStoreDbContext db) =>
            {
                var key = await db.ProductKeys.FindAsync(id);
                if (key == null) return Results.NotFound();

                key.KeyCode = dto.KeyCode;
                key.ProductId = dto.ProductId;
                key.OrderId = dto.OrderId;

                // Logic: If key is assigned to an order, it is sold
                key.IsSold = key.OrderId.HasValue;

                await db.SaveChangesAsync();

                return Results.NoContent();
            });

            group.MapDelete("/{id}", async (int id, GameStoreDbContext db) =>
            {
                var key = await db.ProductKeys.FindAsync(id);
                if (key == null) return Results.NotFound();

                db.ProductKeys.Remove(key);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });
        }
    }

    public class ProductKeyDto
    {
        public int Id { get; set; }
        public string KeyCode { get; set; } = string.Empty;
        public bool IsSold { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int? OrderId { get; set; }
    }

    public class CreateProductKeyDto
    {
        public int ProductId { get; set; }
        public string KeyCode { get; set; } = string.Empty;
        public bool IsSold { get; set; }
        public int? OrderId { get; set; }
    }

    public class UpdateProductKeyDto
    {
        public int ProductId { get; set; }
        public string KeyCode { get; set; } = string.Empty;
        public int? OrderId { get; set; }
    }
}
