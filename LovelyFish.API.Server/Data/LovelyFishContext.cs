using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Server.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace LovelyFish.API.Data
{
    public class LovelyFishContext : IdentityDbContext<ApplicationUser>
    {
        public LovelyFishContext(DbContextOptions<LovelyFishContext> options)
        : base(options) { }

        public DbSet<Product> Products { get; set; }

        public DbSet<CartItem> CartItems { get; set; }  // 新增购物车项表

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // 这一行很重要，要调用基类方法，否则 Identity 的表不会创建

            // 显式指定 CartItem 主键
            modelBuilder.Entity<CartItem>().HasKey(c => c.Id);

            // 明确 CartItem 和 Product 关系，防止 EF Core 推断失败
            modelBuilder.Entity<CartItem>()
                .HasOne(c => c.Product)
                .WithMany() // 如果 Product 没有导航属性回指 CartItem，就用无导航的关系
                .HasForeignKey(c => c.ProductId)
                .IsRequired();

            //modelBuilder.Entity<Product>().HasData(
            //    new Product { Id = 1, Name = "Fishing Rod", Price = 29.99M, Features = "High-quality fishing rod" },
            //    new Product { Id = 2, Name = "Fishing Net", Price = 19.99M, Features = "Durable fishing net" }
            //);
        }
    }
}