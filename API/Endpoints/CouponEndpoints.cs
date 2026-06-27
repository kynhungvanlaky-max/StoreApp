using GameStore.API.Data;
using GameStore.API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameStore.API.Endpoints
{
    public class CouponDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public int MaxUsage { get; set; }
        public int CurrentUsage { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCouponDto
    {
        public string Code { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public int MaxUsage { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class ApplyCouponDto
    {
        public string Code { get; set; } = string.Empty;
    }

    public static class CouponEndpoints
    {
        public static void MapCouponEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/coupons");

            // GET all coupons (Admin)
            group.MapGet("/", async (GameStoreDbContext db) =>
            {
                var coupons = await db.Coupons.OrderByDescending(c => c.CreatedAt).Select(c => new CouponDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    DiscountPercent = c.DiscountPercent,
                    MaxUsage = c.MaxUsage,
                    CurrentUsage = c.CurrentUsage,
                    ExpiryDate = c.ExpiryDate,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                }).ToListAsync();
                return Results.Ok(coupons);
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // POST create coupon (Admin)
            group.MapPost("/", async (CreateCouponDto dto, GameStoreDbContext db) =>
            {
                if (await db.Coupons.AnyAsync(c => c.Code == dto.Code.ToUpper()))
                {
                    return Results.BadRequest("Mã giảm giá đã tồn tại.");
                }

                var coupon = new Coupon
                {
                    Code = dto.Code.ToUpper(),
                    DiscountPercent = dto.DiscountPercent,
                    MaxUsage = dto.MaxUsage,
                    ExpiryDate = dto.ExpiryDate.ToUniversalTime(),
                    IsActive = dto.IsActive
                };
                db.Coupons.Add(coupon);
                await db.SaveChangesAsync();
                return Results.Ok(coupon);
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // PUT update coupon (Admin)
            group.MapPut("/{id}", async (int id, CreateCouponDto dto, GameStoreDbContext db) =>
            {
                var coupon = await db.Coupons.FindAsync(id);
                if (coupon == null) return Results.NotFound();
                
                coupon.Code = dto.Code.ToUpper();
                coupon.DiscountPercent = dto.DiscountPercent;
                coupon.MaxUsage = dto.MaxUsage;
                coupon.ExpiryDate = dto.ExpiryDate.ToUniversalTime();
                coupon.IsActive = dto.IsActive;

                await db.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // DELETE coupon (Admin)
            group.MapDelete("/{id}", async (int id, GameStoreDbContext db) =>
            {
                var coupon = await db.Coupons.FindAsync(id);
                if (coupon == null) return Results.NotFound();

                db.Coupons.Remove(coupon);
                await db.SaveChangesAsync();
                return Results.NoContent();
            }).RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // POST apply coupon (User)
            group.MapPost("/apply", async (ApplyCouponDto dto, GameStoreDbContext db) =>
            {
                var code = dto.Code.ToUpper();
                var coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == code);
                
                if (coupon == null) return Results.BadRequest(new { message = "Mã giảm giá không tồn tại." });
                if (!coupon.IsActive) return Results.BadRequest(new { message = "Mã giảm giá đã bị khóa." });
                if (coupon.ExpiryDate < DateTime.UtcNow) return Results.BadRequest(new { message = "Mã giảm giá đã hết hạn." });
                if (coupon.CurrentUsage >= coupon.MaxUsage && coupon.MaxUsage > 0) return Results.BadRequest(new { message = "Mã giảm giá đã hết lượt sử dụng." });

                return Results.Ok(new { discountPercent = coupon.DiscountPercent, couponId = coupon.Id });
            });
        }
    }
}
