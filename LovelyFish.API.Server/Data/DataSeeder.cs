using System.Text.Json;
using LovelyFish.API.Data;
using LovelyFish.API.Server.Models;

public static class DataSeeder
{
    public static void Seed(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LovelyFishContext>();

        // 确保数据库存在
        context.Database.EnsureCreated();

        // JSON 文件路径
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "products.json");
        Console.WriteLine($"[DataSeeder] 尝试读取: {filePath}");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[DataSeeder] 文件未找到: {filePath}");
            return;
        }

        var json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        List<ProductJson>? products;
        try
        {
            products = JsonSerializer.Deserialize<List<ProductJson>>(json, options);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[DataSeeder] JSON 解析错误: {ex.Message}");
            return;
        }

        if (products == null || products.Count == 0)
        {
            Console.WriteLine("[DataSeeder] 没有数据可插入。");
            return;
        }

        int insertCount = 0;

        foreach (var pj in products)
        {
            // 默认字段
            var title = pj.Name ?? string.Empty;
            var price = Convert.ToDecimal(pj.Price);
            var description = pj.Features ?? string.Empty;
            var discountPercent = pj.DiscountPercent ?? 0;
            var isClearance = pj.IsClearance ?? false;

            // 找到或创建分类
            var categoryName = pj.Category ?? "Uncategorized";
            var category = context.Categories.FirstOrDefault(c => c.Name == categoryName);
            if (category == null)
            {
                category = new Category { Name = categoryName };
                context.Categories.Add(category);
                context.SaveChanges();
            }



            // 检查是否已存在，如果之前数据库里重复数据没有图片，可能会被 DataSeeder 认为是新产品。
            bool exists = context.Products
                  .Any(p => p.Title == title);

            if (exists)
                continue; // 已存在，跳过
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

            // 处理 Features
            if (!string.IsNullOrEmpty(pj.Features))
            {
                product.Features = pj.Features.Split(',')
                                             .Select(f => f.Trim())
                                             .ToArray();
            }

            // 处理主图
            if (!string.IsNullOrEmpty(pj.Image))
            {
                var imgUrl = pj.Image.Replace("..\\", "/").Replace("../", "/");
                product.Images.Add(new ProductImage { FileName = imgUrl });
            }

            context.Products.Add(product);
            insertCount++;
        }

        if (insertCount > 0)
        {
            context.SaveChanges();
            Console.WriteLine($"[DataSeeder] 成功插入 {insertCount} 条产品数据。");
        }
        else
        {
            Console.WriteLine("[DataSeeder] 没有新产品需要插入。");
        }
    }

    // JSON 临时映射类
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
