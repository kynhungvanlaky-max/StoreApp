namespace Backend.Models
{
    public class OrderViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public List<string> Items { get; set; } = new();
        public List<OrderKeyDto> Keys { get; set; } = new();
    }

    public class OrderKeyDto
    {
        public int Id { get; set; }
        public string KeyCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
    }
}
