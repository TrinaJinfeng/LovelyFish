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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // 这一行很重要，要调用基类方法，否则 Identity 的表不会创建
            //modelBuilder.Entity<Product>().HasData(
            //    new Product { Id = 1, Name = "Fishing Rod", Price = 29.99M, Features = "High-quality fishing rod" },
            //    new Product { Id = 2, Name = "Fishing Net", Price = 19.99M, Features = "Durable fishing net" }
            //);
        }
    }
}