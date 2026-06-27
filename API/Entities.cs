using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.API.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public decimal WalletBalance { get; set; }
        public int RoleId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PasswordHash { get; set; } = null!;
        public string? GoogleId { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsLocked { get; set; }

        [ForeignKey("RoleId")]
        public Role Role { get; set; } = null!;
        
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<ReviewComment> ReviewComments { get; set; } = new List<ReviewComment>();
        public ICollection<License> Licenses { get; set; } = new List<License>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Product> DevelopedProducts { get; set; } = new List<Product>();
        public ICollection<NewsArticle> WrittenArticles { get; set; } = new List<NewsArticle>();
        public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    }

    public class Role
    {
        [Key]
        public int Id { get; set; }
        public string RoleName { get; set; } = null!;
        
        public ICollection<User> Users { get; set; } = new List<User>();
    }

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

    public class Category
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public Category? Parent { get; set; }

        public ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
    }

    public class ProductCategory
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        [ForeignKey("CategoryId")]
        public Category Category { get; set; } = null!;
    }

    public class Promotion
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public ICollection<ProductPromotion> ProductPromotions { get; set; } = new List<ProductPromotion>();
    }

    public class ProductPromotion
    {
        public int ProductId { get; set; }
        public int PromotionId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        [ForeignKey("PromotionId")]
        public Promotion Promotion { get; set; } = null!;
    }

    public class Release
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string VersionNumber { get; set; } = null!;
        public string TargetOS { get; set; } = null!;
        public string DownloadUrl { get; set; } = null!;
        public string Checksum { get; set; } = null!;

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }

    public class Review
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public bool IsRecommended { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsHidden { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public ICollection<ReviewComment> Comments { get; set; } = new List<ReviewComment>();
    }

    public class ReviewComment
    {
        [Key]
        public int Id { get; set; }
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsHidden { get; set; }

        [ForeignKey("ReviewId")]
        public Review Review { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }

    public class License
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public string Type { get; set; } = null!;
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string? HardwareId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }

    public class Notification
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsRead { get; set; }
        public string Type { get; set; } = null!;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }

    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime Expires { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }

    public class Wishlist
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public DateTime AddedDate { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }

    public class ProductMedia
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string MediaType { get; set; } = null!;
        public string MediaUrl { get; set; } = null!;
        public int DisplayOrder { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }

    public class CartItem
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime AddedDate { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }

    public class ProductKey
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string KeyCode { get; set; } = null!;
        public bool IsSold { get; set; }
        public int? OrderId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }
    }

    public class SystemRequirement
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string RequirementType { get; set; } = null!;
        public string TargetOS { get; set; } = null!;
        public string OSVersion { get; set; } = null!;
        public string Processor { get; set; } = null!;
        public string Memory { get; set; } = null!;
        public string Graphics { get; set; } = null!;
        public string Storage { get; set; } = null!;

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime OrderDate { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<ProductKey> ProductKeys { get; set; } = new List<ProductKey>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public class OrderItem
    {
        [Key]
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public decimal PriceAtPurchase { get; set; }

        [ForeignKey("OrderId")]
        public Order Order { get; set; } = null!;

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }

    public class Transaction
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? OrderId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = null!;
        public string PaymentGateway { get; set; } = null!;
        public string GatewayTransactionId { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }
    }

    public class NewsArticle
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Summary { get; set; } = null!;
        public string Content { get; set; } = null!; // Will store HTML or Markdown
        public string? CoverImageUrl { get; set; }
        public int AuthorId { get; set; }
        public DateTime PublishedAt { get; set; }
        public bool IsPublished { get; set; } = true;

        [ForeignKey("AuthorId")]
        public User Author { get; set; } = null!;
    }

    public class PaymentGatewayConfig
    {
        [Key]
        public int Id { get; set; }
        public string GatewayName { get; set; } = null!; // "VNPay", "MoMo"
        public string ConfigKey { get; set; } = null!;
        public string ConfigValue { get; set; } = null!;
        public DateTime UpdatedAt { get; set; }
    }

    public class SupportTicket
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Topic { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? OrderId { get; set; }
        public string Content { get; set; } = null!;
        public string Status { get; set; } = "Pending"; // "Pending", "Resolved", "Closed"
        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }

    public class PasswordResetToken
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }

    public class Coupon
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = null!; // Mã giảm giá
        public decimal DiscountPercent { get; set; }
        public int MaxUsage { get; set; } // Giới hạn số lần dùng
        public int CurrentUsage { get; set; } = 0; // Đã dùng
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        public int AdminId { get; set; }
        public string AdminUsername { get; set; } = null!;
        public string Action { get; set; } = null!; // "Create", "Update", "Delete"
        public string EntityType { get; set; } = null!; // "Order", "User", "Product", "Coupon", etc
        public string EntityId { get; set; } = null!;
        public string Details { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("AdminId")]
        public User Admin { get; set; } = null!;
    }
}

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
        return p.ProductMedia
            .Where(m => m.MediaType == "Image" || m.MediaType == "Cover")
            .Select(m => m.MediaUrl)
            .FirstOrDefault() ?? "";
    }
}