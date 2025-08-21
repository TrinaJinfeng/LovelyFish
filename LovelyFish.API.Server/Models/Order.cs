using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LovelyFish.API.Server.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string? CustomerName { get; set; }       // 新增用户姓名
        public string? ShippingAddress { get; set; }     // 新增收货地址
        public string? PhoneNumber { get; set; }         // 联系电话 Profile里的
        public string? ContactPhone { get; set; }     //  下单页面填写的确认电话

        public string Status { get; set; } = "pending";
        public string Courier { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public decimal TotalPrice { get; set; }

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}

//记录订单