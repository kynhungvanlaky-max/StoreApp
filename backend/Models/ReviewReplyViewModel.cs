namespace Backend.Models
{
    public class ReviewReplyViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsHidden { get; set; }
    }
}
