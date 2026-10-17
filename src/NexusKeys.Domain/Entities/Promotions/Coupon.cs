using System;
using System.ComponentModel.DataAnnotations;

namespace GameStore.API.Entities
{
    public class Coupon
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public decimal DiscountPercent { get; set; }
        public int MaxUsage { get; set; }
        public int CurrentUsage { get; set; } = 0;
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
