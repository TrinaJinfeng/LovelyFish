using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LovelyFish.API.Server.Models; // ApplicationUser
using LovelyFish.API.Server.Data; // <- 包含 LovelyFishContext
using Microsoft.AspNetCore.Authorization;
using LovelyFish.API.Server.Models.Dtos;
using LovelyFish.API.Data;

namespace LovelyFish.API.Server.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")] 
    // 整个控制器 [Authorize(Roles = "Admin")]，只给登录、忘记密码、重置密码用 [AllowAnonymous]。
    // 避免后续新增接口忘记加 [Authorize]，统一管理安全策略。
    public class AdminController : ControllerBase
    {
        private readonly LovelyFishContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AdminController(
                               LovelyFishContext context,
                               UserManager<ApplicationUser> userManager,
                               SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // POST api/admin/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(AdminLoginRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized(new { message = "Invalid credentials" });

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
                return Unauthorized(new { message = "Not an admin" });

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
            if (!result.Succeeded) return Unauthorized(new { message = "Invalid credentials" });

            return Ok(new { message = "Admin login successful" });
        }

        // GET api/admin/me
        [HttpGet("me")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Me()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            return Ok(new { email = user.Email, name = user.Name });
        }

        // POST api/admin/logout
        [HttpPost("logout")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "Logged out" });
        }

        // POST api/admin/forgot-password
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(AdminForgotPasswordRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
                return BadRequest(new { message = "Invalid request" });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // 这里实际情况需要发邮件，你可以先返回 token 方便测试
            return Ok(new { message = "Password reset token generated", token });
        }

        // POST api/admin/reset-password
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(AdminResetPasswordRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
                return BadRequest(new { message = "Invalid request" });

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { message = "Password has been reset successfully" });
        }

        // POST api/admin/change-password
        [HttpPost("change-password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangePassword(AdminChangePasswordRequest model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { message = "Password changed successfully" });
        }

        // GET api/admin/users
        [HttpGet]
        public IActionResult GetUsers()
        {
            var users = _userManager.Users.Select(u => new {
                id = u.Id,
                username = u.UserName,
                email = u.Email,
                active = u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.Now,
                orderCount = 0 // TODO: 这里你可以查订单表统计
            }).ToList();

            return Ok(users);
        }

        // PUT api/admin/users/{id}/active
        [HttpPut("{id}/active")]
        public async Task<IActionResult> ToggleActive(string id, [FromBody] bool active)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (active)
            {
                user.LockoutEnd = null; // 启用
            }
            else
            {
                user.LockoutEnd = DateTimeOffset.MaxValue; // 禁用
            }

            await _userManager.UpdateAsync(user);
            return Ok();
        }

        // GET api/admin/orders 后台订单管理接口
        [HttpGet("orders")]
        public IActionResult GetOrders([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            // 从数据库中查询订单
            var query = _context.Orders.AsQueryable();

            // 搜索客户姓名或电话
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower(); // 用户输入的小写版

                query = query.Where(o =>
                    (o.CustomerName != null && o.CustomerName.ToLower().Contains(lowerSearch)) ||
                    (o.PhoneNumber != null && o.PhoneNumber.ToLower().Contains(lowerSearch)) ||
                    (o.ContactPhone != null && o.ContactPhone.ToLower().Contains(lowerSearch))
                );
            }

            // 按创建时间倒序
            query = query.OrderByDescending(o => o.CreatedAt);

            // 计算分页信息
            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // 取当前页数据
            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    id = o.Id,
                    customerName = o.CustomerName,
                    phoneNumber = o.PhoneNumber,
                    contactPhone = o.ContactPhone,
                    shippingAddress = o.ShippingAddress,
                    totalPrice = o.TotalPrice,
                    status = o.Status,
                    courier = o.Courier,
                    trackingNumber = o.TrackingNumber,
                    createdAt = o.CreatedAt
                })
                .ToList();

            return Ok(new
            {
                items,
                totalPages,
                totalItems
            });
        }

        // View Details
        [HttpGet("orders/{id}")]
        public IActionResult GetOrderById(int id)
        {
            var order = _context.Orders
                .Where(o => o.Id == id)
                .Select(o => new {
                    id = o.Id,
                    customerName = o.CustomerName,
                    phoneNumber = o.PhoneNumber,
                    contactPhone = o.ContactPhone,
                    shippingAddress = o.ShippingAddress,
                    totalPrice = o.TotalPrice,
                    status = o.Status,
                    courier = o.Courier,
                    trackingNumber = o.TrackingNumber,
                    createdAt = o.CreatedAt
                })
                .FirstOrDefault();

            if (order == null)
                return NotFound();

            return Ok(order);
        }
    }
}
