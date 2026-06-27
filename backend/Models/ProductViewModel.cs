namespace Backend.Models
{
    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string DeveloperName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}
