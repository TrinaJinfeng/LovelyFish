using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LovelyFish.API.Server.Models;  // ApplicationUser
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

            return Ok(new
            {
                name = user.Name,
                email = user.Email,
                phone = user.PhoneNumber,
                address = user.Address
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
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest model)
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
            var resetLink = $"http://localhost:3000/reset-password?email={Uri.EscapeDataString(model.Email)}&token={Uri.EscapeDataString(token)}";

            // TODO: 用邮件服务发送 resetLink 给用户
            Console.WriteLine($"发送密码重置链接: {resetLink}");

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

//UserManager 用来管理用户创建、查找等。

//SignInManager 处理登录逻辑。

//注册接口：接收邮箱和密码，创建用户。

//登录接口：验证邮箱密码，返回登录结果。

//新增 Logout 接口

//HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme) 会清理 .AspNetCore.Identity.Application Cookie，让用户真正退出。

//加了 [Authorize]，防止未登录时调用浪费资源。

//Login 中 isPersistent: false

//不让 Cookie 永久保存（关闭浏览器就失效）。

//如果你需要“记住我”功能，可以用 true 并在前端给用户选项。

//用户在 忘记密码页面 输入邮箱。

//后端调用 UserManager.GeneratePasswordResetTokenAsync(user) 生成一次性 Token。

//生成带有 email 和 token 的链接发给用户邮箱。

//用户点击链接到 重置密码页面，前端从 URL 中读取 email 和 token，输入新密码后提交到 /reset-password。

//后端用 UserManager.ResetPasswordAsync 验证 Token 并更新密码。

//开发调试说明
//🔹 测试流程（无邮件服务）
//在 忘记密码 页面输入已注册邮箱 → 发送 /forgot-password

//后端控制台会输出一个 reset link，类似：

//perl
//复制
//编辑
//https://localhost:3000/reset-password?email=trina@126.com&token=XYZ...
//打开浏览器访问这个地址，前端页面会自动把 email/token 填入表单（你可以在 ResetPasswordPage 加个 useEffect 自动读取 query 参数）。

//输入新密码 → 调用 /reset-password → 返回成功。
//     邮件发送（生产）
//后续你只需要在：

//csharp
//复制
//编辑
//Console.WriteLine($"Reset link (dev only): {resetLink}");
//位置换成真正的 SMTP 邮件发送逻辑 就行，比如：

//csharp
//复制
//编辑
//await _emailSender.SendEmailAsync(model.Email, "Reset your password", $"Click here: {resetLink}");