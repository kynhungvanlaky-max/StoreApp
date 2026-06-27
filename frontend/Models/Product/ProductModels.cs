namespace Frontend.Models.Product
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Pricing { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string DeveloperName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public List<MediaItemDto> Media { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public bool IsOutOfStock { get; set; }
    }

    public class MediaItemDto
    {
        public string Url { get; set; } = string.Empty;
        public string Type { get; set; } = "Image";
    }

    public class PaginatedList<T>
    {
        [System.Text.Json.Serialization.JsonPropertyName("Data")]
        public List<T> Items { get; set; } = new();
        [System.Text.Json.Serialization.JsonPropertyName("CurrentPage")]
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
