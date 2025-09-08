using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LovelyFish.API.Server.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }  // Primary key

        public string UserId { get; set; } = string.Empty; // Reference to the user who placed the order

        public string? CustomerName { get; set; } // Customer's full name

        public string CustomerEmail { get; set; } = string.Empty; // Customer email for notifications
        public string? ShippingAddress { get; set; } // Shipping address
        public string? PhoneNumber { get; set; } // User profile phone number
        public string? ContactPhone { get; set; } // Phone number entered during order confirmation

        public string Status { get; set; } = "pending"; // Order status: pending, shipped, delivered, etc.
        public string Courier { get; set; } = string.Empty; // Courier company name
        public string TrackingNumber { get; set; } = string.Empty; // Tracking number for shipment
        public DateTime CreatedAt { get; set; } // Timestamp of order creation

        public decimal TotalPrice { get; set; } // Total order amount

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>(); // List of products in the order
    }
}
