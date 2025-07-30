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

        // 读取 JSON 文件
        var filePath = Path.Combine(AppContext.BaseDirectory, "Data", "products.json");

        Console.WriteLine($"[DataSeeder] 当前尝试读取路径: {filePath}");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[DataSeeder] 文件 {filePath} 未找到。");
            return;
        }

        var json = File.ReadAllText(filePath);
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

        int insertCount = 0;

        foreach (var p in products)
        {
            p.Name = p.Name ?? string.Empty;
            p.Image = p.Image ?? string.Empty;
            p.Category = p.Category ?? string.Empty;
            p.Features = p.Features ?? string.Empty;

            // 检查是否已存在：用 Name 作为唯一标识（你也可以改成 ID 或 Name+Category）
            bool exists = context.Products.Any(x => x.Name == p.Name);
            if (!exists)
            {
                context.Products.Add(p);
                insertCount++;
            }
        }

        if (insertCount > 0)
        {
            context.SaveChanges();
            Console.WriteLine($"[DataSeeder] 成功追加 {insertCount} 条新产品数据。");
        }
        else
        {
            Console.WriteLine("[DataSeeder] 没有新产品需要插入。");
        }
    }
}
