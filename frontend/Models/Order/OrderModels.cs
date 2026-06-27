namespace Frontend.Models.Order
{
    public class CheckoutRequest
    {
        public List<int> ProductIds { get; set; } = new List<int>();
        public string PaymentMethod { get; set; } = "MockGateway"; // VNPay, MoMo, MockGateway
        public string? CouponCode { get; set; }
    }

    public class CheckoutResponse
    {
        public string Message { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string PaymentUrl { get; set; } = string.Empty;
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public List<string> Items { get; set; } = new();
        public List<ProductKeyDto>? Keys { get; set; }
    }

    public class ProductKeyDto
    {
        public int Id { get; set; }
        public string KeyCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
    }
}
