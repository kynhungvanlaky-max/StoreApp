using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.API.Entities
{
    public class SupportTicket
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Topic { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? OrderId { get; set; }
        public string Content { get; set; } = null!;
        public string Status { get; set; } = "Pending";
        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
