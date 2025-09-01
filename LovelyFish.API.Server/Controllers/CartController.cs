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
                return BadRequest("参数无效");

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
                if (product == null) return NotFound("商品不存在");

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

            // 加载 CartItems，同时 Include Product，再 ThenInclude Product.Images
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                    .ThenInclude(p => p.Images)  // 关键：加载图片集合
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
            if (quantity < 1) return BadRequest("数量必须大于0");

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            if (dto.Items == null || !dto.Items.Any())
                return BadRequest("请至少选择一个商品");

            var itemIds = dto.Items.Select(i => i.Id).ToList();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId && itemIds.Contains(c.Id))
                .ToListAsync();

            if (!cartItems.Any()) return BadRequest("没有有效的购物车商品");

            // 更新数量（以前端传的为准）
            foreach (var dtoItem in dto.Items)
            {
                var cartItem = cartItems.FirstOrDefault(c => c.Id == dtoItem.Id);
                if (cartItem != null)
                {
                    cartItem.Quantity = dtoItem.Quantity;
                }
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound("用户不存在");

            Console.WriteLine($"[DEBUG] User ID: {user.Id}");
            Console.WriteLine($"[DEBUG] NewUserCouponUsed: {user.NewUserCouponUsed}");

            var phone = user?.PhoneNumber ?? string.Empty;

            // 计算原始订单总价
            decimal originalTotal = cartItems.Sum(c =>
            {
                var price = c.Product.DiscountPercent > 0
                    ? c.Product.Price * (1 - c.Product.DiscountPercent / 100m)
                    : c.Product.Price;
                return price * c.Quantity;
            });

            decimal discount = 0;

            // 新人卷（仅一次）
            if (dto.UseNewUserCoupon && !user.NewUserCouponUsed)
            {
                discount += 5;
                user.NewUserCouponUsed = true;
            }

            // 检查 50/100 卷互斥
            if (dto.Use50Coupon && dto.Use100Coupon)
                return BadRequest("50 coupon and 100 coupon cannot be used together");

            // 累计消费 + 本次订单
            decimal accumulatedWithCurrent = user.AccumulatedAmount + originalTotal;
            if (dto.Use100Coupon && accumulatedWithCurrent >= 100)
            {
                discount += 10;
                user.AccumulatedAmount = 0; // 使用后清零
            }
            else if (dto.Use50Coupon && accumulatedWithCurrent >= 50)
            {
                discount += 5;
                user.AccumulatedAmount = 0; // 使用后清零
            }
            else
            {
                // 没用 50/100 卷，累计金额累加
                user.AccumulatedAmount += originalTotal;
            }

            // 最终总价，不能小于 0
            decimal finalTotal = Math.Max(originalTotal - discount, 0);

            // 创建订单
            var order = new Order
            {
                UserId = userId,
                CreatedAt = DateTime.Now,
                TotalPrice = finalTotal,
                CustomerName = dto.CustomerName,
                CustomerEmail = dto.CustomerEmail,
                ShippingAddress = dto.ShippingAddress,
                PhoneNumber = phone,        // Profile 电话
                ContactPhone = dto.Phone,   // 下单页面电话
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

            // ==================== Brevo 邮件通知 ====================
            try
            {
                var brevoApiKey = emailSettings.Value.BrevoApiKey;
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("accept", "application/json");
                client.DefaultRequestHeaders.Add("api-key", brevoApiKey);

                // =========== 用户邮件内容 ==========
                string BuildUserHtmlContent(string name)
                {
                    var sb = new StringBuilder();
                    sb.Append($"<p>Hi {name},</p>");
                    sb.Append("<p>感谢您的订单！请通过以下方式进行转账支付：</p>");
                    sb.Append("<p><strong>Bank:</strong> " + emailSettings.Value.BankName + "<br>");
                    sb.Append("<strong>Account Name:</strong> " + emailSettings.Value.AccountName + "<br>");
                    sb.Append("<strong>Account Number:</strong> " + emailSettings.Value.AccountNumber + "</p>");
                    sb.Append("<h4>订单明细：</h4><ul>");

                    foreach (var item in cartItems)
                        sb.Append($"<li>{item.Product.Title} × {item.Quantity} - {item.Product.Price:C}</li>");
                    sb.Append("</ul>");
                    sb.Append($"<p>原始总价: {originalTotal:C}</p>");
                    sb.Append($"<p>折扣: {discount:C}</p>");
                    sb.Append($"<p><strong>最终付款金额: {finalTotal:C}</strong></p>");
                    return sb.ToString();
                }

                string BuildUserTextContent(string name)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Hi {name},");
                    sb.AppendLine("感谢您的订单！请通过以下方式进行转账支付：");
                    sb.AppendLine("Bank: " + emailSettings.Value.BankName);
                    sb.AppendLine("Account Name: " + emailSettings.Value.AccountName);
                    sb.AppendLine("Account Number: " + emailSettings.Value.AccountNumber);
                    sb.AppendLine("订单明细：");
                    foreach (var item in cartItems)
                        sb.AppendLine($"{item.Product.Title} × {item.Quantity} - {item.Product.Price:C}");
                    sb.AppendLine($"原始总价: {originalTotal:C}");
                    sb.AppendLine($"折扣: {discount:C}");
                    sb.AppendLine($"最终付款金额: {finalTotal:C}");
                    return sb.ToString();
                }


                // ========= 管理员邮件内容 ========
                string BuildAdminHtmlContent(Order order)
                {
                    var sb = new StringBuilder();
                    sb.Append("<h3>📢 新订单提醒</h3>");
                    sb.Append($"<p><strong>订单号:</strong> {order.Id}</p>");
                    sb.Append($"<p><strong>客户姓名:</strong> {order.CustomerName}</p>");
                    sb.Append($"<p><strong>客户邮箱:</strong> {order.CustomerEmail}</p>");
                    sb.Append("<h4>订单明细：</h4><ul>");
                    foreach (var item in cartItems)
                        sb.Append($"<li>{item.Product.Title} × {item.Quantity} - {(item.Product.Price * item.Quantity):C}</li>");
                    sb.Append("</ul>");
                    sb.Append($"<p><strong>最终付款金额: {finalTotal:C}</strong></p>");
                    sb.Append("<p>请尽快在后台查看订单详情。</p>");
                    return sb.ToString();
                }

                string BuildAdminTextContent(Order order)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("📢 新订单提醒");
                    sb.AppendLine($"订单号: {order.Id}");
                    sb.AppendLine($"客户姓名: {order.CustomerName}");
                    sb.AppendLine($"客户邮箱: {order.CustomerEmail}");
                    sb.AppendLine("订单明细：");
                    foreach (var item in cartItems)
                        sb.AppendLine($"{item.Product.Title} × {item.Quantity} - {(item.Product.Price * item.Quantity):C}");
                    sb.AppendLine($"最终付款金额: {finalTotal:C}");
                    sb.AppendLine("请尽快在后台查看订单详情。");
                    return sb.ToString();
                }

                // ====== 发送邮件 ======
                var userPayload = new
                {
                    sender = new { email = "lovelyfishaquarium@outlook.com", name = "LovelyFishAquarium" },
                    to = new[] { new { email = user.Email, name = dto.CustomerName } },
                    subject = "订单确认 - LovelyFishAquarium",
                    htmlContent = BuildUserHtmlContent(dto.CustomerName),
                    textContent = BuildUserTextContent(dto.CustomerName)
                };
                var userContent = new StringContent(JsonSerializer.Serialize(userPayload), Encoding.UTF8, "application/json");
                await client.PostAsync("https://api.brevo.com/v3/smtp/email", userContent);


                var adminPayload = new
                {
                    sender = new { email = "lovelyfishaquarium@outlook.com", name = "LovelyFishAquarium" },
                    to = new[] { new { email = "lovelyfishaquarium@outlook.com", name = "管理员" } },
                    subject = "新订单通知 - LovelyFishAquarium",
                    htmlContent = BuildAdminHtmlContent(order),
                    textContent = BuildAdminTextContent(order)
                };
                var adminContent = new StringContent(JsonSerializer.Serialize(adminPayload), Encoding.UTF8, "application/json");
                await client.PostAsync("https://api.brevo.com/v3/smtp/email", adminContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Brevo Email Error] " + ex.Message);
                // 邮件失败不影响订单保存
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


//用户和管理员同时收到邮件

//邮件显示商品明细、原价、折扣、最终付款金额

//HTML + 纯文本双版本

//银行信息（Bank + Account Name + Account Number）可配置化

//邮件发送失败不影响订单保存
