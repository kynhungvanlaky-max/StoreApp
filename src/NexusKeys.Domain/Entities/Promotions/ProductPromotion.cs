using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.API.Entities
{
    public class ProductPromotion
    {
        public int ProductId { get; set; }
        public int PromotionId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        [ForeignKey("PromotionId")]
        public Promotion Promotion { get; set; } = null!;
    }
}
