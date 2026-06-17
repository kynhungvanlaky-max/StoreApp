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
        }
    }
}
