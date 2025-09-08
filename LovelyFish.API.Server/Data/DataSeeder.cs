using System.Text.Json;
using LovelyFish.API.Data;
using LovelyFish.API.Server.Models;

public static class DataSeeder
{
    // Seed initial data into the database
    public static void Seed(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LovelyFishContext>();

        // Ensure database is created
        context.Database.EnsureCreated();

        //// Only import Json when Products is null
        //if (context.Products.Any())
        //{
        //    Console.WriteLine("[DataSeeder] Products table not empty, skipping JSON import.");
        //    return;
        //}

        // JSON file path
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "products.json");
        Console.WriteLine($"[DataSeeder] Trying to read: {filePath}");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[DataSeeder] File not found: {filePath}");
            return;
        }

        // Read JSON content
        var json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        List<ProductJson>? products;
        try
        {
            // Deserialize JSON to list of ProductJson
            products = JsonSerializer.Deserialize<List<ProductJson>>(json, options);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[DataSeeder] JSON parsing error: {ex.Message}");
            return;
        }

        if (products == null || products.Count == 0)
        {
            Console.WriteLine("[DataSeeder] No data to insert.");
            return;
        }

        int insertCount = 0;

        foreach (var pj in products)
        {
            // Default fields
            var title = pj.Name ?? string.Empty;
            var price = Convert.ToDecimal(pj.Price);
            var description = pj.Features ?? string.Empty;
            var discountPercent = pj.DiscountPercent ?? 0;
            var isClearance = pj.IsClearance ?? false;

            // Find or create category
            var categoryName = pj.Category ?? "Uncategorized";
            var category = context.Categories.FirstOrDefault(c => c.Name == categoryName);
            if (category == null)
            {
                category = new Category { Name = categoryName };
                context.Categories.Add(category);
                context.SaveChanges();
            }

            // Skip if product already exists (by title)
            bool exists = context.Products.Any(p => p.Title == title);
            if (exists)
                continue;

            var product = new Product
            {
                Title = title,
                Price = price,
                Description = description,
                DiscountPercent = discountPercent,
                IsClearance = isClearance,
                CategoryId = category.Id,
                Stock = 10
            };

            // Handle Features
            if (!string.IsNullOrEmpty(pj.Features))
            {
                product.Features = pj.Features.Split(',')
                                             .Select(f => f.Trim())
                                             .ToArray();
            }

            // Handle main image
            if (!string.IsNullOrEmpty(pj.Image))
            {
                var imgUrl = pj.Image.Replace("..\\", "/").Replace("../", "/");
                product.Images.Add(new ProductImage { FileName = imgUrl });
            }

            context.Products.Add(product);
            insertCount++;
        }

        // Save changes if any products were inserted
        if (insertCount > 0)
        {
            context.SaveChanges();
            Console.WriteLine($"[DataSeeder] Successfully inserted {insertCount} product(s).");
        }
        else
        {
            Console.WriteLine("[DataSeeder] No new products to insert.");
        }
    }

    // Temporary JSON mapping class
    private class ProductJson
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Image { get; set; }
        public string Category { get; set; }
        public string Features { get; set; }
        public int? DiscountPercent { get; set; }
        public bool? IsClearance { get; set; }
    }
}
