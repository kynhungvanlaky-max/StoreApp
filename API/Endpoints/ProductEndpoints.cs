using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStore.API.Endpoints
{
    public static class ProductEndpoints
    {
        public static void MapProductEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/products");

            // GET all products with pagination and search
            group.MapGet("/", async (GameStoreDbContext db, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null) =>
            {
                var query = db.Products
                    .Include(p => p.Developer)
                    .Include(p => p.ProductMedia)
                    .Include(p => p.ProductCategories)
                        .ThenInclude(pc => pc.Category)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Type = p.Type,
                        Pricing = p.Pricing,
                        BasePrice = p.BasePrice,
                        Status = p.Status,
                        ReleaseDate = p.ReleaseDate,
                        CreatedAt = p.CreatedAt,
                        DeveloperName = p.Developer.Username,
                        ImageUrl = p.ProductMedia.FirstOrDefault(m => m.MediaType == "Image" || m.MediaType == "Cover") != null ? p.ProductMedia.FirstOrDefault(m => m.MediaType == "Image" || m.MediaType == "Cover")!.MediaUrl : "",
                        Media = p.ProductMedia.Select(m => new MediaItemDto { Url = m.MediaUrl, Type = m.MediaType }).ToList(),
                        Categories = p.ProductCategories.Select(pc => pc.Category.Name).ToList()
                    })
                    .ToListAsync();

                return Results.Ok(new
                {
                    Data = products,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                });
            });

            // GET product by id
            group.MapGet("/{id}", async (int id, GameStoreDbContext db) =>
            {
                var p = await db.Products
                    .Include(p => p.Developer)
                    .Include(p => p.ProductMedia)
                    .Include(p => p.ProductCategories)
                        .ThenInclude(pc => pc.Category)
                    .FirstOrDefaultAsync(x => x.Id == id);
                
                if (p == null) return Results.NotFound();

                return Results.Ok(new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Type = p.Type,
                    Pricing = p.Pricing,
                    BasePrice = p.BasePrice,
                    Status = p.Status,
                    ReleaseDate = p.ReleaseDate,
                    CreatedAt = p.CreatedAt,
                    DeveloperName = p.Developer.Username,
                    ImageUrl = p.ProductMedia.FirstOrDefault(m => m.MediaType == "Image" || m.MediaType == "Cover") != null ? p.ProductMedia.FirstOrDefault(m => m.MediaType == "Image" || m.MediaType == "Cover")!.MediaUrl : "",
                    Media = p.ProductMedia.Select(m => new MediaItemDto { Url = m.MediaUrl, Type = m.MediaType }).ToList(),
                    Categories = p.ProductCategories.Select(pc => pc.Category.Name).ToList()
                });
            });

            // POST new product
            group.MapPost("/", async (CreateProductDto dto, GameStoreDbContext db) =>
            {
                var newProduct = new Product
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Type = dto.Type,
                    Pricing = dto.Pricing,
                    BasePrice = dto.BasePrice,
                    Status = dto.Status,
                    ReleaseDate = dto.ReleaseDate,
                    DeveloperId = dto.DeveloperId,
                    CreatedAt = DateTime.UtcNow
                };

                db.Products.Add(newProduct);
                await db.SaveChangesAsync();

                if (dto.Media != null && dto.Media.Any())
                {
                    int order = 1;
                    foreach (var m in dto.Media)
                    {
                        db.Set<ProductMedia>().Add(new ProductMedia
                        {
                            ProductId = newProduct.Id,
                            MediaType = m.Type,
                            MediaUrl = m.Url,
                            DisplayOrder = order++
                        });
                    }
                    await db.SaveChangesAsync();
                }
                else if (!string.IsNullOrEmpty(dto.ImageUrl))
                {
                    db.Set<ProductMedia>().Add(new ProductMedia
                    {
                        ProductId = newProduct.Id,
                        MediaType = "Image",
                        MediaUrl = dto.ImageUrl,
                        DisplayOrder = 1
                    });
                    await db.SaveChangesAsync();
                }

                return Results.Created($"/api/products/{newProduct.Id}", newProduct);
            });

            // PUT update a product
            group.MapPut("/{id}", async (int id, UpdateProductDto dto, GameStoreDbContext db) =>
            {
                var p = await db.Products.FindAsync(id);
                if (p == null) return Results.NotFound();

                p.Name = dto.Name;
                p.Description = dto.Description;
                p.Type = dto.Type;
                p.Pricing = dto.Pricing;
                p.BasePrice = dto.BasePrice;
                p.Status = dto.Status;
                p.ReleaseDate = dto.ReleaseDate;

                if (dto.Media != null && dto.Media.Any())
                {
                    var existingMedia = await db.Set<ProductMedia>().Where(m => m.ProductId == id).ToListAsync();
                    db.Set<ProductMedia>().RemoveRange(existingMedia);
                    
                    int order = 1;
                    foreach (var m in dto.Media)
                    {
                        db.Set<ProductMedia>().Add(new ProductMedia
                        {
                            ProductId = id,
                            MediaType = m.Type,
                            MediaUrl = m.Url,
                            DisplayOrder = order++
                        });
                    }
                }
                else if (!string.IsNullOrEmpty(dto.ImageUrl))
                {
                    var existingMedia = await db.Set<ProductMedia>().Where(m => m.ProductId == id).ToListAsync();
                    db.Set<ProductMedia>().RemoveRange(existingMedia);

                    db.Set<ProductMedia>().Add(new ProductMedia
                    {
                        ProductId = id,
                        MediaType = "Image",
                        MediaUrl = dto.ImageUrl,
                        DisplayOrder = 1
                    });
                }

                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            // DELETE product
            group.MapDelete("/{id}", async (int id, GameStoreDbContext db) =>
            {
                var product = await db.Products.FindAsync(id);
                if (product == null) return Results.NotFound();

                db.Products.Remove(product);
                await db.SaveChangesAsync();

                return Results.NoContent();
            });
        }
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Pricing { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string DeveloperName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public List<MediaItemDto> Media { get; set; } = new();
        public List<string> Categories { get; set; } = new();
    }

    public class MediaItemDto
    {
        public string Url { get; set; } = string.Empty;
        public string Type { get; set; } = "Image"; // "Image" or "Video"
    }

    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Pricing { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public int DeveloperId { get; set; } = 1; // Default
        public string ImageUrl { get; set; } = string.Empty;
        public List<MediaItemDto> Media { get; set; } = new();
    }

    public class UpdateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Pricing { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public List<MediaItemDto> Media { get; set; } = new();
    }
}
