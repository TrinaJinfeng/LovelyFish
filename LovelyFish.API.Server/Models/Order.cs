using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LovelyFish.API.Server.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;       // 新增用户姓名
        public string ShippingAddress { get; set; } = string.Empty;    // 新增收货地址
        public DateTime CreatedAt { get; set; }

        public decimal TotalPrice { get; set; }

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}

//记录订单