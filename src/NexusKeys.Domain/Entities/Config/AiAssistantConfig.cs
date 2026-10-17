using System;
using System.ComponentModel.DataAnnotations;

namespace GameStore.API.Entities
{
    public class AiAssistantConfig
    {
        [Key]
        public int Id { get; set; }
        public string ConfigKey { get; set; } = null!;
        public string ConfigValue { get; set; } = null!;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
