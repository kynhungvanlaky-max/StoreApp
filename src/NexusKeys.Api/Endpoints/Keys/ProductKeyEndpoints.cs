using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStore.API.Endpoints
{
    public static class ProductKeyEndpoints
    {
        public static void MapProductKeyEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var isV1 = EndpointRouteConventions.IsV1(apiPrefix);
            var resourceName = isV1 ? "product-keys" : "keys";
            var group = app.MapGroup($"{apiPrefix}/{resourceName}").RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));

            group.MapGet("/", async (
                GameStoreDbContext db,
                [FromQuery] string? search = null,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 50);
                var normalizedSearch = search?.Trim();

                var filteredKeys = db.ProductKeys
                    .Include(k => k.Product)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(normalizedSearch))
                {
                    filteredKeys = filteredKeys.Where(k =>
                        k.Product.Name.Contains(normalizedSearch) ||
                        k.KeyCode.Contains(normalizedSearch));
                }

                // Phân trang theo sản phẩm để toàn bộ key của cùng một game luôn nằm cùng trang.
                var productQuery = filteredKeys
                    .Select(k => new { k.ProductId, ProductName = k.Product.Name })
                    .Distinct()
                    .OrderBy(p => p.ProductName)
                    .ThenBy(p => p.ProductId);

                var totalItems = await productQuery.CountAsync();
                var totalPages = totalItems == 0
                    ? 1
                    : (int)Math.Ceiling(totalItems / (double)pageSize);
                page = Math.Min(page, totalPages);

                var productIds = await productQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => p.ProductId)
                    .ToListAsync();

                var keys = await filteredKeys
                    .Where(k => productIds.Contains(k.ProductId))
                    .OrderBy(k => k.Product.Name)
                    .ThenBy(k => k.Id)
                    .Select(k => new ProductKeyDto
                    {
                        Id = k.Id,
                        KeyCode = k.KeyCode,
                        IsSold = k.IsSold,
                        ProductId = k.ProductId,
                        ProductName = k.Product.Name,
                        OrderId = k.OrderId
                    })
                    .ToListAsync();

                return Results.Ok(new
                {
                    Data = keys,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                });
            });

            group.MapPost("/", async (CreateProductKeyDto dto, GameStoreDbContext db) =>
            {
                var product = await db.Products.FindAsync(dto.ProductId);
                if (product == null)
                {
                    return isV1
                        ? Results.NotFound(new { message = "Product not found" })
                        : Results.BadRequest("Product not found");
                }

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

                return Results.Created($"{EndpointRouteConventions.PublicPrefix(apiPrefix)}/{resourceName}/{key.Id}", key.Id);
            });

            group.MapPut("/{id}", async (int id, UpdateProductKeyDto dto, GameStoreDbContext db) =>
            {
                var key = await db.ProductKeys.FindAsync(id);
                if (key == null) return Results.NotFound();

                var product = await db.Products.FindAsync(dto.ProductId);
                if (product == null)
                {
                    return Results.NotFound(new { message = "Sản phẩm không tồn tại." });
                }

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
                if (key.IsSold || key.OrderId.HasValue)
                {
                    return Results.Conflict(new
                    {
                        message = "Không thể xóa key đã bán hoặc đã gán cho đơn hàng."
                    });
                }

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
