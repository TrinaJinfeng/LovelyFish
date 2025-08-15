using Microsoft.AspNetCore.Identity;

namespace LovelyFish.API.Server.Models
{
   
  public class ApplicationUser : IdentityUser
        {
        // 用户真实姓名
        public string? Name { get; set; }
        public string? Address { get; set; }

        // PhoneNumber 已经在 IdentityUser 里有了，不需要再加
    }
}

