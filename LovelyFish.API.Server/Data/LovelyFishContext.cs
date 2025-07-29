using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Models;

namespace LovelyFish.API.Data
{
    public class LovelyFishContext : DbContext
    {
        public LovelyFishContext(DbContextOptions<LovelyFishContext> options)
        : base(options) { }

        public DbSet<Product> Products { get; set; }
    }
}
