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

namespace LovelyFish.API.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            // isPersistent: false 表示不记住登录，关闭浏览器 Cookie 失效
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Wrong Username or password!" });
            }

            return Ok(new { message = "Login successful" });
        }
        // POST api/account/logout
        [HttpPost("logout")]
        [Authorize] // 必须登录才能调用
        public async Task<IActionResult> Logout()
        {
            // 清除认证 Cookie
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            // 也可以用 SignInManager 的登出方法（等效）
            // await _signInManager.SignOutAsync();

            return Ok(new { message = "Logged out" });
        }

        // POST api/account/me
        [HttpGet("me")]
        [Authorize] // 必须是已登录用户才能调用
        public async Task<IActionResult> Me()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // 这里获取角色列表
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                name = user.Name,
                email = user.Email,
                phone = user.PhoneNumber,
                address = user.Address,
                newUserUsed = user.NewUserCouponUsed,  // 新增字段
                roles = roles // 返回角色数组
            });
        }

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

            user.Name = model.Name;
            user.PhoneNumber = model.Phone;
            user.Address = model.Address;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { message = "资料更新成功" });
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
                // 即使用户不存在也返回 OK，避免暴露账号信息
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
                                    <p>请点击以下链接重置密码:</p>
                                    <p><a href='{resetLink}' target='_blank'>重置密码</a></p>
                                    <p>如果您没有请求重置密码，请忽略此邮件。</p>
";

                // 纯文本邮件
                var textContent = $@"
                                    Hi {user.Name ?? user.Email},

                                    请点击以下链接重置密码:
                                    {resetLink}

                                    如果您没有请求重置密码，请忽略此邮件。
                                    ";

                Console.WriteLine("===== HTML 内容 =====");
                Console.WriteLine(htmlContent);
                Console.WriteLine("====================");

                var payload = new
                {
                    sender = new { email = emailSettings.Value.SenderEmail, name = emailSettings.Value.SenderName },
                    to = new[] { new { email = model.Email, name = user.Name ?? model.Email } },
                    subject = "密码重置 - LovelyFishAquarium",
                    htmlContent,
                    textContent
                };



                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Brevo Email Error] " + ex.Message);
                // 邮件发送失败也不影响返回
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

            // 新密码复杂度校验
            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$");
            if (!passwordRegex.IsMatch(model.NewPassword))
            {
                return BadRequest(new { message = "新密码必须至少8位，包含大写字母、小写字母、数字和特殊字符。" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var isOldPasswordValid = await _userManager.CheckPasswordAsync(user, model.OldPassword);
            if (!isOldPasswordValid)
            {
                return BadRequest(new { message = "当前密码不正确。" });
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { message = "密码修改成功。" });
        }

    }


    // 请求模型
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
    //新增了 [HttpPost("change-password")] 接口，带 [Authorize] 保护，必须登录用户才能调用。
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

