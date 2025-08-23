using System.ComponentModel.DataAnnotations;

namespace LovelyFish.API.Server.Models.Dtos
{
    public class AdminForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}