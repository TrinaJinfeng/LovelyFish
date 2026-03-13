using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Server.Models;
using LovelyFish.API.Data;
using System.Security.Claims;
using LovelyFish.API.Server.Dtos;
using Swashbuckle.AspNetCore.SwaggerUI;
using LovelyFish.API.Server.Models.Dtos;
using System.Numerics;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using LovelyFish.API.Server.Services;

namespace LovelyFish.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly LovelyFishContext _context;

        public CartController(LovelyFishContext context)
        {
            _context = context;
        }

        // POST /api/cart
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddCartItemDto dto)
        {
            if (dto == null || dto.ProductId <= 0 || dto.Quantity <= 0)
                return BadRequest("Invalid parameters");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ProductId == dto.ProductId && c.UserId == userId);

            if (existingItem != null)
                existingItem.Quantity += dto.Quantity;
            else
            {
                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null) return NotFound("Product not found");

                var newItem = new CartItem
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    UserId = userId
                };
                await _context.CartItems.AddAsync(newItem);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // GET /api/cart
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CartItem>>> GetCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            // Load CartItems, include Product, then include Product.Images
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                    .ThenInclude(p => p.Images)  // Key: load product images
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return cartItems;
        }

        // POST /api/cart/increment/{id}
        [HttpPost("increment/{id}")]
        public async Task<IActionResult> Increment(int id)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item == null) return NotFound();
            item.Quantity++;
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        // POST /api/cart/decrement/{id}
        [HttpPost("decrement/{id}")]
        public async Task<IActionResult> Decrement(int id)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item == null) return NotFound();
            if (item.Quantity > 1) item.Quantity--;
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        // POST /api/cart/update/{id}?quantity=5
        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateQuantity(int id, [FromQuery] int quantity)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item == null) return NotFound();
            if (quantity < 1) return BadRequest("Quantity must be greater than 0");

            item.Quantity = quantity;
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        // DELETE /api/cart/remove/{id}
        [HttpDelete("remove/{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item == null) return NotFound();
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("checkout")]
        //public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto, [FromServices] IOptions<EmailSettings> emailSettings)
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto, [FromServices] EmailService emailService)
        {
            //var settings = emailSettings.Value;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            if (dto.Items == null || !dto.Items.Any())
                return BadRequest("Please select at least one product");

            var itemIds = dto.Items.Select(i => i.Id).ToList();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId && itemIds.Contains(c.Id))
                .ToListAsync();

            if (!cartItems.Any()) return BadRequest("No valid cart items found");

            // Update quantities (based on frontend input)
            foreach (var dtoItem in dto.Items)
            {
                var cartItem = cartItems.FirstOrDefault(c => c.Id == dtoItem.Id);
                if (cartItem != null)
                {
                    cartItem.Quantity = dtoItem.Quantity;
                }
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound("User not found");

            Console.WriteLine($"[DEBUG] User ID: {user.Id}");
            Console.WriteLine($"[DEBUG] NewUserCouponUsed: {user.NewUserCouponUsed}");

            var phone = user?.PhoneNumber ?? string.Empty;

            // Calculate original total price
            decimal originalTotal = cartItems.Sum(c =>
            {
                var price = c.Product.DiscountPercent > 0
                    ? c.Product.Price * (1 - c.Product.DiscountPercent / 100m)
                    : c.Product.Price;
                return price * c.Quantity;
            });

            decimal discount = 0;

            // New user coupon (only once)
            if (dto.UseNewUserCoupon && !user.NewUserCouponUsed)
            {
                discount += 5;
                user.NewUserCouponUsed = true;
            }

            // Check 50/100 coupon mutual exclusivity
            if (dto.Use50Coupon && dto.Use100Coupon)
                return BadRequest("50 coupon and 100 coupon cannot be used together");

            // Accumulated spending + current order
            decimal accumulatedWithCurrent = user.AccumulatedAmount + originalTotal;
            if (dto.Use100Coupon && accumulatedWithCurrent >= 100)
            {
                discount += 10;
                user.AccumulatedAmount = 0; // Reset after using
            }
            else if (dto.Use50Coupon && accumulatedWithCurrent >= 50)
            {
                discount += 5;
                user.AccumulatedAmount = 0; // Reset after using
            }
            else
            {
                // If no 50/100 coupon is used, accumulate the amount
                user.AccumulatedAmount += originalTotal;
            }

            // Final total, cannot be less than 0
            decimal finalTotal = Math.Max(originalTotal - discount, 0);

            // Create order
            var order = new Order
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                TotalPrice = finalTotal,
                CustomerName = dto.CustomerName,
                CustomerEmail = dto.CustomerEmail,
                DeliveryMethod = dto.DeliveryMethod,
                ShippingAddress = dto.DeliveryMethod == "courier" ? dto.ShippingAddress : null,
                PhoneNumber = phone,        // Profile phone
                ContactPhone = dto.Phone,   // Checkout phone
                OrderItems = cartItems.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    Price = c.Product.DiscountPercent > 0
                            ? c.Product.Price * (1 - c.Product.DiscountPercent / 100m)
                            : c.Product.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            // ==================== EmailService ====================
            try
            {
                // User Email
                var userHtml = $@"
                <p>Hi {dto.CustomerName},</p>
                <p>Thank you for your order with <strong>Lovely Fish Aquarium</strong>. We have received your order successfully.</p>
                <h4>Order Details:</h4>
                <ul>{string.Join("", cartItems.Select(c => $"<li>{c.Product.Title} × {c.Quantity} - {c.Product.Price:C}</li>"))}</ul>
                <p>Original Total: {originalTotal:C}</p>
                <p>Discount: {discount:C}</p>
                <p><strong>Final Payment: {finalTotal:C}</strong></p>
                <p>Bank: {emailService.Settings.BankName}<br>
                Account Name: {emailService.Settings.AccountName}<br>
                Account Number: {emailService.Settings.AccountNumber}</p>
                <p>Please put your name and OrderId as reference.</p>
                <p>Thank you for choosing Lovely Fish Aquarium!</p>
                ";

                var userText = $@"
                Hi {dto.CustomerName},

                Thank you for your order with Lovely Fish Aquarium. We have received your order successfully.

                Order Details:
                {string.Join("\n", cartItems.Select(c => $"- {c.Product.Title} × {c.Quantity} - {c.Product.Price:C}"))}

                Original Total: {originalTotal:C}
                Discount: {discount:C}
                Final Payment: {finalTotal:C}

                Bank: {emailService.Settings.BankName}
                Account Name: {emailService.Settings.AccountName}
                Account Number: {emailService.Settings.AccountNumber}

                Please put your name and OrderId as reference.

                Thank you for choosing Lovely Fish Aquarium!
";

                await emailService.SendEmail(user.Email, dto.CustomerName, "Order Confirmation - LovelyFishAquarium", userHtml, userText);

                // Admin Email
                var adminHtml = $@"
                <h3>📢 New Order Notification</h3>
                <p><strong>Order ID:</strong> {order.Id}</p>
                <p><strong>Customer Name:</strong> {order.CustomerName}</p>
                <p><strong>Customer Email:</strong> {order.CustomerEmail}</p>
                <h4>Order Details:</h4>
                <ul>{string.Join("", cartItems.Select(c => $"<li>{c.Product.Title} × {c.Quantity} - {(c.Product.Price * c.Quantity):C}</li>"))}</ul>
                <p><strong>Final Payment: {finalTotal:C}</strong></p>
                <p>Please check the admin panel for more details.</p>
                ";

                var adminText = $@"
                📢 New Order Notification
                Order ID: {order.Id}
                Customer Name: {order.CustomerName}
                Customer Email: {order.CustomerEmail}
                Order Details:
                {string.Join("\n", cartItems.Select(c => $"- {c.Product.Title} × {c.Quantity} - {(c.Product.Price * c.Quantity):C}"))}
                Final Payment: {finalTotal:C}
                Please check the admin panel for more details.
                ";

                await emailService.SendEmail(emailService.Settings.AdminEmail, emailService.Settings.AdminName, "New Order Notification - LovelyFishAquarium", adminHtml, adminText);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[EmailService Error] " + ex.Message);
                
            }


            return Ok(new
            {
                orderId = order.Id,
                originalTotal,
                totalPrice = finalTotal,
                newUserUsed = user.NewUserCouponUsed,
                discount,
                order = new OrderDto
                {
                    Id = order.Id,
                    CreatedAt = order.CreatedAt,
                    TotalPrice = finalTotal,
                    CustomerName = order.CustomerName,
                    ShippingAddress = order.ShippingAddress,
                    PhoneNumber = order.PhoneNumber,
                    ContactPhone = order.ContactPhone,
                    Status = order.Status,
                    OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                    {
                        Id = oi.Id,
                        ProductName = oi.Product != null ? oi.Product.Title : "Deleted Product",
                        Quantity = oi.Quantity,
                        Price = oi.Price
                    }).ToList()
                }
            });
        }
    }
}

// Both user and admin receive email notifications
// Emails include product details, original price, discount, and final payment amount
// Emails are sent in both HTML and plain text versions
// Bank info (Bank + Account Name + Account Number) is configurable
// Email sending failure does not affect order saving
