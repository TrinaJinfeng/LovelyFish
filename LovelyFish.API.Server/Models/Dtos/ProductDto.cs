namespace LovelyFish.API.Server.Models.Dtos
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public decimal Price { get; set; }
        public int DiscountPercent { get; set; }
        public int Stock { get; set; }
        public string? Description { get; set; }
        public List<string> Features { get; set; } = new List<string>(); // <- 改为 List<string>
        public int CategoryId { get; set; }
        public string? CategoryTitle { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();

        // 新增字段
        public string? MainImageUrl { get; set; }
        public bool IsClearance { get; set; }

        public bool IsNewArrival { get; set; }
    }

}