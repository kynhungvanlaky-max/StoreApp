namespace Backend.Models
{
    public class PaymentConfigDto
    {
        public string GatewayName { get; set; } = string.Empty;
        public string ConfigKey { get; set; } = string.Empty;
        public string ConfigValue { get; set; } = string.Empty;
    }
}
