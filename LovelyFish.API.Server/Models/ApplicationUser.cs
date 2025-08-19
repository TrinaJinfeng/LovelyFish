using Microsoft.AspNetCore.Identity;

namespace LovelyFish.API.Server.Models
{
   
  public class ApplicationUser : IdentityUser
        {
        // 用户真实姓名
        public string? Name { get; set; }
        public string? Address { get; set; }

        // PhoneNumber 已经在 IdentityUser 里有了，不需要再加

        // 新增字段
        public decimal AccumulatedAmount { get; set; } = 0;   // 累积消费
        public bool NewUserCouponUsed { get; set; } = false;  // 新用户5刀优惠是否已用
    }
}

