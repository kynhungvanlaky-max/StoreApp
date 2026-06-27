namespace Backend.Models
{
    public class ProductKeyViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string KeyCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; 
        public bool IsSold { get; set; }
        public int ProductId { get; set; }
        public int? OrderId { get; set; }
    }
}
