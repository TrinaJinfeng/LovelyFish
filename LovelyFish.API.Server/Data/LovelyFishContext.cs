using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Server.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace LovelyFish.API.Data
{
    // DbContext for the application, inherits from IdentityDbContext to include ASP.NET Identity
    public class LovelyFishContext : IdentityDbContext<ApplicationUser>
    {
        public LovelyFishContext(DbContextOptions<LovelyFishContext> options)
            : base(options) { }

        // Products table
        public DbSet<Product> Products { get; set; }

        // Product images table
        public DbSet<ProductImage> ProductImages { get; set; }

        // Categories table
        public DbSet<Category> Categories { get; set; }

        // Shopping cart items table
        public DbSet<CartItem> CartItems { get; set; }

        // Orders table
        public DbSet<Order> Orders { get; set; }

        // Order items table
        public DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<FishOwner> FishOwners { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Important: call base method so Identity tables are created

            // Explicitly set primary key for CartItem
            modelBuilder.Entity<CartItem>().HasKey(c => c.Id);

            // Define relationship between CartItem and Product
            modelBuilder.Entity<CartItem>()
                .HasOne(c => c.Product)
                .WithMany() // if Product has no navigation property back to CartItem
                .HasForeignKey(c => c.ProductId)
                .IsRequired();

        
        }
    }
}
