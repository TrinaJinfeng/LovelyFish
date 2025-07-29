using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Server.Models;

namespace LovelyFish.API.Data
{
    public class LovelyFishContext : DbContext
    {
        public LovelyFishContext(DbContextOptions<LovelyFishContext> options)
        : base(options) { }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Product>().HasData(
            //    new Product { Id = 1, Name = "Fishing Rod", Price = 29.99M, Features = "High-quality fishing rod" },
            //    new Product { Id = 2, Name = "Fishing Net", Price = 19.99M, Features = "Durable fishing net" }
            //);
        }
    }
}