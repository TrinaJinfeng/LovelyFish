using LovelyFish.API.Server.Models.Dtos;

namespace LovelyFish.API.Server.Dtos
{
    // DTO for checkout process
    public class CheckoutDto
    {
        // Customer's full name
        public string CustomerName { get; set; } = string.Empty;

        // Delivery method: "pickup" or "courier"
        public string DeliveryMethod { get; set; } = "pickup";

        // Shipping address (only required if courier)
        public string ShippingAddress { get; set; } = string.Empty;

        // Phone number entered in ConfirmOrderPage
        public string Phone { get; set; } = string.Empty;

        // Customer email
        public string CustomerEmail { get; set; } = string.Empty;

        // List of items in the order
        public List<CheckoutItemDto> Items { get; set; } = new List<CheckoutItemDto>();

        // Frontend-selected discount coupons
        public bool UseNewUserCoupon { get; set; } = false; // $5 new user coupon, can only use once
        public bool Use50Coupon { get; set; } = false;      // Coupon for orders over $50
        public bool Use100Coupon { get; set; } = false;     // Coupon for orders over $100
    }
}
