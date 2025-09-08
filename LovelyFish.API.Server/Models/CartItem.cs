using System.ComponentModel.DataAnnotations.Schema;

namespace LovelyFish.API.Server.Models
{
    // Represents an item in a user's shopping cart
    public class CartItem
    {
        // Primary key
        public int Id { get; set; }

        // Foreign key linking to the Product
        public int ProductId { get; set; }

        // Navigation property to Product
        public Product Product { get; set; } = null!;

        // The ID of the user who owns this cart item
        public string UserId { get; set; } = null!;

        // Quantity of this product in the cart
        public int Quantity { get; set; }

        // Computed price property (not mapped to the database)
        [NotMapped]
        public decimal Price => Product?.Price ?? 0;
    }
}
