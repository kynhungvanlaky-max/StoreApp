namespace Backend.Models
{
    public class CommentViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsHidden { get; set; }
        public List<ReviewReplyViewModel> Replies { get; set; } = new();
    }
}
