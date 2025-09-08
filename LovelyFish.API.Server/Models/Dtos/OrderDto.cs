namespace LovelyFish.API.Server.Models.Dtos
{
    // DTO representing an Order for API responses
    public class OrderDto
    {
       
        public int Id { get; set; }  // Unique identifier for the order

        
        public DateTime CreatedAt { get; set; } // Timestamp when the order was created

        
        public decimal TotalPrice { get; set; } // Total price of the order including discounts

        
        public string CustomerName { get; set; } = string.Empty; // Customer's full name

        
        public string ShippingAddress { get; set; } = string.Empty; // Shipping address for the order

        
        public string PhoneNumber { get; set; } = string.Empty;  // Customer's phone number from profile

        
        public string ContactPhone { get; set; } = string.Empty;  // Contact phone provided during checkout (ConfirmOrderPage)

        
        public string Status { get; set; } = "pending"; // Current order status (e.g., pending, shipped, delivered)

        
        public string Courier { get; set; } = string.Empty; // Courier company handling the order

        
        public string TrackingNumber { get; set; } = string.Empty; // Tracking number provided by courier

        
        public List<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>(); // List of order items; initialized to avoid null references
    }
}
