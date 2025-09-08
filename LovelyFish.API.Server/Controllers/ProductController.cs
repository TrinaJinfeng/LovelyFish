using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Data;
using LovelyFish.API.Server.Models;
using LovelyFish.API.Server.Models.Dtos;
using Microsoft.Extensions.Options;
using LovelyFish.API.Server.Services;

namespace LovelyFish.API.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly LovelyFishContext _context;
        private readonly string _apiBaseUrl;

        // Constructor: inject DbContext and EmailSettings to get API base URL
        public ProductController(LovelyFishContext context, IOptions<EmailSettings> emailSettings)
        {
            _context = context;
            _apiBaseUrl = emailSettings.Value.ApiBaseUrl;
        }

        //// Build full URL for product image
        //private string GetImageUrl(string fileName)
        //{
        //    if (string.IsNullOrEmpty(fileName)) return null;
        //    return $"{_apiBaseUrl}/uploads/{fileName}";
        //}

        // ==================== Get products with filters and pagination ====================
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] string? category = null,
            [FromQuery] bool? isClearance = null,
            [FromQuery] bool? isNewArrival = null)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .AsQueryable();

            // Search by product title
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(lowerSearch));
            }

            // Filter by category
            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category.Name == category);

            // Filter new arrivals
            if (isNewArrival.HasValue && isNewArrival.Value)
                query = query.Where(p => p.IsNewArrival);

            // Filter clearance items
            if (isClearance.HasValue && isClearance.Value)
                query = query.Where(p => p.IsClearance);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var productsList = await query
                .OrderBy(p => p.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map entities to DTOs
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
                MainImageUrl = p.Images.FirstOrDefault()?.FileName,
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

        // ==================== Get single product by ID ====================
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
                ImageUrls = product.Images.Select(i => i.FileName).ToList(),
                MainImageUrl = product.Images.FirstOrDefault()?.FileName,
                IsClearance = product.IsClearance
            };

            return Ok(dto);
        }

        // ==================== Create new product ====================
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
                             ?? new List<ProductImage>() // from ProductImages
                };

                _context.Products.Add(product); // add to Products Table
                await _context.SaveChangesAsync();

                var category = await _context.Categories.FindAsync(product.CategoryId);

                var resultDto = new ProductDto //return to Frontend
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
                    ImageUrls = product.Images.Select(i => i.FileName).ToList(),//all url list
                    MainImageUrl = product.Images.FirstOrDefault()?.FileName, // first pic url
                    IsClearance = product.IsClearance
                };

                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, resultDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateProduct Error: {ex}");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==================== Update existing product ====================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto dto, [FromServices] BlobService blobService)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null) return NotFound();

                // Update product info
                product.Title = dto.Title;
                product.Price = dto.Price;
                product.DiscountPercent = dto.DiscountPercent;
                product.Stock = dto.Stock;
                product.Description = dto.Description;
                product.Features = dto.Features?.ToArray() ?? Array.Empty<string>();
                product.CategoryId = dto.CategoryId;
                product.IsClearance = dto.IsClearance;

                // Update images-->ProductImage
                //_context.ProductImages.RemoveRange(product.Images); // Remove old images
                //if (dto.ImageUrls != null && dto.ImageUrls.Count > 0)
                //{
                //    foreach (var fileName in dto.ImageUrls)
                //    {
                //        product.Images.Add(new ProductImage
                //        {
                //            FileName = fileName,
                //            ProductId = id
                //        });
                //    }
                //}
                // ---------------- Update pictures ----------------
                var existingUrls = product.Images.Select(i => i.FileName).ToList();
                var newUrls = dto.ImageUrls ?? new List<string>();

                // get the pictures going to delete (not from frontend, but existing in sql)
                var urlsToDelete = existingUrls.Except(newUrls).ToList();
                if (urlsToDelete.Any())
                {
                    var imagesToRemove = product.Images.Where(i => urlsToDelete.Contains(i.FileName)).ToList();
                    _context.ProductImages.RemoveRange(imagesToRemove); //delete ProductImage record

                    // delete from blob storage
                    foreach (var url in urlsToDelete)
                    {
                        try
                        {
                            await blobService.DeleteFileAsync(url);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to delete blob {url}: {ex.Message}");
                        }
                    }
                }

                // pictures to add (from frontend, but not in sql)
                var urlsToAdd = newUrls.Except(existingUrls).ToList();
                foreach (var fileName in urlsToAdd)
                {
                    product.Images.Add(new ProductImage
                    {
                        FileName = fileName,
                        ProductId = id
                    });
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateProduct Error: {ex}");
                return BadRequest(new { message = ex.Message });
            }
        }

        // ==================== Delete product ====================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id, [FromServices] BlobService blobService)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // delete all product images from blob storage
            foreach (var image in product.Images)
            {
                try
                {
                    await blobService.DeleteFileAsync(image.FileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete blob {image.FileName}: {ex.Message}");
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync(); //delete ProductImage record
            return NoContent();
        }

        // ==================== Get all product categories ====================
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
