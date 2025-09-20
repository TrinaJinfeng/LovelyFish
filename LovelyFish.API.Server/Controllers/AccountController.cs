using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LovelyFish.API.Server.Models;  // ApplicationUser
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LovelyFish.API.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        //private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtSettings _jwtSettings;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            //SignInManager<ApplicationUser> signInManager,
            IOptions<JwtSettings> jwtSettings)
        {
            _userManager = userManager;
            //_signInManager = signInManager;
            _jwtSettings = jwtSettings.Value; // 从配置或 .env 获取 JWT_SECRET

            // 后续生成 JWT 时直接用 _jwtSettings.Secret
        }

        // POST api/account/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return BadRequest(ModelState);
            }

            return Ok(new { message = "User registered successfully" });
        }

        // POST api/account/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { message = "Invalid username or password" });

            // 创建 JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? "")
            };

            // 可以加角色
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);

            return Ok(new { token = jwtToken });
        }

        // POST api/account/logout
        [HttpPost("logout")]
        [Authorize] // Must be logged in to call
        public IActionResult Logout()
        {
            // JWT 不需要服务器操作，前端删除 token 即可
            return Ok(new { message = "Logged out successfully" });
        }

        // Get current user 
        [HttpGet("me")]
        [Authorize] // Must be an authenticated user
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            // Retrieve assigned roles
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                phone = user.PhoneNumber,
                address = user.Address,
                newUserUsed = user.NewUserCouponUsed,//Check user is new or not
                roles = roles // Return roles array
            });
        }

        // POST api/account/update-profile
        [HttpPost("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Unauthorized();

            user.Name = model.Name;
            user.PhoneNumber = model.Phone;
            user.Address = model.Address;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { message = "Profile updated successfully" });
        }

        // POST api/account/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest model, [FromServices] IOptions<EmailSettings> emailSettings)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
                return BadRequest(new { message = "Email is required" });

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Return OK even if user does not exist, to avoid account enumeration
                return Ok(new { message = "If that email exists, a reset link has been sent" });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = $"{emailSettings.Value.FrontendBaseUrl}/reset-password?email={Uri.EscapeDataString(model.Email)}&token={Uri.EscapeDataString(token)}";

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("accept", "application/json");
                client.DefaultRequestHeaders.Add("api-key", emailSettings.Value.BrevoApiKey);

                var htmlContent = $@"
                                    <p>Hi {user.Name ?? user.Email},</p>
                                    <p>Please click the following link to reset your password:</p>
                                    <p><a href='{resetLink}' target='_blank'>Reset Password</a></p>
                                    <p>If you did not request a password reset, please ignore this email.</p>
";

                var textContent = $@"
                                    Hi {user.Name ?? user.Email},

                                    Please click the following link to reset your password:
                                    {resetLink}

                                    If you did not request a password reset, please ignore this email.
                                    ";

                Console.WriteLine("===== HTML Content =====");
                Console.WriteLine(htmlContent);
                Console.WriteLine("====================");

                var payload = new
                {
                    sender = new { email = emailSettings.Value.SenderEmail, name = emailSettings.Value.SenderName },
                    to = new[] { new { email = model.Email, name = user.Name ?? model.Email } },
                    subject = "Password Reset - LovelyFishAquarium",
                    htmlContent,
                    textContent
                };

                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Brevo Email Error] " + ex.Message);
                // Even if email fails, do not expose details to client
            }

            return Ok(new { message = "If that email exists, a reset link has been sent" });
        }

        // POST api/account/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { message = "Invalid request" });

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { message = "Password has been reset successfully" });
        }

        // POST api/account/change-password
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate new password complexity
            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$");
            if (!passwordRegex.IsMatch(model.NewPassword))
            {
                return BadRequest(new { message = "New password must be at least 8 characters long and include uppercase, lowercase, number, and special character." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var isOldPasswordValid = await _userManager.CheckPasswordAsync(user, model.OldPassword);
            if (!isOldPasswordValid)
            {
                return BadRequest(new { message = "Current password is incorrect." });
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { message = "Password changed successfully." });
        }
    }

    // Request models
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;
    }
}
