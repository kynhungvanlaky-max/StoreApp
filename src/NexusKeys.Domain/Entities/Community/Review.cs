using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.API.Entities
{
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
}
