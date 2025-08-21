namespace LovelyFish.API.Server.Models.Dtos
{
    public class OrderDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalPrice { get; set; }


        public string CustomerName { get; set; } = string.Empty;    
        public string ShippingAddress { get; set; } = string.Empty;  

        public string PhoneNumber { get; set; } = string.Empty;     // add PhSone Number from Profile
        public string ContactPhone { get; set; } = string.Empty;    //  add Contact Phone from ConfirmOrderPage 
        public string Status { get; set; } = "pending";          //  add Order status from Order Page
        public string Courier { get; set; } = string.Empty;         //  add Courier company from Order Page
        public string TrackingNumber { get; set; } = string.Empty;  //  add tracking number from Order Page
        public List<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>(); //给 OrderItems 初始化空列表，避免 null

    }
}