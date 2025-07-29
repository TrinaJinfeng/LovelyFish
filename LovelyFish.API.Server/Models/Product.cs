using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LovelyFish.API.Server.Models
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // 自动生成 Id
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Output { get; set; }

        public int Wattage { get; set; }

        public string Image { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Features { get; set; } = string.Empty;
    }
}