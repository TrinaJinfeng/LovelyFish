using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Server.Models;
using LovelyFish.API.Data;
using System.Security.Claims;
using LovelyFish.API.Server.Dtos;
using Swashbuckle.AspNetCore.SwaggerUI;

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

        // POST /api/cart/checkout
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return BadRequest("购物车为空");

            var order = new Order
            {
                UserId = userId,
                CreatedAt = DateTime.Now,
                TotalPrice = cartItems.Sum(c => c.Quantity * c.Product.Price),
                CustomerName = dto.CustomerName,
                ShippingAddress = dto.ShippingAddress,
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

            return Ok(new { orderId = order.Id });
        }
    }

        // DTO 用于添加购物车
        public class AddCartItemDto
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; } = 1;
        }

        // DTO 用于提交订单
        public class CheckoutDto
        {
            public string CustomerName { get; set; } = string.Empty;
            public string ShippingAddress { get; set; } = string.Empty;
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