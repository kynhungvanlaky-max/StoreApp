using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace GameStore.API.Endpoints
{
    public static class NewsEndpoints
    {
        public static void MapNewsEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/news");

            group.MapGet("/", async (GameStoreDbContext db, [FromQuery] int page = 1, [FromQuery] int pageSize = 10) =>
            {
                var query = db.NewsArticles
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
                
                return Results.Ok(new
                {
                    Data = news,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                });
            });

            group.MapGet("/{id}", async (int id, GameStoreDbContext db) =>
            {
                var article = await db.NewsArticles
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
                
                return Results.Ok(article);
            });
            group.MapPost("/", async (CreateNewsDto dto, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var authorIdStr = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? 
                                  httpContext.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                
                int authorId = 1;
                if (!string.IsNullOrEmpty(authorIdStr) && int.TryParse(authorIdStr, out int parsedId))
                {
                    authorId = parsedId;
                }
                else
                {
                    var firstUser = await db.Users.FirstOrDefaultAsync();
                    if (firstUser != null) authorId = firstUser.Id;
                }

                var news = new NewsArticle
                {
                    Title = dto.Title,
                    Summary = dto.Summary,
                    Content = dto.Content,
                    CoverImageUrl = dto.CoverImageUrl,
                    IsPublished = true,
                    PublishedAt = DateTime.UtcNow,
                    AuthorId = authorId
                };

                db.NewsArticles.Add(news);
                await db.SaveChangesAsync();

                return Results.Ok(news);
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            group.MapPut("/{id}", async (int id, UpdateNewsDto dto, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var news = await db.NewsArticles.FindAsync(id);
                if (news == null) return Results.NotFound();

                news.Title = dto.Title;
                news.Summary = dto.Summary;
                news.Content = dto.Content;
                news.CoverImageUrl = dto.CoverImageUrl;

                await db.SaveChangesAsync();
                return Results.Ok(news);
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            group.MapDelete("/{id}", async (int id, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var news = await db.NewsArticles.FindAsync(id);
                if (news == null) return Results.NotFound();

                db.NewsArticles.Remove(news);
                await db.SaveChangesAsync();
                return Results.Ok();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));
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
