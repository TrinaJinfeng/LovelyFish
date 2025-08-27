using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LovelyFish.API.Server.Models
{
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column("Url")]
        public string FileName { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;
    }
}
