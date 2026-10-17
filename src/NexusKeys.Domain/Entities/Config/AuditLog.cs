using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.API.Entities
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        public int AdminId { get; set; }
        public string AdminUsername { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string EntityType { get; set; } = null!;
        public string EntityId { get; set; } = null!;
        public string Details { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("AdminId")]
        public User Admin { get; set; } = null!;
    }
}
