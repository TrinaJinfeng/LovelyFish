using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LovelyFish.API.Server.Models; // ApplicationUser
using LovelyFish.API.Server.Data; // <- includes LovelyFishContext
using Microsoft.AspNetCore.Authorization;
using LovelyFish.API.Server.Models.Dtos;
using LovelyFish.API.Data;
using Microsoft.Extensions.Options;
using System.Text;

namespace LovelyFish.API.Server.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    // Entire controller is restricted to Admin role. 
    // Only login, forgot-password, and reset-password are marked with [AllowAnonymous]. 
    // This ensures all other actions are protected by default and avoids forgetting [Authorize] on new endpoints.
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
        public async Task<IActionResult> ForgotPassword(AdminForgotPasswordRequest model, [FromServices] IOptions<EmailSettings> emailSettings)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
                return BadRequest(new { message = "Invalid request" });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Generate reset link
            var resetLink = $"{emailSettings.Value.FrontendBaseUrl}/admin/reset-password?email={Uri.EscapeDataString(model.Email)}&token={Uri.EscapeDataString(token)}";

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("accept", "application/json");
                client.DefaultRequestHeaders.Add("api-key", emailSettings.Value.BrevoApiKey);

                var htmlContent = $"<p>Hi {user.Name ?? user.Email},</p>" +
                                  $"<p>Please click the link below to reset your admin password:</p>" +
                                  $"<p><a href='{resetLink}'>Reset Password</a></p>" +
                                  "<p>If you did not request a password reset, please ignore this email.</p>";

                var textContent = $"Hi {user.Name ?? user.Email},\n\n" +
                                  $"Please click the link below to reset your admin password:\n{resetLink}\n\n" +
                                  "If you did not request a password reset, please ignore this email.";

                Console.WriteLine("===== HTML Content =====");
                Console.WriteLine(htmlContent);
                Console.WriteLine("=======================");

                var payload = new
                {
                    sender = new { email = emailSettings.Value.SenderEmail, name = emailSettings.Value.SenderName },
                    to = new[] { new { email = model.Email, name = user.Name ?? model.Email } },
                    subject = "Admin Password Reset - LovelyFishAquarium",
                    htmlContent,
                    textContent
                };

                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Brevo Email Error] " + ex.Message);
                // Email failure does not affect the response
            }

            return Ok(new { message = "If that email exists, a reset link has been sent" });

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
                orderCount = 0 // TODO: Here you could query the order table to count
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
                user.LockoutEnd = null; // enable
            }
            else
            {
                user.LockoutEnd = DateTimeOffset.MaxValue; // disable
            }

            await _userManager.UpdateAsync(user);
            return Ok();
        }

        // GET api/admin/orders - order management in admin panel
        [HttpGet("orders")]
        public IActionResult GetOrders([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            // Query orders from database
            var query = _context.Orders.AsQueryable();

            // Search by customer name or phone
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();

                query = query.Where(o =>
                    (o.CustomerName != null && o.CustomerName.ToLower().Contains(lowerSearch)) ||
                    (o.PhoneNumber != null && o.PhoneNumber.ToLower().Contains(lowerSearch)) ||
                    (o.ContactPhone != null && o.ContactPhone.ToLower().Contains(lowerSearch))
                );
            }

            // Order by creation time (descending)
            query = query.OrderByDescending(o => o.CreatedAt);

            // Pagination
            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

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

        // GET api/admin/orders/{id} - view order details
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
