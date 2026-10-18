using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameStore.API.Endpoints
{
    public class PaymentConfigDto
    {
        public string GatewayName { get; set; } = string.Empty;
        public string ConfigKey { get; set; } = string.Empty;
        public string ConfigValue { get; set; } = string.Empty;
    }

    public static class PaymentGatewayEndpoints
    {
        public static void MapPaymentGatewayEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var isV1 = EndpointRouteConventions.IsV1(apiPrefix);
            var resourceName = isV1 ? "payment-configurations" : "payment-configs";
            var group = app.MapGroup($"{apiPrefix}/{resourceName}")
                .RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // Lấy tất cả cấu hình (đã che thông tin nhạy cảm)
            group.MapGet("/", async (HttpContext httpContext, GameStoreDbContext db) =>
            {
                var configs = await db.PaymentGatewayConfigs.AsNoTracking().ToListAsync();
                var masked = configs.Select(c => new PaymentGatewayConfig
                {
                    Id = c.Id,
                    GatewayName = c.GatewayName,
                    ConfigKey = c.ConfigKey,
                    ConfigValue = MaskValue(c.ConfigKey, c.ConfigValue),
                    UpdatedAt = c.UpdatedAt
                }).ToList();
                return Results.Ok(masked);
            });

            // Cập nhật cấu hình
            group.MapMethods("/", [isV1 ? HttpMethods.Put : HttpMethods.Post], async (List<PaymentConfigDto> req, HttpContext httpContext, GameStoreDbContext db) =>
            {
                foreach (var dto in req)
                {
                    if (dto.ConfigValue.Contains("****"))
                    {
                        // Giữ nguyên giá trị cũ nếu client gửi lên chuỗi đã che
                        continue;
                    }

                    var config = await db.PaymentGatewayConfigs
                        .FirstOrDefaultAsync(c => c.GatewayName == dto.GatewayName && c.ConfigKey == dto.ConfigKey);
                    
                    if (config != null)
                    {
                        config.ConfigValue = dto.ConfigValue;
                        config.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        db.PaymentGatewayConfigs.Add(new PaymentGatewayConfig
                        {
                            GatewayName = dto.GatewayName,
                            ConfigKey = dto.ConfigKey,
                            ConfigValue = dto.ConfigValue,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
                
                await db.SaveChangesAsync();
                return Results.NoContent();
            });
        }

        private static bool IsSensitiveKey(string key)
        {
            return key.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
                   key.Contains("Key", StringComparison.OrdinalIgnoreCase) ||
                   key.Contains("Password", StringComparison.OrdinalIgnoreCase);
        }

        private static string MaskValue(string key, string? value)
        {
            if (string.IsNullOrEmpty(value) || !IsSensitiveKey(key))
                return value ?? string.Empty;
            if (value.Length <= 4)
                return "****";
            return new string('*', Math.Max(4, value.Length - 4)) + value[^4..];
        }
    }
}
