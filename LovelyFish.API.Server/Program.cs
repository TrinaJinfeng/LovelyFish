using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Data;
using System.Text.Json;
using LovelyFish.API.Server.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<LovelyFishContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000") // React 前端地址
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors(); 

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

using (var scope = app.Services.CreateScope())
{
    DataSeeder.Seed(scope.ServiceProvider);
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LovelyFishContext>();

    // 确保数据库存在
    context.Database.EnsureCreated();

    try
    {
        var product = new Product
        {
            Name = "Test Product",
            Price = 199,
            Output = 1000,
            Wattage = 60,
            Image = "test.jpg",
            Category = "测试",
            Features = "Just test"
        };

        context.Products.Add(product);
        context.SaveChanges();
        Console.WriteLine("[Test] 成功插入测试产品！");
    }
    catch (Exception ex)
    {
        Console.WriteLine("[Test] 插入失败：" + ex.Message);
        if (ex.InnerException != null)
            Console.WriteLine("Inner: " + ex.InnerException.Message);
    }
}

app.Run();
