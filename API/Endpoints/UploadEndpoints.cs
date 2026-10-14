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
        public static string GetUploadDirectory()
        {
            var baseDir = Directory.GetCurrentDirectory();
            var possiblePaths = new[]
            {
                Path.Combine(baseDir, "MediaStorage"),
                Path.Combine(baseDir, "..", "MediaStorage"),
                @"D:\DA\MediaStorage",
                Path.Combine(AppContext.BaseDirectory, "MediaStorage")
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    if (Directory.Exists(path)) return Path.GetFullPath(path);
                }
                catch {}
            }

            var target = Path.Combine(baseDir, "MediaStorage");
            try { Directory.CreateDirectory(target); } catch {}
            return target;
        }

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

                var uploadDir = GetUploadDirectory();
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

                        // URL tương thích cả truy cập trực tiếp port 5021 lẫn qua reverse proxy
                        var host = request.Host.HasValue ? $"{request.Scheme}://{request.Host}" : "http://localhost:5021";
                        uploadedUrls.Add($"{host}/media/{newFileName}");
                    }
                }

                return Results.Ok(new { urls = uploadedUrls });
            })
            .DisableAntiforgery();

            // GET list of uploaded files
            group.MapGet("/", (HttpRequest request) =>
            {
                var uploadDir = GetUploadDirectory();
                if (!Directory.Exists(uploadDir)) return Results.Ok(new string[0]);

                var host = request.Host.HasValue ? $"{request.Scheme}://{request.Host}" : "http://localhost:5021";
                var files = Directory.GetFiles(uploadDir)
                    .Select(f => new 
                    {
                        FileName = Path.GetFileName(f),
                        Url = $"{host}/media/{Path.GetFileName(f)}",
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
                var uploadDir = GetUploadDirectory();
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
