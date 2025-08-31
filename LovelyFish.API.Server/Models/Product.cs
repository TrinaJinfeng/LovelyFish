using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace LovelyFish.API.Server.Models
{
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Description { get; set; } = string.Empty;

        public int DiscountPercent { get; set; } = 0;
        public bool IsClearance { get; set; } = false;
        public bool IsNewArrival { get; set; } = false;

        public string FeaturesJson { get; set; } = string.Empty;

        [NotMapped]
        public string[] Features
        {
            get => string.IsNullOrEmpty(FeaturesJson) ? new string[0] : JsonSerializer.Deserialize<string[]>(FeaturesJson);
            set => FeaturesJson = JsonSerializer.Serialize(value);
        }

        [Required]
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    }
}