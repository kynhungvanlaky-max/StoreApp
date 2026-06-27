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
        public decimal BasePrice { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CheckWishlistResponse
    {
        public bool IsWishlisted { get; set; }
    }
}
