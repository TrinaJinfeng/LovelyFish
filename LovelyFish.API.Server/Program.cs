using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Data;
using System.Text.Json;
using LovelyFish.API.Server.Models;
using Microsoft.AspNetCore.Identity;
using LovelyFish.API.Server.Data;
using DotNetEnv;
using LovelyFish.API.Server.Services;

var builder = WebApplication.CreateBuilder(args);


// 加载 .env 文件（本地开发用）

Env.Load(); // 如果本地没有 .env 也不会报错


// 配置系统读取方式
// 优先读取 appsettings.json 再读取环境变量

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

//EmailService 里用 IOptions<EmailSettings> 获取配置。

//所有默认值（邮箱、银行信息）都写在 EmailSettings 里。

//控制器只负责调用 SendEmail，不用再写 HttpClient 逻辑。

// 注入 EmailSettings
builder.Services.Configure<EmailSettings>(options =>
{
    options.BrevoApiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY");
    options.FromEmail = Environment.GetEnvironmentVariable("FROM_EMAIL");
    options.FromName = Environment.GetEnvironmentVariable("FROM_NAME");
    options.BankName = Environment.GetEnvironmentVariable("BANK_NAME");
    options.AccountNumber = Environment.GetEnvironmentVariable("ACCOUNT_NUMBER");
    options.AccountName = Environment.GetEnvironmentVariable("ACCOUNT_NAME");
});

// 注入 EmailService
builder.Services.AddSingleton<EmailService>();


//Add services to the container. 控制器

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
       // options.JsonSerializerOptions.WriteIndented = true; // (可选) 格式化输出 JSON
    });
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
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest  //（本地开发 OK，上线要改成 Always）
        : CookieSecurePolicy.Always;
    options.Cookie.Name = ".LovelyFish.AuthCookie";

    // 跨域时 SameSite 必须 None，否则 Cookie 不会带过去
    options.Cookie.SameSite = SameSiteMode.None;

    options.ExpireTimeSpan = TimeSpan.FromHours(1);

    options.SlidingExpiration = true;

    options.LoginPath = "/api/account/login";
    options.LogoutPath = "/api/account/logout";

    options.Events.OnRedirectToLogin = context =>
    {
        // 如果是 API 请求，则返回 401 而不是重定向
        if (context.Request.Path.StartsWithSegments("/api") && context.Response.StatusCode == 200)
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        // 其他情况继续默认重定向
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
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
app.UseDefaultFiles();
app.UseStaticFiles();


app.UseRouting();
app.UseCors("AllowReactApp");



app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("/index.html");


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    //1. 产品数据 Seeder
    var context = services.GetRequiredService<LovelyFishContext>();
    context.Database.EnsureCreated();
    Console.WriteLine("[Seed] 产品种子方法开始执行");
    DataSeeder.Seed(services);

    //2. 管理员账号 Seeder
    Console.WriteLine("[Seed] IdentitySeeder 开始执行");
    await IdentitySeeder.SeedAdminAsync(services);  // 调用IdentitySeeder
}


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


app.Run();

