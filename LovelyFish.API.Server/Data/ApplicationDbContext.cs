using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Server.Models;  // 你的 ApplicationUser 命名空间

namespace LovelyFish.API.Server.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // 你可以加其他 DbSet 例如：
        // public DbSet<Product> Products { get; set; }
    }
}
