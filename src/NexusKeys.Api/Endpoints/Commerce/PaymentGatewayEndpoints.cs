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

            // Lấy tất cả cấu hình
            group.MapGet("/", async (HttpContext httpContext, GameStoreDbContext db) =>
            {
                var configs = await db.PaymentGatewayConfigs.ToListAsync();
                return Results.Ok(configs);
            });

            // Cập nhật cấu hình
            group.MapMethods("/", [isV1 ? HttpMethods.Put : HttpMethods.Post], async (List<PaymentConfigDto> req, HttpContext httpContext, GameStoreDbContext db) =>
            {
                foreach (var dto in req)
                {
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
    }
}
