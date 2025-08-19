using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Server.Models;
using LovelyFish.API.Data;
using System.Security.Claims;
using LovelyFish.API.Server.Dtos;
using Swashbuckle.AspNetCore.SwaggerUI;
using LovelyFish.API.Server.Models.Dtos;
using System.Numerics;

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

            return await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();
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

        // POST /api/cart/estimate---> estimatediscount
        [HttpPost("estimate")]
        public async Task<IActionResult> Estimate([FromBody] CheckoutDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            if (dto.CartItemIds == null || !dto.CartItemIds.Any())
                return BadRequest("请至少选择一个商品");

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId && dto.CartItemIds.Contains(c.Id))
                .ToListAsync();

            if (!cartItems.Any())
                return BadRequest("没有有效的购物车商品");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound("用户不存在");

            // 计算总价和折扣逻辑（和 Checkout 一致）
            decimal originalTotal = cartItems.Sum(c => c.Quantity * c.Product.Price);
            decimal discount = 0;

            // 新用户券
            if (dto.UseNewUserCoupon && !user.NewUserCouponUsed)
                discount += 5;

            // 累积消费优惠（只能选一个满减券）
            decimal accumulatedWithCurrent = user.AccumulatedAmount + originalTotal;
            decimal fullReduction = 0;
            if (dto.Use100Coupon && accumulatedWithCurrent >= 100)
                fullReduction = 10;
            else if (dto.Use50Coupon && accumulatedWithCurrent >= 50)
                fullReduction = 5;

            discount += fullReduction;

            return Ok(new
            {
                totalQuantity = cartItems.Sum(c => c.Quantity),
                originalTotal,
                discount,
                finalTotal = Math.Max(originalTotal - discount, 0),
                canUseNewUserCoupon = !user.NewUserCouponUsed
            });
        } 

        // POST /api/cart/checkout ---> submit order
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            if (dto.CartItemIds == null || !dto.CartItemIds.Any())
                return BadRequest("请至少选择一个商品");

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId && dto.CartItemIds.Contains(c.Id))
                .ToListAsync();

            if (!cartItems.Any())
                return BadRequest("没有有效的购物车商品");

            // 取用户电话
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound("用户不存在");

            Console.WriteLine($"[DEBUG] User ID: {user.Id}");
            Console.WriteLine($"[DEBUG] NewUserCouponUsed: {user.NewUserCouponUsed}");

            var phone = user?.PhoneNumber ?? string.Empty;

            // 计算原始订单总价
            decimal originalTotal = cartItems.Sum(c => c.Quantity * c.Product.Price);

            // 计算折扣
            decimal discount = 0;

            // 新用户优惠 $5
            if (dto.UseNewUserCoupon && !user.NewUserCouponUsed)
            {
                discount += 5;
                user.NewUserCouponUsed = true;
            }

            // 累积消费优惠（按本次订单 + 累计消费判断）
            decimal accumulatedWithCurrent = user.AccumulatedAmount + originalTotal;

            if (dto.Use100Coupon && accumulatedWithCurrent >= 100)
            {
                discount += 10;
                user.AccumulatedAmount = 0; // 使用后清零
            }
            else if (dto.Use50Coupon && accumulatedWithCurrent >= 50)
            {
                discount += 5;
                user.AccumulatedAmount = 0;
            }
            else
            {
                // 如果没用优惠券，累计金额加本次订单
                user.AccumulatedAmount += originalTotal;
            }

            // 最终总价
            decimal finalTotal = Math.Max(originalTotal - discount, 0);


            var order = new Order
            {
                UserId = userId,
                CreatedAt = DateTime.Now,
                TotalPrice = finalTotal,
                CustomerName = dto.CustomerName,
                ShippingAddress = dto.ShippingAddress,
                PhoneNumber = phone,  // ← Profile电话
                ContactPhone = dto.Phone,          // ✅ ConfirmOrderPage电话 双层确认

                OrderItems = cartItems.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    Price = c.Product.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            return Ok(new { 
                orderId = order.Id,
                originalTotal,
                totalPrice = finalTotal,
                newUserUsed = user.NewUserCouponUsed,
                discount

            });
        }
    }

     

        
    
}



//把 ApplicationDbContext 替换为 LovelyFishContext，以确保能访问 CartItems 和 Products 表。

//新增了 POST /api/cart 接口，接收 { productId, quantity }，用于添加商品到购物车。

//保留了你原有的增减数量和删除接口。

//你前端调用添加商品接口时，用 POST 发送 productId 和 quantity，即可新增或增加数量。

//Submit order ：
//新增 Order / OrderItem 模型

//在 DbContext 注册

//创建 POST /api/cart/checkout 接口

//前端点击 Submit Order 按钮调用即可

//改动说明：

//新增了 CheckoutDto，前端可传递 CustomerName 和 ShippingAddress。

//Checkout 接口接收 [FromBody] CheckoutDto dto，然后把姓名和地址写入 Order 表。

//保留原来的购物车增减、更新、删除接口。

//提交订单后清空购物车，并返回 orderId

//前端调用 / cart / estimate 就能拿到 原价、折扣、最终价，不用再自己算。

///cart/checkout 完整生成订单，数据库状态更新。

//新人券 + 满减券逻辑统一在后端计算，保证前端显示与最终订单一致。

//累积消费优惠只能选一个满减券，逻辑和你之前描述一致。