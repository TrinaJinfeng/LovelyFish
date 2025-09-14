using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Data;
using System.Text.Json;
using LovelyFish.API.Server.Models;
using Microsoft.AspNetCore.Identity;
using LovelyFish.API.Server.Data;
using DotNetEnv;
using LovelyFish.API.Server.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Load .env file (for local development)
Env.Load(); 

// Configuration sources
// Read appsettings.json first, then environment variables
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();


// Inject EmailSettings from environment variables
builder.Services.Configure<EmailSettings>(options =>
{
    options.BrevoApiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY");
    options.FromEmail = Environment.GetEnvironmentVariable("FROM_EMAIL");
    options.FromName = Environment.GetEnvironmentVariable("FROM_NAME");
    options.AdminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
    options.AdminName = Environment.GetEnvironmentVariable("ADMIN_NAME");
    options.BankName = Environment.GetEnvironmentVariable("BANK_NAME");
    options.AccountNumber = Environment.GetEnvironmentVariable("ACCOUNT_NUMBER");
    options.AccountName = Environment.GetEnvironmentVariable("ACCOUNT_NAME");
    options.FrontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL");
    options.ApiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL");

});

// Register EmailService as singleton
builder.Services.AddSingleton<EmailService>();

// Register BlobService (file upload service) as singleton
builder.Services.AddSingleton<BlobService>();

// -------------------
// Controllers and JSON options
// -------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Avoid reference loop issues when serializing navigation properties
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// -------------------
// Swagger configuration
// -------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -------------------
// Database connection string setup
// -------------------
var rawConn = builder.Configuration.GetConnectionString("DefaultConnection");
var dbServer = Environment.GetEnvironmentVariable("AZURE_SQL_SERVER");
var dbName = Environment.GetEnvironmentVariable("AZURE_SQL_DB");
var dbUser = Environment.GetEnvironmentVariable("AZURE_SQL_USER");
var dbPass = Environment.GetEnvironmentVariable("AZURE_SQL_PASSWORD");

// Blob storage settings
builder.Services.Configure<BlobSettings>(options =>
{
    options.ConnectionString = Environment.GetEnvironmentVariable("AZURE_BLOB_CONNECTION_STRING");
    options.ContainerName = "uploads";
});

var frontendBaseUrl = builder.Configuration["FRONTEND_BASE_URL"];



// Build final connection string
string finalConn;
if (!string.IsNullOrEmpty(dbUser) && !string.IsNullOrEmpty(dbPass))
{
    finalConn = rawConn
        .Replace("%AZURE_SQL_SERVER%", dbServer)
        .Replace("%AZURE_SQL_DB%", dbName)
        .Replace("%AZURE_SQL_USER%", dbUser)
        .Replace("%AZURE_SQL_PASSWORD%", dbPass);
}
else
{
    // Fallback to Windows authentication if user/pass not provided
    finalConn = $"Server={dbServer};Database={dbName};Trusted_Connection=True;Encrypt=False;";
}

// Register DbContexts
builder.Services.AddDbContext<LovelyFishContext>(options =>
    options.UseSqlServer(finalConn, sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(finalConn, sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Identity configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.None // Use None in the development environment to avoid iPhone rejecting cookies over HTTP; otherwise, use CookieSecurePolicy.Always
        : CookieSecurePolicy.Always;

    options.Cookie.Name = ".LovelyFish.AuthCookie";

    // ?? 修改 SameSite 兼容开发环境手机登录
    options.Cookie.SameSite = builder.Environment.IsDevelopment()
        ? SameSiteMode.Lax          // ?? 开发环境 Lax
        : SameSiteMode.None;        // ?? 生产环境 None

    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
    options.LoginPath = "/api/account/login";
    options.LogoutPath = "/api/account/logout";

    // ?? 新增日志记录和 API 返回 401 修复
    options.Events.OnValidatePrincipal = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        if (!context.Principal.Identity.IsAuthenticated)
        {
            logger.LogWarning("Cookie validation failed for user {User}", context.Principal?.Identity?.Name ?? "Unknown");
        }
        return Task.CompletedTask;
    };

    // ?? 修复 OnRedirectToLogin，不依赖 StatusCode==200
    options.Events.OnRedirectToLogin = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            // ?? API 请求未认证 → 返回 401
            logger.LogWarning("API request unauthorized: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        // 页面请求 → 重定向到登录页
        logger.LogInformation("Redirecting to login page: {Path}", context.Request.Path);
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

    // ?? 可选：API 权限不足返回 403
    options.Events.OnRedirectToAccessDenied = context =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Access denied for path: {Path}", context.Request.Path);

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});




//CORS configuration(local + Azure front-end)
//-------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(frontendBaseUrl)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});


// Build and configure app
// -------------------

var app = builder.Build();


// Middleware
// -------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LovelyFish API V1");
    c.RoutePrefix = "swagger"; // Swagger UI URL: /swagger

});

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Fallback for SPA routing
app.MapFallbackToFile("/index.html");


// Database seeding
// -------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<LovelyFishContext>();
    context.Database.EnsureCreated();

    Console.WriteLine("[Seed] Product seeder started");
    DataSeeder.Seed(services);

    var emailSettings = services.GetRequiredService<IOptions<EmailSettings>>();
    Console.WriteLine("[Seed] IdentitySeeder started");
    await IdentitySeeder.SeedAdminAsync(services,emailSettings);
}

app.Run();
