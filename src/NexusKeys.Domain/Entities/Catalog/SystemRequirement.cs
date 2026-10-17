using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.API.Entities
{
    public class SystemRequirement
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string RequirementType { get; set; } = null!;
        public string TargetOS { get; set; } = null!;
        public string OSVersion { get; set; } = null!;
        public string Processor { get; set; } = null!;
        public string Memory { get; set; } = null!;
        public string Graphics { get; set; } = null!;
        public string Storage { get; set; } = null!;

        [ForeignKey("ProductId")]
        public Product Product { get; set; } = null!;
    }
}
