using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Server.Models;
using LovelyFish.API.Data;
using System.Security.Claims;  

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
        // 添加商品到购物车，如果已有该商品，数量加一
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddCartItemDto dto)
        {
            if (dto == null || dto.ProductId <= 0 || dto.Quantity <= 0)
            {
                return BadRequest("参数无效");
            }

            // 获取当前登录用户ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            // 查找购物车里当前用户是否已有该商品
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ProductId == dto.ProductId && c.UserId == userId);

            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
            }
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
            return await _context.CartItems.Include(c => c.Product).ToListAsync();
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
    }

    // 简单 DTO 用于接收添加购物车请求数据
    public class AddCartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}



//把 ApplicationDbContext 替换为 LovelyFishContext，以确保能访问 CartItems 和 Products 表。

//新增了 POST /api/cart 接口，接收 { productId, quantity }，用于添加商品到购物车。

//保留了你原有的增减数量和删除接口。

//你前端调用添加商品接口时，用 POST 发送 productId 和 quantity，即可新增或增加数量。