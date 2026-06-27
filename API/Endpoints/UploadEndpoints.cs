using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GameStore.API.Endpoints
{
    public static class UploadEndpoints
    {
        public static void MapUploadEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/upload");

            group.MapPost("/", async (HttpRequest request) =>
            {
                if (!request.HasFormContentType)
                    return Results.BadRequest("Request must be multipart/form-data");

                var form = await request.ReadFormAsync();
                var files = form.Files;

                if (files.Count == 0)
                    return Results.BadRequest("No files were uploaded.");

                var uploadDir = @"D:\DA\MediaStorage";
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var uploadedUrls = new System.Collections.Generic.List<string>();

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var extension = Path.GetExtension(file.FileName);
                        var newFileName = $"{Guid.CreateVersion7()}{extension}";
                        var filePath = Path.Combine(uploadDir, newFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Trả về URL của API Server Static Files
                        uploadedUrls.Add($"http://localhost:5021/media/{newFileName}");
                    }
                }

                return Results.Ok(new { urls = uploadedUrls });
            })
            // Configure upload sizes if needed
            .DisableAntiforgery();

            // GET list of uploaded files
            group.MapGet("/", () =>
            {
                var uploadDir = @"D:\DA\MediaStorage";
                if (!Directory.Exists(uploadDir)) return Results.Ok(new string[0]);

                var files = Directory.GetFiles(uploadDir)
                    .Select(f => new 
                    {
                        FileName = Path.GetFileName(f),
                        Url = $"http://localhost:5021/media/{Path.GetFileName(f)}",
                        Size = new FileInfo(f).Length,
                        CreatedAt = new FileInfo(f).CreationTime
                    })
                    .OrderByDescending(f => f.CreatedAt)
                    .ToList();

                return Results.Ok(files);
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // DELETE file
            group.MapDelete("/{fileName}", (string fileName) =>
            {
                var uploadDir = @"D:\DA\MediaStorage";
                var filePath = Path.Combine(uploadDir, fileName);
                
                // Prevent directory traversal
                if (filePath != Path.GetFullPath(filePath) || !filePath.StartsWith(uploadDir))
                {
                    return Results.BadRequest("Invalid file path.");
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return Results.NoContent();
                }

                return Results.NotFound();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));
        }
    }
}
