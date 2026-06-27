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
        public string Content { get; set; } = string.Empty;
        public bool IsRecommended { get; set; }
    }

    public class ReplyReviewDto
    {
        public string Content { get; set; } = string.Empty;
    }

    public static class ReviewEndpoints
    {
        public static void MapReviewEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/reviews");

            // GET reviews for a product
            group.MapGet("/product/{productId}", async (int productId, GameStoreDbContext db) =>
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
                        Replies = r.Comments.Select(c => new ReviewCommentDto
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
            group.MapPost("/", async (CreateReviewDto dto, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var userId = httpContext.User.GetUserId();
                if (userId == null) return Results.Unauthorized();

                // 1. Verify if user has purchased the product
                var hasPurchased = await db.Orders
                    .Include(o => o.OrderItems)
                    .AnyAsync(o => o.UserId == userId && o.Status == "Completed" && o.OrderItems.Any(oi => oi.ProductId == dto.ProductId));

                if (!hasPurchased)
                {
                    return Results.Json(new { message = "Bạn cần mua sản phẩm để đánh giá" }, statusCode: 403);
                }

                // 2. Check if user already reviewed this product
                var existingReview = await db.Reviews.FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == dto.ProductId);
                if (existingReview != null)
                {
                    return Results.Json(new { message = "Bạn đã đánh giá sản phẩm này" }, statusCode: 409);
                }

                var review = new Review
                {
                    ProductId = dto.ProductId,
                    UserId = userId.Value,
                    Content = dto.Content,
                    IsRecommended = dto.IsRecommended,
                    CreatedAt = DateTime.UtcNow
                };

                db.Reviews.Add(review);
                await db.SaveChangesAsync();

                return Results.Ok();
            }).RequireAuthorization();

            // EDIT a review
            group.MapPut("/{id}", async (int id, EditReviewDto dto, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var userId = httpContext.User.GetUserId();
                if (userId == null) return Results.Unauthorized();

                var review = await db.Reviews.FindAsync(id);
                if (review == null) return Results.NotFound();
                if (review.UserId != userId) return Results.Forbid();

                review.Content = dto.Content;
                review.IsRecommended = dto.IsRecommended;
                review.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                return Results.Ok();
            }).RequireAuthorization();

            // REPLY to a review
            group.MapPost("/{id}/comments", async (int id, ReplyReviewDto dto, HttpContext httpContext, GameStoreDbContext db) =>
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
                return Results.Ok();
            }).RequireAuthorization();


            // ADMIN: Toggle hide
            group.MapPut("/{id}/hide", async (int id, GameStoreDbContext db) =>
            {
                var review = await db.Reviews.FindAsync(id);
                if (review == null) return Results.NotFound();
                
                review.IsHidden = !review.IsHidden;
                await db.SaveChangesAsync();
                return Results.Ok();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // ADMIN: Delete review
            group.MapDelete("/{id}", async (int id, GameStoreDbContext db) =>
            {
                var review = await db.Reviews.FindAsync(id);
                if (review == null) return Results.NotFound();
                
                db.Reviews.Remove(review);
                await db.SaveChangesAsync();
                return Results.Ok();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // ADMIN: Toggle hide review comment
            group.MapPut("/comments/{id}/hide", async (int id, GameStoreDbContext db) =>
            {
                var comment = await db.ReviewComments.FindAsync(id);
                if (comment == null) return Results.NotFound();
                
                comment.IsHidden = !comment.IsHidden;
                await db.SaveChangesAsync();
                return Results.Ok();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // ADMIN: Delete review comment
            group.MapDelete("/comments/{id}", async (int id, GameStoreDbContext db) =>
            {
                var comment = await db.ReviewComments.FindAsync(id);
                if (comment == null) return Results.NotFound();
                
                db.ReviewComments.Remove(comment);
                await db.SaveChangesAsync();
                return Results.Ok();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // ADMIN: Get all reviews with pagination
            group.MapGet("/admin/all", async (GameStoreDbContext db, int page = 1, int pageSize = 10) =>
            {
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
                        Replies = r.Comments.Select(c => new ReviewCommentDto
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
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));
        }
    }
}
