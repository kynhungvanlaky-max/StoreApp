namespace Backend.Models
{
    public class NewsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public string AuthorName { get; set; } = string.Empty;
    }
}
