using System;
using System.ComponentModel.DataAnnotations;

namespace GameStore.API.Entities
{
    public class PaymentGatewayConfig
    {
        [Key]
        public int Id { get; set; }
        public string GatewayName { get; set; } = null!;
        public string ConfigKey { get; set; } = null!;
        public string ConfigValue { get; set; } = null!;
        public DateTime UpdatedAt { get; set; }
    }
}
