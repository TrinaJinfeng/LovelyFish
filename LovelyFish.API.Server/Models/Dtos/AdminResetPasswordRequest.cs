using System.ComponentModel.DataAnnotations;

namespace LovelyFish.API.Server.Models.Dtos
{
    public class AdminResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "密码至少8位")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
