

using System.ComponentModel.DataAnnotations.Schema;

namespace LovelyFish.API.Server.Models
{
    public class CartItem
    {
        public int Id { get; set; } //key

        public int ProductId { get; set; } // 关联产品
        public Product Product { get; set; } = null!;

        public string UserId { get; set; } = null!; // 关联用户，方便区分不同用户的购物车

        public int Quantity { get; set; }

        // Price 是计算属性，不映射数据库，避免 Price 被当作数据库字段。
        [NotMapped] 
        public decimal Price => Product?.Price ?? 0;  // 读取对应产品价格
    }
}
