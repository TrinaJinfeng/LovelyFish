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

    private string GetImageUrl(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        return $"https://localhost:7148/uploads/{fileName}";
    }

        [HttpGet]
        public async Task<IActionResult> GetProducts(
                [FromQuery] string? search,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 12,
                [FromQuery] string? category = null,
                [FromQuery] bool? isClearance = null,   // 新增可选参数 isClearance
                [FromQuery] bool? isNewArrival = null)  // 新增可选参数 isNewArrival

        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .AsQueryable();

            // 搜索
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(lowerSearch));

            }

            // 按类别过滤
            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category.Name == category);


            // 仅新到产品
            if (isNewArrival.HasValue && isNewArrival.Value)
            {
                query = query.Where(p => p.IsNewArrival);
            }

            // 是否只返回清仓
            if (isClearance.HasValue && isClearance.Value)
            {
                query = query.Where(p => p.IsClearance);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var productsList = await query
                .OrderBy(p => p.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var productDtos = productsList.Select(p => new ProductDto
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Price,
                DiscountPercent = p.DiscountPercent,
                Stock = p.Stock,
                Description = p.Description,
                Features = p.Features.ToList(),
                CategoryId = p.CategoryId,
                CategoryTitle = p.Category?.Name,

                ImageUrls = p.Images.Select(i => i.FileName).ToList(),

               

                MainImageUrl = p.Images.FirstOrDefault() != null ? GetImageUrl(p.Images.First().FileName) : null,

                IsClearance = p.IsClearance,
                IsNewArrival = p.IsNewArrival
            }).ToList();

            return Ok(new
            {
                items = productDtos,
                totalPages,
                totalItems
            });
        }

        // ==================== 获取单个产品 ====================
        [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
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

            ImageUrls = product.Images.Select(i => i.FileName).ToList(), // 返回文件名
            

            MainImageUrl = product.Images.FirstOrDefault() != null ? GetImageUrl(product.Images.First().FileName) : null,
            IsClearance = product.IsClearance
        };

        return Ok(dto);
    }

    // ==================== 新增产品 ====================
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] ProductDto dto)
    {
        try
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

                Images = dto.ImageUrls?.Select(fileName => new ProductImage { FileName = fileName }).ToList()
                            ?? new List<ProductImage>()

                

            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var category = await _context.Categories.FindAsync(product.CategoryId);

            var resultDto = new ProductDto
            {
                Id = product.Id,
                Title = product.Title,
                Price = product.Price,
                DiscountPercent = product.DiscountPercent,
                Stock = product.Stock,
                Description = product.Description,
                Features = product.Features.ToList(),
                CategoryId = product.CategoryId,
                CategoryTitle = category?.Name,

                ImageUrls = product.Images.Select(i => i.FileName).ToList(),

                

                MainImageUrl = product.Images.FirstOrDefault() != null ? GetImageUrl(product.Images.First().FileName) : null,
                IsClearance = product.IsClearance
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, resultDto);
        }
        catch (Exception ex)
        {
            // 打印日志，不让 VS 崩溃
            Console.WriteLine($"CreateProduct 出错: {ex}");
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==================== 更新产品 ====================
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto dto)
    {
        try
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

            // ---- 更新图片 ----
            // 删除旧图片
            _context.ProductImages.RemoveRange(product.Images);

                //添加新图片
                if (dto.ImageUrls != null && dto.ImageUrls.Count > 0)
                {
                    foreach (var fileName in dto.ImageUrls)
                    {
                        product.Images.Add(new ProductImage
                        {
                            FileName = fileName,
                            ProductId = id
                        });
                    }
                }


                await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateProduct 出错: {ex}");
            return BadRequest(new { message = ex.Message });
        }
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



//图片上传 → uploads 文件夹

//前端选择文件 → ProductImageUpload → imageUrls 数组

//新增/编辑产品 → ProductImage 表同步更新

//前端显示图片列表