using System;

namespace Frontend.Models.Wishlist
{
    public class WishlistItem
    {
        public int ProductId { get; set; }
        public DateTime AddedDate { get; set; }
        public WishlistProduct Product { get; set; } = new();
    }

    public class WishlistProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public decimal BasePrice { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int? DiscountPercent { get; set; }
        public string? ImageUrl { get; set; }
        public List<string> Categories { get; set; } = new();
    }

    public class CheckWishlistResponse
    {
        public bool IsWishlisted { get; set; }
    }
}
