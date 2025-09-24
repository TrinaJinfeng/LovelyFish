using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LovelyFish.API.Server.Models;  
//using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Text;
using LovelyFish.API.Server.Services;

namespace LovelyFish.API.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService _tokenService;
        

        public AccountController(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        //Register a new user
        // POST api/account/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Create new ApplicationUser with email as username
            var user = new ApplicationUser { UserName = model.Email, Email = model.Email };

            // Save user with hashed password
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                // Return error messages if failed
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return BadRequest(ModelState);
            }

            // Generate token and return when register successfully
            var token = await _tokenService.GenerateToken(user, _userManager);

            return Ok(new { message = "User registered successfully", token });
        }

        //Login and get Jwt token
        // POST api/account/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Find user by email
            var user = await _userManager.FindByEmailAsync(model.Email);

            // Validate password
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Generate JWT token for authenticated user
            var token = await _tokenService.GenerateToken(user, _userManager);

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    roles = await _userManager.GetRolesAsync(user)
                }
            });
        }

        //Logout (JWT cannot be invalidated directly, client just discards token)
        // POST api/account/logout
        [HttpPost("logout")]
        [Authorize] 
        public IActionResult Logout()
        {          
            return Ok(new { message = "Logged out successfully" });
        }

        // Get current user info
        // GET api/account/me
        [HttpGet("me")]
        [Authorize] // Must be logged in 
        public async Task<IActionResult> Me()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Get roles assigned to user
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                id = user.Id,
                name = user.Name,
                email = user.Email,
                phone = user.PhoneNumber,
                address = user.Address,
                newUserUsed = user.NewUserCouponUsed,//Check if the user has used newUserCounpon
                roles = roles // Return roles array
            });
        }

        // Update user profile (name, phone, address)
        // POST api/account/update-profile
        [HttpPost("update-profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Update user info
            user.Name = model.Name;
            user.PhoneNumber = model.Phone;
            user.Address = model.Address;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { message = "Profile updated successfully" });
        }

        // Send password reset link via email
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

            // Generate reset token
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

        // Reset password using token
        // POST api/account/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { message = "Invalid request" });

            // Validate token and reset password
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { message = "Password has been reset successfully" });
        }

        // Change password for authenticated user
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

            // Verify old password
            var isOldPasswordValid = await _userManager.CheckPasswordAsync(user, model.OldPassword);
            if (!isOldPasswordValid)
            {
                return BadRequest(new { message = "Current password is incorrect." });
            }

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { message = "Password changed successfully." });
        }
    }

    // Request DTOs (Data Transfer Objects)
    public class RegisterRequest
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
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
