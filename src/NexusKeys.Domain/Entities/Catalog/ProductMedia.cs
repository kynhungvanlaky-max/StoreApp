using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.API.Entities
{
    public class ProductMedia
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string MediaType { get; set; } = null!;
        public string MediaUrl { get; set; } = null!;
        public int DisplayOrder { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }
}
