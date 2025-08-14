namespace LovelyFish.API.Server.Models.Dtos
{
    public class OrderItemDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
