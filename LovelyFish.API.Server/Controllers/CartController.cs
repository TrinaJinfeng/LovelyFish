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
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto, [FromServices] IOptions<EmailSettings> emailSettings)
        {
            var settings = emailSettings.Value;
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
                CreatedAt = DateTime.Now,
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

            // ==================== Brevo Email Notifications ====================
            try
            {
                var brevoApiKey = emailSettings.Value.BrevoApiKey;
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("accept", "application/json");
                client.DefaultRequestHeaders.Add("api-key", brevoApiKey);

                // =========== User Email Content ==========
                string BuildUserHtmlContent(string name)
                {
                    var sb = new StringBuilder();
                    sb.Append($"<p>Hi {name},</p>");
                    sb.Append("<p>Thank you for your order with <strong>Lovely Fish Aquarium</strong>. We are pleased to confirm that we have received your order successfully.</p>");

                    sb.Append("<h4>Order Details:</h4><ul>");
                    foreach (var item in cartItems)
                        sb.Append($"<li>{item.Product.Title} × {item.Quantity} - {item.Product.Price:C}</li>");
                    sb.Append("</ul>");

                    sb.Append($"<p>Original Total: {originalTotal:C}</p>");
                    sb.Append($"<p>Discount: {discount:C}</p>");
                    sb.Append($"<p><strong>Final Payment: {finalTotal:C}</strong></p>");

                    sb.Append("<p>If you prefer to pick up your order, we will provide our store address. You may pay in cash or make a bank transfer using the following details:</p>");
                    sb.Append("<p><strong>Bank:</strong> " + emailSettings.Value.BankName + "<br>");
                    sb.Append("<strong>Account Name:</strong> " + emailSettings.Value.AccountName + "<br>");
                    sb.Append("<strong>Account Number:</strong> " + emailSettings.Value.AccountNumber + "</p>");

                    sb.Append("<p>If you prefer courier delivery, we will email you shortly with the updated total including courier fees.</p>");

                    //sb.Append($"<p><strong>Order Reference Number:</strong> {referenceNumber}</p>");
                    sb.Append("<p><strong>Please put your name and OrderId as reference </p>");
                    sb.Append("<p>Once your payment is confirmed, we will process and dispatch your order promptly. You can also track your order anytime in your account under <em>Orders</em>.</p>");
                    sb.Append("<p>Thank you for choosing Lovely Fish Aquarium!</p>");

                    return sb.ToString();
                }

                string BuildUserTextContent(string name)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Hi {name},");
                    sb.AppendLine();
                    sb.AppendLine("Thank you for your order with Lovely Fish Aquarium. We are pleased to confirm that we have received your order successfully.");
                    sb.AppendLine();
                    
                    sb.AppendLine();
                    sb.AppendLine("Order Details:");
                    foreach (var item in cartItems)
                        sb.AppendLine($"- {item.Product.Title} × {item.Quantity} - {item.Product.Price:C}");
                    sb.AppendLine();
                    sb.AppendLine($"Original Total: {originalTotal:C}");
                    sb.AppendLine($"Discount: {discount:C}");
                    sb.AppendLine($"Final Payment: {finalTotal:C}");
                    sb.AppendLine();
                    sb.AppendLine("If you prefer to pick up your order, we will provide our store address. You may pay in cash or make a bank transfer using the following details:");
                    sb.AppendLine($"Bank: {emailSettings.Value.BankName}");
                    sb.AppendLine($"Account Name: {emailSettings.Value.AccountName}");
                    sb.AppendLine($"Account Number: {emailSettings.Value.AccountNumber}");
                    
                    sb.AppendLine();
                    sb.AppendLine("If you prefer courier delivery, we will email you shortly with the updated total including courier fees.");
                    sb.AppendLine();
                    sb.AppendLine("Please put your name and OrderId as reference");
                    sb.AppendLine();
                    sb.AppendLine("Once your payment is confirmed, we will process and dispatch your order promptly. You can also track your order anytime in your account under Orders.");
                    sb.AppendLine();
                    sb.AppendLine("Thank you for choosing Lovely Fish Aquarium!");

                    return sb.ToString();
                }

                // ========= Admin Email Content ========
                string BuildAdminHtmlContent(Order order)
                {
                    var sb = new StringBuilder();
                    sb.Append("<h3>📢 New Order Notification</h3>");
                    sb.Append($"<p><strong>Order ID:</strong> {order.Id}</p>");
                    sb.Append($"<p><strong>Customer Name:</strong> {order.CustomerName}</p>");
                    sb.Append($"<p><strong>Customer Email:</strong> {order.CustomerEmail}</p>");
                    sb.Append("<h4>Order Details:</h4><ul>");
                    foreach (var item in cartItems)
                        sb.Append($"<li>{item.Product.Title} × {item.Quantity} - {(item.Product.Price * item.Quantity):C}</li>");
                    sb.Append("</ul>");
                    sb.Append($"<p><strong>Final Payment: {finalTotal:C}</strong></p>");
                    sb.Append("<p>Please check the admin panel for more details.</p>");
                    return sb.ToString();
                }

                string BuildAdminTextContent(Order order)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("📢 New Order Notification");
                    sb.AppendLine($"Order ID: {order.Id}");
                    sb.AppendLine($"Customer Name: {order.CustomerName}");
                    sb.AppendLine($"Customer Email: {order.CustomerEmail}");
                    sb.AppendLine("Order Details:");
                    foreach (var item in cartItems)
                        sb.AppendLine($"{item.Product.Title} × {item.Quantity} - {(item.Product.Price * item.Quantity):C}");
                    sb.AppendLine($"Final Payment: {finalTotal:C}");
                    sb.AppendLine("Please check the admin panel for more details.");
                    return sb.ToString();
                }

                // ====== Send Emails ======
                var userPayload = new
                {
                    sender = new { email = settings.FromEmail, name = settings.FromName },
                    to = new[] { new { email = user.Email, name = dto.CustomerName } },
                    subject = "Order Confirmation - LovelyFishAquarium",
                    htmlContent = BuildUserHtmlContent(dto.CustomerName),
                    textContent = BuildUserTextContent(dto.CustomerName)
                };
                var userContent = new StringContent(JsonSerializer.Serialize(userPayload), Encoding.UTF8, "application/json");
                await client.PostAsync("https://api.brevo.com/v3/smtp/email", userContent);

                var adminPayload = new
                {
                    sender = new { email = settings.FromEmail, name = settings.FromName },
                    to = new[] { new { email = settings.AdminEmail, name = settings.AdminName } },
                    subject = "New Order Notification - LovelyFishAquarium",
                    htmlContent = BuildAdminHtmlContent(order),
                    textContent = BuildAdminTextContent(order)
                };
                var adminContent = new StringContent(JsonSerializer.Serialize(adminPayload), Encoding.UTF8, "application/json");
                await client.PostAsync("https://api.brevo.com/v3/smtp/email", adminContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Brevo Email Error] " + ex.Message);
                // Email failure does not affect order saving
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
