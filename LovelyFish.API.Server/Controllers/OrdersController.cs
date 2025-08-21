using LovelyFish.API.Data;
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

    [HttpGet("my")]
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

                Status = o.Status ?? "pending",                  // 新增订单状态
                Courier = o.Courier ?? "",                       // 新增快递公司
                TrackingNumber = o.TrackingNumber ?? "",         // 新增快递单号

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

    // 管理员获取所有订单
    [HttpGet("admin")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrders()
    {
        try
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();

            var result = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                CreatedAt = o.CreatedAt,
                TotalPrice = o.TotalPrice,
                CustomerName = o.CustomerName ?? "",
                ShippingAddress = o.ShippingAddress ?? "",
                PhoneNumber = o.PhoneNumber ?? "",
                ContactPhone = o.ContactPhone ?? "",
                Status = o.Status ?? "pending",
                Courier = o.Courier ?? "",
                TrackingNumber = o.TrackingNumber ?? "",
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
            Console.WriteLine("GetAllOrders Error: " + ex);
            return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
        }
    }

    // 管理员更新订单状态/快递信息
    [HttpPut("admin/{orderId}")]
    public async Task<IActionResult> UpdateOrder(int orderId, [FromBody] OrderUpdateDto dto)
    {
        try
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            order.Status = dto.Status ?? order.Status;
            order.Courier = dto.Courier ?? order.Courier;
            order.TrackingNumber = dto.TrackingNumber ?? order.TrackingNumber;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Order updated successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine("UpdateOrder Error: " + ex);
            return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
        }
    }
}

//新增功能说明：

//GET /api/orders/my → 用户查看自己订单。

//GET /api/orders/admin → 管理员查看所有订单。

//PUT /api/orders/admin/{orderId} → 管理员更新订单状态或快递信息。
