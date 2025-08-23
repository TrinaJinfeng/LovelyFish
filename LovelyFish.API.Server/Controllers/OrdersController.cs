using LovelyFish.API.Data;
using LovelyFish.API.Server.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LovelyFish.API.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly LovelyFishContext _context;

        public OrdersController(LovelyFishContext context)
        {
            _context = context;
        }

        // ==================== 普通用户 ====================
        // GET api/orders/my
        [HttpGet("my")]
        [Authorize]
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

                var result = orders.Select(o => MapToDto(o)).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetMyOrders Error: " + ex);
                return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
            }
        }

        // ==================== 管理员 ====================
        // GET api/orders/admin
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrders()
        {
            try
            {
                var orders = await _context.Orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .ToListAsync();

                var result = orders.Select(o => MapToDto(o)).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllOrders Error: " + ex);
                return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
            }
        }

        // PUT api/orders/{orderId}/status
        [HttpPut("{orderId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] string status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return NotFound();

                order.Status = status ?? order.Status;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Order status updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("UpdateOrderStatus Error: " + ex);
                return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
            }
        }

        // PUT api/orders/{orderId}/shipping
        [HttpPut("{orderId}/shipping")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateShippingInfo(int orderId, [FromBody] ShippingUpdateDto dto)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return NotFound();

                order.Courier = dto.Courier ?? order.Courier;
                order.TrackingNumber = dto.TrackingNumber ?? order.TrackingNumber;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Shipping info updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("UpdateShippingInfo Error: " + ex);
                return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
            }
        }

        // ==================== 私有方法 ====================
        private OrderDto MapToDto(Models.Order o)
        {
            return new OrderDto
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
                    ProductName = oi.Product != null ? oi.Product.Title : "Deleted Product",
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            };
        }
    }

}


//普通用户只能 GET /orders/my，管理员才能 GET /orders/admin。

//管理员更新状态 PUT /orders/{id}/ status，更新快递信息 PUT /orders/{id}/ shipping。

//MapToDto 方法统一映射，避免重复代码。

//DTO ShippingUpdateDto 放在控制器文件末尾，前端 OrdersAdminPage.jsx 可以直接对接。

  