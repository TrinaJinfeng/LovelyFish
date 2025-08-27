using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Server.Data;
using LovelyFish.API.Server.Models;
using LovelyFish.API.Data;

namespace LovelyFish.API.Server.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LovelyFishContext _context;

        public AdminUsersController(UserManager<ApplicationUser> userManager, LovelyFishContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET api/admin/users
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(u => u.UserName.ToLower().Contains(lowerSearch) || u.Email.ToLower().Contains(lowerSearch));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var usersList = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 获取所有订单到内存，避免 EF Core 子查询报错
            var userIds = usersList.Select(u => u.Id).ToList();
            var orders = await _context.Orders
                .Where(o => userIds.Contains(o.UserId))
                .ToListAsync();

            var users = usersList.Select(u => new
            {
                id = u.Id,
                username = u.UserName,
                email = u.Email,
                active = u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.Now,
                orderCount = orders.Count(o => o.UserId == u.Id)
            }).ToList();

            return Ok(new { items = users, totalPages });
        }

        // PUT api/admin/users/{id}/active
        [HttpPut("{id}/active")]
        public async Task<IActionResult> ToggleActive(string id, [FromBody] bool active)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.LockoutEnd = active ? null : DateTimeOffset.MaxValue;
            await _userManager.UpdateAsync(user);

            return Ok();
        }

        // GET api/admin/users/{userId}/orders
        [HttpGet("{userId}/orders")]
        public async Task<IActionResult> GetOrdersByUserId(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query = _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await query
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
                .ToListAsync();

            Console.WriteLine("Orders fetched: " + items.Count);

            return Ok(new { items, totalPages });
        }
    }
}
