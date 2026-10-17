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
}
