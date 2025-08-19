namespace LovelyFish.API.Server.Models.Dtos
{
    public class OrderDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalPrice { get; set; }


        public string CustomerName { get; set; } = string.Empty;    // 新增姓名
        public string ShippingAddress { get; set; } = string.Empty;  // 新增收货地址

        public string PhoneNumber { get; set; } = string.Empty;     // 新增联系电话
        public string ContactPhone { get; set; } = string.Empty;    //ConfirmOrderPage 电话
        public List<OrderItemDto> OrderItems { get; set; }

    }
}