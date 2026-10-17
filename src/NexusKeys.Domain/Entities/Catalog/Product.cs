using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.API.Entities
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int DeveloperId { get; set; }
        public string Type { get; set; } = null!;
        public string Pricing { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = null!;
        public DateTime ReleaseDate { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey("DeveloperId")]
        public User Developer { get; set; } = null!;

        public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
        public ICollection<ProductPromotion> ProductPromotions { get; set; } = new List<ProductPromotion>();
        public ICollection<Release> Releases { get; set; } = new List<Release>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<License> Licenses { get; set; } = new List<License>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<ProductMedia> ProductMedia { get; set; } = new List<ProductMedia>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<ProductKey> ProductKeys { get; set; } = new List<ProductKey>();
        public ICollection<SystemRequirement> SystemRequirements { get; set; } = new List<SystemRequirement>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
