using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.API.Entities
{
    public class ProductKey
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string KeyCode { get; set; } = null!;
        public bool IsSold { get; set; }
        public int? OrderId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }
    }
}
