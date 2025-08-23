using System.ComponentModel.DataAnnotations;

namespace LovelyFish.API.Server.Models.Dtos
{
    public class AdminChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
            ErrorMessage = "密码至少8位，且必须包含大写字母、小写字母、数字和特殊字符")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
