using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.API.Entities
{
    public class Release
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string VersionNumber { get; set; } = null!;
        public string TargetOS { get; set; } = null!;
        public string DownloadUrl { get; set; } = null!;
        public string Checksum { get; set; } = null!;

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }
}
