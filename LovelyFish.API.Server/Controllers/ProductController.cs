using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Data;
using LovelyFish.API.Server.Models;
using LovelyFish.API.Server.Models.Dtos;

namespace LovelyFish.API.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly LovelyFishContext _context;

        public ProductController(LovelyFishContext context)
        {
            _context = context;
        }

        // ==================== 私有方法：生成完整图片 URL ====================
        private string GetImageUrl(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            relativePath = relativePath.TrimStart('/'); // 去掉开头的斜杠
            return $"https://localhost:7148/upload/{relativePath}";
        }

        // ==================== 获取所有产品 ====================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .ToListAsync();

            // 转换成 DTO 并添加 MainImageUrl
            var productDtos = products.Select(p => new ProductDto
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Price,
                DiscountPercent = p.DiscountPercent,
                Stock = p.Stock,
                Description = p.Description,
                Features = p.Features.ToList(), // 转成 List<string> 给前端
                CategoryId = p.CategoryId,
                CategoryTitle = p.Category?.Name,
                ImageUrls = p.Images
                    .Where(i => !string.IsNullOrEmpty(i.Url))
                    .Select(i => GetImageUrl(i.Url))
                    .ToList(),
                MainImageUrl = p.Images.FirstOrDefault() != null
                    ? GetImageUrl(p.Images.First().Url)
                    : null,
                IsClearance = p.IsClearance
            });


            return Ok(productDtos);
        }

        // ==================== 获取单个产品 ====================
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var dto = new ProductDto
            {
                Id = product.Id,
                Title = product.Title,
                Price = product.Price,
                DiscountPercent = product.DiscountPercent,
                Stock = product.Stock,
                Description = product.Description,
                Features = product.Features.ToList(),
                CategoryId = product.CategoryId,
                CategoryTitle = product.Category?.Name,
                ImageUrls = product.Images
                    .Where(i => !string.IsNullOrEmpty(i.Url))
                    .Select(i => $"https://localhost:7148/upload/{i.Url}")
                    .ToList(),
                MainImageUrl = product.Images.FirstOrDefault() != null
                    ? $"https://localhost:7148/upload/{product.Images.First().Url}"
                    : null,
                IsClearance = product.IsClearance
            };


            return Ok(dto);
        }

        // ==================== 新增产品 ====================
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct([FromBody] ProductDto dto)
        {
            var product = new Product
            {
                Title = dto.Title,
                Price = dto.Price,
                DiscountPercent = dto.DiscountPercent,
                Stock = dto.Stock,
                Description = dto.Description,
                Features = dto.Features?.ToArray() ?? Array.Empty<string>(),
                CategoryId = dto.CategoryId,
                IsClearance = dto.IsClearance,
                Images = dto.ImageUrls?.Select(url => new ProductImage { Url = url }).ToList() ?? new List<ProductImage>()
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // 返回 DTO
            var resultDto = new ProductDto
            {
                Id = product.Id,
                Title = product.Title,
                Price = product.Price,
                DiscountPercent = product.DiscountPercent,
                Stock = product.Stock,
                Description = product.Description,
                Features = product.Features.ToList(), // 转成 List<string>
                CategoryId = product.CategoryId,
                CategoryTitle = (await _context.Categories.FindAsync(product.CategoryId))?.Name,
                ImageUrls = product.Images
                    .Where(i => !string.IsNullOrEmpty(i.Url))
                    .Select(i => GetImageUrl(i.Url))
                    .ToList(),
                MainImageUrl = product.Images.FirstOrDefault() != null
                    ? GetImageUrl(product.Images.First().Url)
                    : null,
                IsClearance = product.IsClearance
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, resultDto);
        }

        // ==================== 更新产品 ====================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto dto)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            product.Title = dto.Title;
            product.Price = dto.Price;
            product.DiscountPercent = dto.DiscountPercent;
            product.Stock = dto.Stock;
            product.Description = dto.Description;
            product.Features = dto.Features?.ToArray() ?? Array.Empty<string>();
            product.CategoryId = dto.CategoryId;
            product.IsClearance = dto.IsClearance;

            // 更新图片
            product.Images.Clear();
            if (dto.ImageUrls != null)
            {
                product.Images = dto.ImageUrls.Select(url => new ProductImage { Url = url, ProductId = id }).ToList();
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ==================== 删除产品 ====================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ==================== 获取所有分类 ====================
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<Category>>> GetProductCategories()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .ToListAsync();

            return Ok(categories);
        }
    }
}
