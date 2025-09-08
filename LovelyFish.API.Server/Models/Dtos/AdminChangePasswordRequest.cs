using System.ComponentModel.DataAnnotations;

namespace LovelyFish.API.Server.Models.Dtos
{
    // DTO used when an admin wants to change their password
    public class AdminChangePasswordRequest
    {
        // Current password is required
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        // New password is required and must match the regex pattern:
        // Minimum 8 characters, at least one uppercase, one lowercase, one number, and one special character
        [Required]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters long and contain uppercase, lowercase, number, and special character.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
