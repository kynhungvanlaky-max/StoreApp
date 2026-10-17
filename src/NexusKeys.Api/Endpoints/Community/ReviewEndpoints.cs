using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace GameStore.API.Endpoints
{
    public class ReviewCommentDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ReviewDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsPositive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ReviewCommentDto> Replies { get; set; } = new();
    }

    public class CreateReviewDto
    {
        public int ProductId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsRecommended { get; set; }
    }

    public class EditReviewDto
    {
        public string? Content { get; set; }
        public bool? IsRecommended { get; set; }
        public bool? IsHidden { get; set; }
    }

    public class ReplyReviewDto
    {
        public string Content { get; set; } = string.Empty;
    }

    public static class ReviewEndpoints
    {
        public static void MapReviewEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var isV1 = EndpointRouteConventions.IsV1(apiPrefix);
            var group = app.MapGroup($"{apiPrefix}/reviews");
            var productGroup = isV1 ? app.MapGroup($"{apiPrefix}/products") : group;
            var commentGroup = isV1 ? app.MapGroup($"{apiPrefix}/review-comments") : group;

            // GET reviews for a product
            productGroup.MapGet(isV1 ? "/{productId:int}/reviews" : "/product/{productId:int}", async (int productId, GameStoreDbContext db) =>
            {
                var reviews = await db.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Comments).ThenInclude(c => c.User)
                    .Where(r => r.ProductId == productId && !r.IsHidden)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReviewDto
                    {
                        Id = r.Id,
                        ProductId = r.ProductId,
                        Username = r.User.Username,
                        Content = r.Content,
                        IsPositive = r.IsRecommended,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        Replies = r.Comments.Where(c => !c.IsHidden).Select(c => new ReviewCommentDto
                        {
                            Id = c.Id,
                            Username = c.User.Username,
                            Content = c.Content,
                            CreatedAt = c.CreatedAt
                        }).OrderBy(c => c.CreatedAt).ToList()
                    })
                    .ToListAsync();

                return Results.Ok(reviews);
            });

            // CREATE a review
            productGroup.MapPost(isV1 ? "/{productId:int}/reviews" : "/", async (int? productId, CreateReviewDto dto, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var userId = httpContext.User.GetUserId();
                if (userId == null) return Results.Unauthorized();
                var targetProductId = productId ?? dto.ProductId;

                // 1. Verify if user has purchased the product
                var hasPurchased = await db.Orders
                    .Include(o => o.OrderItems)
                    .AnyAsync(o => o.UserId == userId && o.Status == "Completed" && o.OrderItems.Any(oi => oi.ProductId == targetProductId));

                if (!hasPurchased)
                {
                    return Results.Json(new { message = "Bạn cần mua sản phẩm để đánh giá" }, statusCode: 403);
                }

                // 2. Check if user already reviewed this product
                var existingReview = await db.Reviews.FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == targetProductId);
                if (existingReview != null)
                {
                    return Results.Json(new { message = "Bạn đã đánh giá sản phẩm này" }, statusCode: 409);
                }

                var review = new Review
                {
                    ProductId = targetProductId,
                    UserId = userId.Value,
                    Content = dto.Content,
                    IsRecommended = dto.IsRecommended,
                    CreatedAt = DateTime.UtcNow
                };

                db.Reviews.Add(review);
                await db.SaveChangesAsync();

                return Results.Created($"{EndpointRouteConventions.PublicPrefix(apiPrefix)}/reviews/{review.Id}", new { review.Id });
            }).RequireAuthorization();

            // EDIT a review
            group.MapMethods("/{id:int}", [isV1 ? HttpMethods.Patch : HttpMethods.Put], async (int id, EditReviewDto dto, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var userId = httpContext.User.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var review = await db.Reviews.FindAsync(id);
                if (review == null) return Results.NotFound();
                var isAdmin = httpContext.User.IsInRole("Administrator");
                if (review.UserId != userId && !isAdmin) return Results.Forbid();
                if (dto.IsHidden.HasValue && !isAdmin) return Results.Forbid();

                if (dto.Content is not null) review.Content = dto.Content;
                if (dto.IsRecommended.HasValue) review.IsRecommended = dto.IsRecommended.Value;
                if (dto.IsHidden.HasValue) review.IsHidden = dto.IsHidden.Value;
                review.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization();

            // REPLY to a review
            group.MapPost("/{id:int}/comments", async (int id, ReplyReviewDto dto, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var userId = httpContext.User.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var review = await db.Reviews.FindAsync(id);
                if (review == null) return Results.NotFound();

                var comment = new ReviewComment
                {
                    ReviewId = id,
                    UserId = userId.Value,
                    Content = dto.Content,
                    CreatedAt = DateTime.UtcNow
                };

                db.ReviewComments.Add(comment);
                await db.SaveChangesAsync();
                return Results.Created($"{EndpointRouteConventions.PublicPrefix(apiPrefix)}/review-comments/{comment.Id}", new { comment.Id });
            }).RequireAuthorization();


            if (!isV1)
            {
                group.MapPut("/{id:int}/hide", async (int id, GameStoreDbContext db) =>
                {
                    var review = await db.Reviews.FindAsync(id);
                    if (review == null) return Results.NotFound();
                    review.IsHidden = !review.IsHidden;
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));
            }

            // ADMIN: Delete review
            group.MapDelete("/{id:int}", async (int id, GameStoreDbContext db) =>
            {
                var review = await db.Reviews.Include(r => r.Comments).FirstOrDefaultAsync(r => r.Id == id);
                if (review == null) return Results.NotFound();

                // Xóa comments trước để tránh FK violation (DB dùng Restrict)
                if (review.Comments.Any())
                {
                    db.ReviewComments.RemoveRange(review.Comments);
                }
                
                db.Reviews.Remove(review);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));

            if (!isV1)
            {
                group.MapPut("/comments/{id:int}/hide", async (int id, GameStoreDbContext db) =>
                {
                    var comment = await db.ReviewComments.FindAsync(id);
                    if (comment == null) return Results.NotFound();
                    comment.IsHidden = !comment.IsHidden;
                    await db.SaveChangesAsync();
                    return Results.Ok();
                }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));
            }
            else
            {
                commentGroup.MapPatch("/{id:int}", async (int id, UpdateReviewCommentDto dto, GameStoreDbContext db) =>
                {
                    var comment = await db.ReviewComments.FindAsync(id);
                    if (comment == null) return Results.NotFound();
                    comment.IsHidden = dto.IsHidden;
                    await db.SaveChangesAsync();
                    return Results.NoContent();
                }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));
            }

            // ADMIN: Delete review comment
            commentGroup.MapDelete(isV1 ? "/{id:int}" : "/comments/{id:int}", async (int id, GameStoreDbContext db) =>
            {
                var comment = await db.ReviewComments.FindAsync(id);
                if (comment == null) return Results.NotFound();
                
                db.ReviewComments.Remove(comment);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));

            // ADMIN: Get all reviews with pagination
            group.MapGet(isV1 ? "/" : "/admin/all", async (GameStoreDbContext db, int page = 1, int pageSize = 10) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);
                var query = db.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Comments).ThenInclude(c => c.User)
                    .OrderByDescending(r => r.CreatedAt);

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var reviews = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ReviewDto
                    {
                        Id = r.Id,
                        ProductId = r.ProductId,
                        Username = r.User.Username,
                        Content = r.Content,
                        IsPositive = r.IsRecommended,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        Replies = r.Comments.Where(c => !c.IsHidden).Select(c => new ReviewCommentDto
                        {
                            Id = c.Id,
                            Username = c.User.Username,
                            Content = c.Content,
                            CreatedAt = c.CreatedAt
                        }).OrderBy(c => c.CreatedAt).ToList()
                    })
                    .ToListAsync();

                return Results.Ok(new
                {
                    Data = reviews,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                });
            }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));
        }
    }

    public class UpdateReviewCommentDto
    {
        public bool IsHidden { get; set; }
    }
}
