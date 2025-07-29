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

        // 如果数据库中没有数据，就进行填充
        if (!context.Products.Any())
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "products.json");
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[DataSeeder] 文件 {filePath} 未找到。");
                return;
            }

            var json = File.ReadAllText(filePath);

            // 配置 Json 选项：忽略大小写
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            List<Product>? products;
            try
            {
                products = JsonSerializer.Deserialize<List<Product>>(json, options);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[DataSeeder] JSON 解析错误: {ex.Message}");
                return;
            }

            if (products == null || products.Count == 0)
            {
                Console.WriteLine("[DataSeeder] 没有可插入的数据。");
                return;
            }

            // 防止字符串为 null
            foreach (var p in products)
            {
                p.Name = p.Name ?? string.Empty;
                p.Image = p.Image ?? string.Empty;
                p.Category = p.Category ?? string.Empty;
                p.Features = p.Features ?? string.Empty;
            }

            context.Products.AddRange(products);
            context.SaveChanges();

            Console.WriteLine($"[DataSeeder] 成功插入 {products.Count} 条 Product 数据。");
        }
    }
}