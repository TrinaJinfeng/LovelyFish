using System.ComponentModel.DataAnnotations;

namespace LovelyFish.API.Server.Models.Dtos
{
    // DTO used when an admin wants to reset password via email token
    public class AdminResetPasswordRequest
    {
        // Admin's email is required and must be a valid email format
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Reset token sent via email, required
        [Required]
        public string Token { get; set; } = string.Empty;

        // New password is required, minimum length 8
        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
