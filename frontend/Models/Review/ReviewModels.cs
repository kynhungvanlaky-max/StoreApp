namespace Frontend.Models.Review
{
    public class ReviewCommentDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ReviewDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsPositive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ReviewCommentDto> Replies { get; set; } = new();
    }

    public class CreateReviewDto
    {
        public int ProductId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsRecommended { get; set; }
    }

    public class ReplyReviewDto
    {
        public string Content { get; set; } = string.Empty;
    }
}
