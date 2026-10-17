using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.API.Entities
{
    public class Wishlist
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public DateTime AddedDate { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }
}
