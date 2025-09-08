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

        // ==================== Regular User ====================
        // GET api/orders/my
        // Returns orders for the currently logged-in user
        [HttpGet("my")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get logged-in user ID
            if (userId == null) return Unauthorized();

            try
            {
                var orders = await _context.Orders
                    .Where(o => o.UserId == userId) // Filter orders by current user
                    .OrderByDescending(o => o.CreatedAt) // Latest orders first
                    .Include(o => o.OrderItems) // Include related order items
                        .ThenInclude(oi => oi.Product) // Include product details
                          .ThenInclude(p => p.Images) // Include product images
                    .ToListAsync();

                var result = orders.Select(o => MapToDto(o)).ToList(); // Map entities to DTOs
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetMyOrders Error: " + ex);
                return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
            }
        }

        // ==================== Admin ====================
        // GET api/orders/admin
        // Returns all orders (admin only)
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrders()
        {
            try
            {
                var orders = await _context.Orders
                    .OrderByDescending(o => o.CreatedAt) // Latest orders first
                    .Include(o => o.OrderItems) // Include related order items
                        .ThenInclude(oi => oi.Product) // Include product details
                        .ThenInclude(p => p.Images) // Include product images
                    .ToListAsync();

                var result = orders.Select(o => MapToDto(o)).ToList(); // Map entities to DTOs
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllOrders Error: " + ex);
                return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
            }
        }

        // PUT api/orders/{orderId}/status
        // Admin updates order status
        [HttpPut("{orderId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromBody] string status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return NotFound();

                order.Status = status ?? order.Status; // Update status if provided
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
        // Admin updates shipping info (courier + tracking number)
        [HttpPut("{orderId}/shipping")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateShippingInfo(int orderId, [FromBody] ShippingUpdateDto dto)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return NotFound();

                order.Courier = dto.Courier ?? order.Courier; // Update courier if provided
                order.TrackingNumber = dto.TrackingNumber ?? order.TrackingNumber; // Update tracking if provided

                await _context.SaveChangesAsync();
                return Ok(new { message = "Shipping info updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("UpdateShippingInfo Error: " + ex);
                return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
            }
        }

        // ==================== Private Methods ====================
        // Map Order entity to DTO to unify API response format
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
                    ProductId = oi.ProductId,
                    ProductName = oi.Product != null ? oi.Product.Title : "Deleted Product", // Handle deleted products
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    MainImageUrl = oi.Product?.Images?.FirstOrDefault()?.FileName
                                ?? "https://lovelyfishstorage2025.blob.core.windows.net/images/placeholder.png"
                }).ToList()
            };
        }
    }
}
