using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Data;
using System.Text.Json;
using LovelyFish.API.Server.Models;
using Microsoft.AspNetCore.Identity;
using LovelyFish.API.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container. 控制器

builder.Services.AddControllers();
// Swagger 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注册自己的数据库上下文
builder.Services.AddDbContext<LovelyFishContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 添加 Identity 所用的 DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 添加 Identity 服务 指定用户和角色（使用 ApplicationUser 作为用户类型）
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 配置 Identity Cookie 认证安全策略
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // 仅 HTTPS 发送
    options.Cookie.Name = ".LovelyFish.AuthCookie";

    // 跨域时 SameSite 必须 None，否则 Cookie 不会带过去
    options.Cookie.SameSite = SameSiteMode.None;

    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;

    options.LoginPath = "/api/account/login";
    options.LogoutPath = "/api/account/logout";
});

// 配置 CORS：允许 React 前端携带 Cookie
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // 允许携带 Cookie
    });
});


var app = builder.Build();

// 开发环境启用 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// 静态文件（前端 React 构建产物）
app.UseDefaultFiles();
app.UseStaticFiles();

// 路由 & CORS
app.UseRouting();
app.UseCors("AllowReactApp");


// 认证中间件（新增）
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("/index.html");

//using (var scope = app.Services.CreateScope())
//{
//    DataSeeder.Seed(scope.ServiceProvider);
//}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LovelyFishContext>();

    // 确保数据库存在
    context.Database.EnsureCreated();
    Console.WriteLine("[Seed] 种子方法开始执行");
    DataSeeder.Seed(scope.ServiceProvider);

//    try
//    {
//        var product = new Product
//        {
//            Name = "Test Product",
//            Price = 199,
//            Output = 1000,
//            Wattage = 60,
//            Image = "test.jpg",
//            Category = "测试",
//            Features = "Just test"
//        };

//        context.Products.Add(product);
//        context.SaveChanges();
//        Console.WriteLine("[Test] 成功插入测试产品！");
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine("[Test] 插入失败：" + ex.Message);
//        if (ex.InnerException != null)
//            Console.WriteLine("Inner: " + ex.InnerException.Message);
//    }
}

app.Run();


//添加 Identity Cookie 的配置，确保：

//Cookie 是 HttpOnly，不能被 JS 访问

//只通过 HTTPS 发送

//设置 SameSite 为 Lax 或 Strict 防止 CSRF

//适当设置过期时间
//改动重点
//SameSite = None

//你的原来是 Lax，这在跨域请求带 Cookie 时会被浏览器直接拒绝。

//如果你以后要让别人用你的网站（不同域名），必须是 None + Secure = Always。

//AllowCredentials() 搭配 WithOrigins()

//WithOrigins("*") + AllowCredentials() 是不允许的（浏览器安全限制）。

//必须明确写出允许的域名列表。

//SecurePolicy = Always

//保证 Cookie 只能在 HTTPS 上传输。

//本地开发时如果没 HTTPS，可以暂时用 CookieSecurePolicy.SameAsRequest。

//跨域和认证顺序

//必须是：

//scss
//复制
//编辑
//app.UseRouting();
//app.UseCors();
//app.UseAuthentication();
//app.UseAuthorization();
//这个版本的效果
//登录时，后端会通过 Identity 设置 .LovelyFish.AuthCookie，浏览器保存到 Cookie。

//刷新页面时 Cookie 会自动带上，请求自动被认证。

//登出时 HttpContext.SignOutAsync() 会清掉 Cookie，用户完全退出。

//无论是本地开发还是将来上线到新西兰，别人用你的网站都能登录/登出正常。