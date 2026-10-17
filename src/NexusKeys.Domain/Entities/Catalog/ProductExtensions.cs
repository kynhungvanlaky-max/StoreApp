using System;
using System.Linq;

public static class ProductExtensions
{
    public static decimal? GetActiveDiscountPercent(this GameStore.API.Entities.Product p, DateTime now)
    {
        return p.ProductPromotions
            .Where(pp => pp.Promotion.StartDate <= now && pp.Promotion.EndDate >= now)
            .OrderByDescending(pp => pp.Promotion.DiscountPercent)
            .Select(pp => (decimal?)pp.Promotion.DiscountPercent)
            .FirstOrDefault();
    }

    public static decimal? GetActiveDiscountPrice(this GameStore.API.Entities.Product p, DateTime now)
    {
        var discountPercent = p.GetActiveDiscountPercent(now);
        if (discountPercent == null) return null;
        return p.BasePrice * (100 - discountPercent.Value) / 100;
    }

    public static string GetCoverImageUrl(this GameStore.API.Entities.Product p)
    {
        var preferredImage = p.ProductMedia
            .Where(m => string.Equals(m.MediaType, "Cover", StringComparison.OrdinalIgnoreCase)
                || string.Equals(m.MediaType, "Image", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => string.Equals(m.MediaType, "Cover", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.MediaUrl)
            .FirstOrDefault();

        return preferredImage
            ?? p.ProductMedia.Select(m => m.MediaUrl).FirstOrDefault()
            ?? string.Empty;
    }
}
