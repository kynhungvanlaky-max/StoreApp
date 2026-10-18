using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace GameStore.API.Endpoints
{
    public static class NewsEndpoints
    {
        public static void MapNewsEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var group = app.MapGroup($"{apiPrefix}/news");

            group.MapGet("/", async (GameStoreDbContext db, [FromServices] IMemoryCache? cache, [FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                string cacheKey = $"news_list_v{CacheKeys.NewsVersion}_{page}_{pageSize}";
                if (cache != null && cache.TryGetValue(cacheKey, out object? cachedNews) && cachedNews != null)
                {
                    return Results.Ok(cachedNews);
                }

                var query = db.NewsArticles.AsNoTracking()
                    .Include(n => n.Author)
                    .Where(n => n.IsPublished)
                    .OrderByDescending(n => n.PublishedAt);

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var news = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new
                    {
                        n.Id,
                        n.Title,
                        n.Summary,
                        n.CoverImageUrl,
                        n.PublishedAt,
                        AuthorName = n.Author.Username
                    })
                    .ToListAsync();
                
                var result = new
                {
                    Data = news,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                cache?.Set(cacheKey, result, TimeSpan.FromMinutes(5));
                return Results.Ok(result);
            });

            group.MapGet("/{id:int}", async (int id, GameStoreDbContext db, [FromServices] IMemoryCache? cache) =>
            {
                string cacheKey = $"news_detail_v{CacheKeys.NewsVersion}_{id}";
                if (cache != null && cache.TryGetValue(cacheKey, out object? cachedArticle) && cachedArticle != null)
                {
                    return Results.Ok(cachedArticle);
                }

                var article = await db.NewsArticles.AsNoTracking()
                    .Include(n => n.Author)
                    .Where(n => n.Id == id && n.IsPublished)
                    .Select(n => new
                    {
                        n.Id,
                        n.Title,
                        n.Summary,
                        n.Content,
                        n.CoverImageUrl,
                        n.PublishedAt,
                        AuthorName = n.Author.Username,
                        AuthorAvatar = n.Author.AvatarUrl
                    })
                    .FirstOrDefaultAsync();

                if (article == null) return Results.NotFound();
                
                cache?.Set(cacheKey, article, TimeSpan.FromMinutes(5));
                return Results.Ok(article);
            });

            var sanitizer = new Ganss.Xss.HtmlSanitizer();

            group.MapPost("/", async (CreateNewsDto dto, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var authorId = httpContext.User.GetUserId() ?? 1;
                if (authorId == 0)
                {
                    var firstUser = await db.Users.FirstOrDefaultAsync();
                    if (firstUser != null) authorId = firstUser.Id;
                }

                var news = new NewsArticle
                {
                    Title = dto.Title,
                    Summary = dto.Summary,
                    Content = sanitizer.Sanitize(dto.Content ?? string.Empty),
                    CoverImageUrl = dto.CoverImageUrl,
                    IsPublished = true,
                    PublishedAt = DateTime.UtcNow,
                    AuthorId = authorId
                };

                db.NewsArticles.Add(news);
                await db.SaveChangesAsync();
                CacheKeys.InvalidateNews();

                return Results.Created($"{EndpointRouteConventions.PublicPrefix(apiPrefix)}/news/{news.Id}", news);
            }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));

            group.MapPut("/{id:int}", async (int id, UpdateNewsDto dto, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var news = await db.NewsArticles.FindAsync(id);
                if (news == null) return Results.NotFound();

                news.Title = dto.Title;
                news.Summary = dto.Summary;
                news.Content = sanitizer.Sanitize(dto.Content ?? string.Empty);
                news.CoverImageUrl = dto.CoverImageUrl;

                await db.SaveChangesAsync();
                CacheKeys.InvalidateNews();
                return Results.Ok(news);
            }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));

            group.MapDelete("/{id:int}", async (int id, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var news = await db.NewsArticles.FindAsync(id);
                if (news == null) return Results.NotFound();

                db.NewsArticles.Remove(news);
                await db.SaveChangesAsync();
                CacheKeys.InvalidateNews();
                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));
        }
    }

    public class CreateNewsDto
    {
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
    }

    public class UpdateNewsDto
    {
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
    }
}
