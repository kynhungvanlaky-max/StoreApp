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

                        using var readStream = file.OpenReadStream();
                        var header = new byte[16];
                        int bytesRead = await readStream.ReadAsync(header.AsMemory(0, 16));
                        if (!IsValidFileSignature(header, bytesRead, extension))
                        {
                            return Results.BadRequest($"Nội dung file '{file.FileName}' không đúng định dạng hợp lệ.");
                        }

                        var newFileName = $"{Guid.CreateVersion7()}{extension}";
                        var filePath = Path.Combine(uploadDir, newFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            if (readStream.CanSeek)
                            {
                                readStream.Seek(0, SeekOrigin.Begin);
                            }
                            else
                            {
                                await stream.WriteAsync(header.AsMemory(0, bytesRead));
                            }
                            await readStream.CopyToAsync(stream);
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

                // Prevent directory traversal
                var safeFileName = Path.GetFileName(fileName);
                if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName != fileName || safeFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    return Results.BadRequest("Invalid file path.");
                }

                var filePath = Path.Combine(uploadDir, safeFileName);
                var fullFilePath = Path.GetFullPath(filePath);
                var fullUploadDir = uploadDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

                if (!fullFilePath.StartsWith(fullUploadDir, StringComparison.OrdinalIgnoreCase))
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

        private static bool IsValidFileSignature(byte[] header, int bytesRead, string extension)
        {
            if (bytesRead < 4) return false;

            return extension switch
            {
                ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => bytesRead >= 8 &&
                          header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                          header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
                ".gif" => header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38,
                ".webp" => bytesRead >= 12 &&
                           header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                           header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
                ".mp4" => bytesRead >= 8 &&
                          header[4] == 0x66 && header[5] == 0x74 && header[6] == 0x79 && header[7] == 0x70,
                ".webm" => header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3,
                _ => false
            };
        }
    }
}
