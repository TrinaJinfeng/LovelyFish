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
                CustomerName = o.CustomerName,           // DTO 新增字段
                ShippingAddress = o.ShippingAddress,     // DTO 新增字段
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
            return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
        }
    }
}
