using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GameStore.API.Endpoints
{
    public static class UploadEndpoints
    {
        // Allowed file extensions for upload
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".webm" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public static void MapUploadEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var isV1 = EndpointRouteConventions.IsV1(apiPrefix);
            var resourceName = isV1 ? "media-files" : "upload";
            var group = app.MapGroup($"{apiPrefix}/{resourceName}");

            group.MapPost("/", async (HttpRequest request, IConfiguration config) =>
            {
                if (!request.HasFormContentType)
                    return Results.BadRequest("Request must be multipart/form-data");

                var form = await request.ReadFormAsync();
                var files = form.Files;

                if (files.Count == 0)
                    return Results.BadRequest("No files were uploaded.");

                var uploadDir = config["AppSettings:MediaStoragePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "MediaStorage");
                uploadDir = Path.GetFullPath(uploadDir);
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var apiBaseUrl = config["AppSettings:ApiBaseUrl"] ?? "http://localhost:5021";
                var uploadedUrls = new System.Collections.Generic.List<string>();

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        // Validate file size
                        if (file.Length > MaxFileSize)
                        {
                            return Results.BadRequest($"File '{file.FileName}' vượt quá giới hạn {MaxFileSize / 1024 / 1024}MB.");
                        }

                        // Validate file extension
                        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        if (!AllowedExtensions.Contains(extension))
                        {
                            return Results.BadRequest($"Định dạng file '{extension}' không được hỗ trợ. Chỉ cho phép: {string.Join(", ", AllowedExtensions)}");
                        }

                        var newFileName = $"{Guid.CreateVersion7()}{extension}";
                        var filePath = Path.Combine(uploadDir, newFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        uploadedUrls.Add($"{apiBaseUrl}/media/{newFileName}");
                    }
                }

                return isV1
                    ? Results.Created($"{EndpointRouteConventions.PublicPrefix(apiPrefix)}/media-files", new { urls = uploadedUrls })
                    : Results.Ok(new { urls = uploadedUrls });
            })
            .RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"))
            .DisableAntiforgery();

            // GET list of uploaded files
            group.MapGet("/", (IConfiguration config) =>
            {
                var uploadDir = config["AppSettings:MediaStoragePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "MediaStorage");
                uploadDir = Path.GetFullPath(uploadDir);
                if (!Directory.Exists(uploadDir)) return Results.Ok(new string[0]);

                var apiBaseUrl = config["AppSettings:ApiBaseUrl"] ?? "http://localhost:5021";

                var files = Directory.GetFiles(uploadDir)
                    .Select(f => new 
                    {
                        FileName = Path.GetFileName(f),
                        Url = $"{apiBaseUrl}/media/{Path.GetFileName(f)}",
                        Size = new FileInfo(f).Length,
                        CreatedAt = new FileInfo(f).CreationTime
                    })
                    .OrderByDescending(f => f.CreatedAt)
                    .ToList();

                return Results.Ok(files);
            }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));

            // DELETE file
            group.MapDelete("/{fileName}", (string fileName, IConfiguration config) =>
            {
                var uploadDir = config["AppSettings:MediaStoragePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "MediaStorage");
                uploadDir = Path.GetFullPath(uploadDir);
                var filePath = Path.Combine(uploadDir, fileName);
                
                // Prevent directory traversal
                if (Path.GetFullPath(filePath) != filePath || !Path.GetFullPath(filePath).StartsWith(uploadDir))
                {
                    return Results.BadRequest("Invalid file path.");
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return Results.NoContent();
                }

                return Results.NotFound();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator", "Developer", "Staff"));
        }
    }
}
