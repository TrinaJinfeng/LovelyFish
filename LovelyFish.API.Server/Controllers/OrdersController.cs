using LovelyFish.API.Data;
using LovelyFish.API.Server.Models;
using LovelyFish.API.Server.Models.Dtos; // 引入 DTO 命名空间
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly LovelyFishContext _context;

    public OrdersController(LovelyFishContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetMyOrders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        try
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();

            var result = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                CreatedAt = o.CreatedAt,
                TotalPrice = o.TotalPrice,
                CustomerName = o.CustomerName ?? "",           // 防止 null
                ShippingAddress = o.ShippingAddress ?? "",     // 防止 null
                PhoneNumber = o.PhoneNumber ?? "",             // Profile 电话
                ContactPhone = o.ContactPhone ?? "",           // ConfirmOrderPage 电话

                OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductName = oi.Product != null ? oi.Product.Name : "Deleted Product",
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            // 输出完整异常，方便调试
            Console.WriteLine("GetMyOrders Error: " + ex);
            return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
        }
    }
}
