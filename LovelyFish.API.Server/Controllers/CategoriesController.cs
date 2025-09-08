using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Data;
using LovelyFish.API.Server.Models;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly LovelyFishContext _context;

    public CategoriesController(LovelyFishContext context)
    {
        _context = context;
    }

    // GET: api/categories
    // Retrieve all categories, including their related products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        // Use Include to eager load related products in a single query
        return await _context.Categories
                             .Include(c => c.Products)
                             .ToListAsync();
    }

    // POST: api/categories
    // Create a new category
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] Category category)
    {
        // Add category to the DbContext
        _context.Categories.Add(category);
        // Save changes to database asynchronously
        await _context.SaveChangesAsync();
        // Return the created category
        return Ok(category);
    }

    // PUT: api/categories/{id}
    // Update the name of an existing category
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
    {
        // Find the category by ID
        var cat = await _context.Categories.FindAsync(id);
        if (cat == null) return NotFound(); // Return 404 if not found

        // Update the category name
        cat.Name = category.Name;
        await _context.SaveChangesAsync();
        return Ok(cat);
    }

    // DELETE: api/categories/{id}
    // Delete a category by ID
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        // Find the category
        var cat = await _context.Categories.FindAsync(id);
        if (cat == null) return NotFound();

        // Remove category from DbContext and save changes
        _context.Categories.Remove(cat);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
