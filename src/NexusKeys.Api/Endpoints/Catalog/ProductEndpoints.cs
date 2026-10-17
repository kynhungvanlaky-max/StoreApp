using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GameStore.API.Endpoints
{
    public static class ProductEndpoints
    {
        public static void MapProductEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var group = app.MapGroup($"{apiPrefix}/products");

            // GET all products with pagination and search
            group.MapGet("/", async (GameStoreDbContext db,
                [FromServices] IMemoryCache? cache,
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
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                string catKey = categories != null ? string.Join(",", categories) : string.Empty;
                string priceKey = priceRanges != null ? string.Join(",", priceRanges) : string.Empty;
                string cacheKey = $"products_v{CacheKeys.ProductVersion}_{page}_{pageSize}_{search}_{catKey}_{priceKey}_{sortBy}_{status}_{pricing}_{type}";

                if (cache != null && cache.TryGetValue(cacheKey, out object? cachedResult) && cachedResult != null)
                {
                    return Results.Ok(cachedResult);
                }

                var query = db.Products.AsNoTracking().AsQueryable();

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
                    if (string.Equals(type, "App", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(type, "Software", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(p => p.Type == "Software" || p.Type == "App" ||
                            ((p.Type == null || p.Type == "") && p.ProductCategories.Any(pc => 
                                pc.Category.Name == "Phần mềm" ||
                                pc.Category.Name == "Software" ||
                                pc.Category.Name == "Ứng dụng" ||
                                pc.Category.Name == "Đồ họa & Thiết kế 3D" ||
                                pc.Category.Name == "Biên tập Video & Âm thanh" ||
                                pc.Category.Name == "Lập trình & Tiện ích" ||
                                pc.Category.Name == "Công cụ bản quyền")));
                    }
                    else if (string.Equals(type, "Game", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(p => p.Type == "Game" || 
                            ((p.Type == null || p.Type == "") && !p.ProductCategories.Any(pc => 
                                pc.Category.Name == "Phần mềm" ||
                                pc.Category.Name == "Software" ||
                                pc.Category.Name == "Ứng dụng" ||
                                pc.Category.Name == "Đồ họa & Thiết kế 3D" ||
                                pc.Category.Name == "Biên tập Video & Âm thanh" ||
                                pc.Category.Name == "Lập trình & Tiện ích" ||
                                pc.Category.Name == "Công cụ bản quyền" ||
                                pc.Category.Name == "Thẻ quà tặng")));
                    }
                    else if (string.Equals(type, "GiftCard", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(type, "Gift Card", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(p => p.Type == "GiftCard" ||
                            ((p.Type == null || p.Type == "") && p.ProductCategories.Any(pc => pc.Category.Name == "Thẻ quà tặng")));
                    }
                    else
                    {
                        query = query.Where(p => p.Type == type);
                    }
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim();
                    var sLower = s.ToLower();
                    query = query.Where(p => 
                        p.Name.Contains(s) || 
                        p.Description.Contains(s) ||
                        p.Name.ToLower().Contains(sLower) ||
                        p.Description.ToLower().Contains(sLower));
                }

                if (categories != null && categories.Length > 0)
                {
                    var catClean = categories.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList();
                    if (catClean.Any())
                    {
                        var expandedList = new HashSet<string>(catClean, StringComparer.OrdinalIgnoreCase);
                        if (expandedList.Contains("Nhập vai")) expandedList.Add("Nhập vai (RPG)");
                        if (expandedList.Contains("Nhập vai (RPG)")) expandedList.Add("Nhập vai");
                        if (expandedList.Contains("Nhập vai RPG")) { expandedList.Add("Nhập vai (RPG)"); expandedList.Add("Nhập vai"); }
                        if (expandedList.Contains("RPG")) expandedList.Add("Nhập vai (RPG)");
                        if (expandedList.Contains("Action")) expandedList.Add("Hành động");
                        if (expandedList.Contains("Adventure")) expandedList.Add("Phiêu lưu");
                        if (expandedList.Contains("Open World")) expandedList.Add("Thế giới mở");
                        if (expandedList.Contains("Shooter")) expandedList.Add("Bắn súng");
                        if (expandedList.Contains("Strategy")) expandedList.Add("Chiến thuật");
                        if (expandedList.Contains("Sports")) expandedList.Add("Thể thao");
                        if (expandedList.Contains("Simulation")) expandedList.Add("Mô phỏng");
                        if (expandedList.Contains("Survival")) expandedList.Add("Sinh tồn");
                        if (expandedList.Contains("Horror")) expandedList.Add("Kinh dị");
                        if (expandedList.Contains("Racing")) expandedList.Add("Đua xe");

                        bool hasGraphicCat = expandedList.Contains("Đồ họa & Thiết kế 3D");
                        bool hasVideoCat = expandedList.Contains("Biên tập Video & Âm thanh");
                        bool hasCodeCat = expandedList.Contains("Lập trình & Tiện ích");
                        bool hasToolCat = expandedList.Contains("Công cụ bản quyền");

                        var searchArray = expandedList.Concat(expandedList.Select(x => x.ToLower())).Distinct().ToArray();
                        query = query.Where(p => 
                            p.ProductCategories.Any(pc => searchArray.Contains(pc.Category.Name))
                            || (hasGraphicCat && (p.Name.Contains("Photoshop") || p.Name.Contains("Blender") || p.Name.Contains("AutoCAD") || p.Name.Contains("Substance") || p.Name.Contains("3D Modeler") || p.Name.Contains("Aseprite")))
                            || (hasVideoCat && (p.Name.Contains("Premiere") || p.Name.Contains("Video") || p.Name.Contains("Music") || p.Name.Contains("FL Studio") || p.Name.Contains("VEGAS")))
                            || (hasCodeCat && (p.Name.Contains("DevStudio") || p.Name.Contains("Visual Studio") || p.Name.Contains("JetBrains") || p.Name.Contains("Wallpaper") || p.Name.Contains("Windows") || p.Name.Contains("Code") || p.Name.Contains("IDE") || p.Name.Contains("Lập trình")))
                            || (hasToolCat && (p.Type == "Software" || p.Type == "App") && (p.ProductCategories.Any(pc => pc.Category.Name == "Công cụ bản quyền") || p.Name.Contains("DevStudio") || p.Name.Contains("Photoshop") || p.Name.Contains("Visual Studio") || p.Name.Contains("AutoCAD") || p.Name.Contains("Windows") || p.Name.Contains("Music Studio")))
                        );
                    }
                }

                if (priceRanges != null && priceRanges.Length > 0)
                {
                    query = query.Where(p => 
                        (priceRanges.Contains("under_100k") && p.BasePrice < 100000) ||
                        (priceRanges.Contains("100k_500k") && p.BasePrice >= 100000 && p.BasePrice <= 500000) ||
                        (priceRanges.Contains("over_500k") && p.BasePrice > 500000)
                    );
                }

                var now = DateTime.UtcNow;
                query = sortBy switch
                {
                    "price_asc" => query.OrderBy(p => p.BasePrice),
                    "price_desc" => query.OrderByDescending(p => p.BasePrice),
                    "discount" => query
                        .OrderByDescending(p => p.ProductPromotions
                            .Where(pp => pp.Promotion.StartDate <= now && pp.Promotion.EndDate >= now)
                            .Select(pp => (decimal?)pp.Promotion.DiscountPercent)
                            .Max() ?? 0)
                        .ThenByDescending(p => p.ReleaseDate),
                    "bestseller" => query.OrderByDescending(p => p.OrderItems.Count).ThenByDescending(p => p.ReleaseDate),
                    "rating" => query.OrderByDescending(p => p.Reviews.Count(r => r.IsRecommended && !r.IsHidden)).ThenByDescending(p => p.Reviews.Count),
                    _ => query.OrderByDescending(p => p.ReleaseDate)
                };

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var products = await query
                    .Include(p => p.Developer)
                    .Include(p => p.ProductMedia)
                    .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
                    .Include(p => p.ProductPromotions).ThenInclude(pp => pp.Promotion)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var productIds = products.Select(p => p.Id).ToList();
                var inStockIds = await db.ProductKeys
                    .Where(k => productIds.Contains(k.ProductId) && !k.IsSold)
                    .Select(k => k.ProductId)
                    .Distinct()
                    .ToListAsync();
                var inStockSet = inStockIds.ToHashSet();

                var dtos = products.Select(p => new ProductDto
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
                    DeveloperName = p.Developer?.Username ?? string.Empty,
                    ImageUrl = p.GetCoverImageUrl(),
                    Media = p.ProductMedia.Select(m => new MediaItemDto { Url = m.MediaUrl, Type = m.MediaType }).ToList(),
                    Categories = p.ProductCategories.Select(pc => pc.Category.Name).ToList(),
                    IsOutOfStock = !inStockSet.Contains(p.Id)
                }).ToList();

                var responsePayload = new
                {
                    Data = dtos,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                cache?.Set(cacheKey, responsePayload, TimeSpan.FromSeconds(60));

                return Results.Ok(responsePayload);
            });

            // GET product by id
            group.MapGet("/{id:int}", async (int id, GameStoreDbContext db, [FromServices] IMemoryCache? cache) =>
            {
                string cacheKey = $"product_detail_v{CacheKeys.ProductVersion}_{id}";
                if (cache != null && cache.TryGetValue(cacheKey, out ProductDto? cachedDto) && cachedDto != null)
                {
                    return Results.Ok(cachedDto);
                }

                var p = await db.Products
                    .Include(p => p.Developer)
                    .Include(p => p.ProductMedia)
                    .Include(p => p.ProductCategories)
                        .ThenInclude(pc => pc.Category)
                    .Include(p => p.ProductPromotions)
                        .ThenInclude(pp => pp.Promotion)
                    .Include(p => p.SystemRequirements)
                    .FirstOrDefaultAsync(x => x.Id == id);
                
                if (p == null) return Results.NotFound();

                var now = DateTime.UtcNow;
                var isOutOfStock = !await db.ProductKeys.AnyAsync(k => k.ProductId == p.Id && !k.IsSold);
                var dto = new ProductDto
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
                    DeveloperName = p.Developer?.Username ?? string.Empty,
                    ImageUrl = p.GetCoverImageUrl(),
                    Media = p.ProductMedia.Select(m => new MediaItemDto { Url = m.MediaUrl, Type = m.MediaType }).ToList(),
                    Categories = p.ProductCategories.Select(pc => pc.Category.Name).ToList(),
                    IsOutOfStock = isOutOfStock,
                    SystemRequirements = p.SystemRequirements.Select(sr => new SystemRequirementDto
                    {
                        Id = sr.Id,
                        RequirementType = sr.RequirementType,
                        TargetOS = sr.TargetOS,
                        OSVersion = sr.OSVersion,
                        Processor = sr.Processor,
                        Memory = sr.Memory,
                        Graphics = sr.Graphics,
                        Storage = sr.Storage
                    }).ToList()
                };

                cache?.Set(cacheKey, dto, TimeSpan.FromSeconds(60));
                return Results.Ok(dto);
            });

            // POST new product
            group.MapPost("/", async (CreateProductDto dto, GameStoreDbContext db) =>
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return Results.BadRequest(new { message = "Tên sản phẩm không được để trống." });
                if (dto.BasePrice < 0)
                    return Results.BadRequest(new { message = "Giá sản phẩm không được âm." });

                int developerId = dto.DeveloperId;
                if (!string.IsNullOrWhiteSpace(dto.DeveloperName))
                {
                    var existingDev = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == dto.DeveloperName.Trim().ToLower());
                    if (existingDev != null)
                    {
                        developerId = existingDev.Id;
                    }
                    else if (developerId <= 0)
                    {
                        developerId = 1;
                    }
                }
                if (developerId <= 0) developerId = 1;

                var newProduct = new Product
                {
                    Name = dto.Name,
                    Description = dto.Description ?? string.Empty,
                    Type = string.IsNullOrWhiteSpace(dto.Type) ? "Game" : dto.Type,
                    Pricing = string.IsNullOrWhiteSpace(dto.Pricing) ? "Paid" : dto.Pricing,
                    BasePrice = dto.BasePrice,
                    Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status,
                    ReleaseDate = dto.ReleaseDate == default ? DateTime.UtcNow : dto.ReleaseDate.ToUniversalTime(),
                    DeveloperId = developerId,
                    CreatedAt = DateTime.UtcNow
                };

                db.Products.Add(newProduct);
                await db.SaveChangesAsync();

                // Media
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

                // Categories
                if (dto.Categories != null && dto.Categories.Any())
                {
                    var catNames = dto.Categories.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList();
                    var matchedCats = await db.Categories.Where(c => catNames.Contains(c.Name)).ToListAsync();
                    foreach (var cat in matchedCats)
                    {
                        db.Set<ProductCategory>().Add(new ProductCategory
                        {
                            ProductId = newProduct.Id,
                            CategoryId = cat.Id
                        });
                    }
                    await db.SaveChangesAsync();
                }

                CacheKeys.InvalidateProducts();

                return Results.Created(
                    $"{EndpointRouteConventions.PublicPrefix(apiPrefix)}/products/{newProduct.Id}",
                    new { newProduct.Id });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));

            // PUT update a product
            group.MapPut("/{id:int}", async (int id, UpdateProductDto dto, GameStoreDbContext db) =>
            {
                var p = await db.Products.FindAsync(id);
                if (p == null) return Results.NotFound();

                if (string.IsNullOrWhiteSpace(dto.Name))
                    return Results.BadRequest(new { message = "Tên sản phẩm không được để trống." });
                if (dto.BasePrice < 0)
                    return Results.BadRequest(new { message = "Giá sản phẩm không được âm." });

                p.Name = dto.Name;
                p.Description = dto.Description ?? string.Empty;
                p.Type = string.IsNullOrWhiteSpace(dto.Type) ? p.Type : dto.Type;
                p.Pricing = string.IsNullOrWhiteSpace(dto.Pricing) ? p.Pricing : dto.Pricing;
                p.BasePrice = dto.BasePrice;
                p.Status = string.IsNullOrWhiteSpace(dto.Status) ? p.Status : dto.Status;
                if (dto.ReleaseDate != default)
                {
                    p.ReleaseDate = dto.ReleaseDate.ToUniversalTime();
                }

                if (!string.IsNullOrWhiteSpace(dto.DeveloperName))
                {
                    var existingDev = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == dto.DeveloperName.Trim().ToLower());
                    if (existingDev != null)
                    {
                        p.DeveloperId = existingDev.Id;
                    }
                }

                // Media
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

                // Categories
                if (dto.Categories != null)
                {
                    var existingCats = await db.Set<ProductCategory>().Where(pc => pc.ProductId == id).ToListAsync();
                    db.Set<ProductCategory>().RemoveRange(existingCats);

                    var catNames = dto.Categories.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList();
                    var matchedCats = await db.Categories.Where(c => catNames.Contains(c.Name)).ToListAsync();
                    foreach (var cat in matchedCats)
                    {
                        db.Set<ProductCategory>().Add(new ProductCategory
                        {
                            ProductId = id,
                            CategoryId = cat.Id
                        });
                    }
                }

                await db.SaveChangesAsync();
                CacheKeys.InvalidateProducts();
                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));

            // DELETE product
            group.MapDelete("/{id:int}", async (int id, GameStoreDbContext db) =>
            {
                var product = await db.Products.FindAsync(id);
                if (product == null) return Results.NotFound();

                // 1. Kiểm tra xem game đã được mua trong đơn hàng nào chưa
                var hasBeenSold = await db.OrderItems.AnyAsync(oi => oi.ProductId == id);
                if (hasBeenSold)
                {
                    return Results.Conflict(new
                    {
                        message = "Không thể xoá sản phẩm này vì đã có khách hàng mua hàng. Vui lòng chuyển trạng thái sang Ngừng Bán thay vì xoá."
                    });
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
                CacheKeys.InvalidateProducts();

                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));
        }
    }

    public class SystemRequirementDto
    {
        public int Id { get; set; }
        public string RequirementType { get; set; } = string.Empty;
        public string TargetOS { get; set; } = string.Empty;
        public string OSVersion { get; set; } = string.Empty;
        public string Processor { get; set; } = string.Empty;
        public string Memory { get; set; } = string.Empty;
        public string Graphics { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;
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
        public List<SystemRequirementDto> SystemRequirements { get; set; } = new();
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
        public string? DeveloperName { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public List<MediaItemDto> Media { get; set; } = new();
        public List<string> Categories { get; set; } = new();
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
        public string? DeveloperName { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public List<MediaItemDto> Media { get; set; } = new();
        public List<string>? Categories { get; set; }
    }
}
