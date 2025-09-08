using Microsoft.AspNetCore.Identity;

namespace LovelyFish.API.Server.Models
{
    // Custom ApplicationUser extending IdentityUser
    public class ApplicationUser : IdentityUser
    {
        // User's real/full name
        public string? Name { get; set; }

        // User's address
        public string? Address { get; set; }

        // PhoneNumber is already included in IdentityUser

        // Total accumulated spending by the user
        public decimal AccumulatedAmount { get; set; } = 0;

        // Indicates whether the new user $5 coupon has been used
        public bool NewUserCouponUsed { get; set; } = false;
    }
}
