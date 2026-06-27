namespace Frontend.Models.Cart
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = true;
    }
}
