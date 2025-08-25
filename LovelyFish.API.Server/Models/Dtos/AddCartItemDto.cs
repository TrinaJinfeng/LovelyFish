namespace LovelyFish.API.Server.Models.Dtos
{
    public class AddCartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;

    }
}
