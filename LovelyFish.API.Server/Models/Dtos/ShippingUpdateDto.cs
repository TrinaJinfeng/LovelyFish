namespace LovelyFish.API.Server.Models.Dtos
{
    public class ShippingUpdateDto
    {
        public string Courier { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
    }
}
