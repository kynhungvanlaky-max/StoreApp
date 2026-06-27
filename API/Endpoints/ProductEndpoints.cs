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
            group.MapGet("/", async (GameStoreDbContext db, 
                [FromQuery] int page = 1, 
                [FromQuery] int pageSize = 10, 
                [FromQuery] string? search = null, 
                [FromQuery] string[]? categories = null,
                [FromQuery] string[]? priceRanges = null,
                [FromQuery] string? sortBy = null,
                [FromQuery] string? status = null,
                [FromQuery] string? pricing = null,
                [FromQuery] string? type = null) =>
            {
                var query = db.Products
                    .Include(p => p.Developer)
                    .Include(p => p.ProductMedia)
                    .Include(p => p.ProductCategories)
                        .ThenInclude(pc => pc.Category)
                    .Include(p => p.ProductPromotions)
                        .ThenInclude(pp => pp.Promotion)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                if (!string.IsNullOrEmpty(pricing))
                {
                    query = query.Where(p => p.Pricing == pricing);
                }

                if (!string.IsNullOrEmpty(type))
                {
                    query = query.Where(p => p.Type == type);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
                }

                if (categories != null && categories.Length > 0)
                {
                    query = query.Where(p => p.ProductCategories.Any(pc => categories.Contains(pc.Category.Name)));
                }

                if (priceRanges != null && priceRanges.Length > 0)
                {
                    query = query.Where(p => 
                        (priceRanges.Contains("under_100k") && p.BasePrice < 100000) ||
                        (priceRanges.Contains("100k_500k") && p.BasePrice >= 100000 && p.BasePrice <= 500000) ||
                        (priceRanges.Contains("over_500k") && p.BasePrice > 500000)
                    );
                }

                query = sortBy switch
                {
                    "price_asc" => query.OrderBy(p => p.BasePrice),
                    "price_desc" => query.OrderByDescending(p => p.BasePrice),
                    "discount" => query.OrderByDescending(p => p.Pricing == "Khuyến mãi").ThenByDescending(p => p.ReleaseDate),
                    _ => query.OrderByDescending(p => p.ReleaseDate)
                };

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var now = DateTime.UtcNow;
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
                        DiscountPercent = p.GetActiveDiscountPercent(now),
                        DiscountPrice = p.GetActiveDiscountPrice(now),
                        Status = p.Status,
                        ReleaseDate = p.ReleaseDate,
                        CreatedAt = p.CreatedAt,
                        DeveloperName = p.Developer.Username,
                        ImageUrl = p.GetCoverImageUrl(),
                        Media = p.ProductMedia.Select(m => new MediaItemDto { Url = m.MediaUrl, Type = m.MediaType }).ToList(),
                        Categories = p.ProductCategories.Select(pc => pc.Category.Name).ToList(),
                        IsOutOfStock = !db.ProductKeys.Any(k => k.ProductId == p.Id && !k.IsSold)
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
                    .Include(p => p.ProductPromotions)
                        .ThenInclude(pp => pp.Promotion)
                    .FirstOrDefaultAsync(x => x.Id == id);
                
                if (p == null) return Results.NotFound();

                var now = DateTime.UtcNow;
                return Results.Ok(new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Type = p.Type,
                    Pricing = p.Pricing,
                    BasePrice = p.BasePrice,
                    DiscountPercent = p.GetActiveDiscountPercent(now),
                    DiscountPrice = p.GetActiveDiscountPrice(now),
                    Status = p.Status,
                    ReleaseDate = p.ReleaseDate,
                    CreatedAt = p.CreatedAt,
                    DeveloperName = p.Developer.Username,
                    ImageUrl = p.GetCoverImageUrl(),
                    Media = p.ProductMedia.Select(m => new MediaItemDto { Url = m.MediaUrl, Type = m.MediaType }).ToList(),
                    Categories = p.ProductCategories.Select(pc => pc.Category.Name).ToList(),
                    IsOutOfStock = !await db.ProductKeys.AnyAsync(k => k.ProductId == p.Id && !k.IsSold)
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
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

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
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // DELETE product
            group.MapDelete("/{id}", async (int id, GameStoreDbContext db) =>
            {
                var product = await db.Products.FindAsync(id);
                if (product == null) return Results.NotFound();

                // 1. Kiểm tra xem game đã được mua trong đơn hàng nào chưa
                var hasBeenSold = await db.OrderItems.AnyAsync(oi => oi.ProductId == id);
                if (hasBeenSold)
                {
                    return Results.BadRequest("Không thể xoá sản phẩm này vì đã có khách hàng mua hàng (đã tồn tại trong đơn hàng). Vui lòng chuyển trạng thái sang Ngừng Bán thay vì xoá.");
                }

                // 2. Xóa các thực thể liên quan (Cascade Delete thủ công do DB Restrict)
                var media = await db.ProductMedia.Where(m => m.ProductId == id).ToListAsync();
                db.ProductMedia.RemoveRange(media);

                var categories = await db.ProductCategories.Where(c => c.ProductId == id).ToListAsync();
                db.ProductCategories.RemoveRange(categories);

                var promos = await db.ProductPromotions.Where(p => p.ProductId == id).ToListAsync();
                db.ProductPromotions.RemoveRange(promos);

                var keys = await db.ProductKeys.Where(k => k.ProductId == id).ToListAsync();
                db.ProductKeys.RemoveRange(keys);

                var wishlists = await db.Wishlists.Where(w => w.ProductId == id).ToListAsync();
                db.Wishlists.RemoveRange(wishlists);

                var cartItems = await db.CartItems.Where(c => c.ProductId == id).ToListAsync();
                db.CartItems.RemoveRange(cartItems);

                var reviews = await db.Reviews.Where(r => r.ProductId == id).ToListAsync();
                foreach (var review in reviews)
                {
                    var comments = await db.ReviewComments.Where(rc => rc.ReviewId == review.Id).ToListAsync();
                    db.ReviewComments.RemoveRange(comments);
                }
                db.Reviews.RemoveRange(reviews);

                var sysReqs = await db.SystemRequirements.Where(s => s.ProductId == id).ToListAsync();
                db.SystemRequirements.RemoveRange(sysReqs);

                var releases = await db.Releases.Where(r => r.ProductId == id).ToListAsync();
                db.Releases.RemoveRange(releases);

                var licenses = await db.Licenses.Where(l => l.ProductId == id).ToListAsync();
                db.Licenses.RemoveRange(licenses);

                // 3. Xóa sản phẩm chính
                db.Products.Remove(product);
                await db.SaveChangesAsync();

                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));
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
        public decimal? DiscountPrice { get; set; }
        public decimal? DiscountPercent { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string DeveloperName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public List<MediaItemDto> Media { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public bool IsOutOfStock { get; set; }
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
