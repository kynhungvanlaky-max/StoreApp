using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GameStore.API.Data;
using GameStore.API.Entities;
using System.Linq;

namespace GameStore.API.Endpoints
{
    public static class NewsEndpoints
    {
        public static void MapNewsEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/news");

            group.MapGet("/", async (GameStoreDbContext db) =>
            {
                var news = await db.NewsArticles
                    .Include(n => n.Author)
                    .Where(n => n.IsPublished)
                    .OrderByDescending(n => n.PublishedAt)
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
                
                return Results.Ok(news);
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
                var role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role != "Administrator" && role != "Developer") return Results.Forbid();

                var authorIdStr = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? 
                                  httpContext.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                
                if (string.IsNullOrEmpty(authorIdStr) || !int.TryParse(authorIdStr, out int authorId))
                    return Results.Unauthorized();

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
            }).RequireAuthorization();

            group.MapPut("/{id}", async (int id, UpdateNewsDto dto, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role != "Administrator" && role != "Developer") return Results.Forbid();

                var news = await db.NewsArticles.FindAsync(id);
                if (news == null) return Results.NotFound();

                news.Title = dto.Title;
                news.Summary = dto.Summary;
                news.Content = dto.Content;
                news.CoverImageUrl = dto.CoverImageUrl;

                await db.SaveChangesAsync();
                return Results.Ok(news);
            }).RequireAuthorization();

            group.MapDelete("/{id}", async (int id, HttpContext httpContext, GameStoreDbContext db) =>
            {
                var role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role != "Administrator" && role != "Developer") return Results.Forbid();

                var news = await db.NewsArticles.FindAsync(id);
                if (news == null) return Results.NotFound();

                db.NewsArticles.Remove(news);
                await db.SaveChangesAsync();
                return Results.Ok();
            }).RequireAuthorization();
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
