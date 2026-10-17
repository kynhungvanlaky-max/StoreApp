using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.API.Entities
{
    public class NewsArticle
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Summary { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? CoverImageUrl { get; set; }
        public int AuthorId { get; set; }
        public DateTime PublishedAt { get; set; }
        public bool IsPublished { get; set; } = true;

        [ForeignKey("AuthorId")]
        public User Author { get; set; } = null!;
    }
}
