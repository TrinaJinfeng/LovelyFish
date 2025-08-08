using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LovelyFish.API.Server.Models;  // ApplicationUser
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
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
                name = user.UserName,
                email = user.Email
            });
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