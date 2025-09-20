using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Data;
using System.Text.Json;
using LovelyFish.API.Server.Models;
using Microsoft.AspNetCore.Identity;
using LovelyFish.API.Server.Data;
using DotNetEnv;
using LovelyFish.API.Server.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Net;
using System.Security.Principal;

var builder = WebApplication.CreateBuilder(args);

// Load .env file (for local development)
Env.Load();

// Configuration sources
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();


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

// Blob Settings
// -------------------
builder.Services.Configure<BlobSettings>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // 本地测试上传到 wwwroot/uploads
        options.ConnectionString = "";
        options.ContainerName = "uploads";
    }
    else
    {
        // 线上 Azure Blob
        options.ConnectionString = Environment.GetEnvironmentVariable("AZURE_BLOB_CONNECTION_STRING");
        options.ContainerName = "uploads";
    }
});
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
//var rawConn = builder.Configuration.GetConnectionString("DefaultConnection");
//var dbServer = Environment.GetEnvironmentVariable("AZURE_SQL_SERVER");
//var dbName = Environment.GetEnvironmentVariable("AZURE_SQL_DB");
//var dbUser = Environment.GetEnvironmentVariable("AZURE_SQL_USER");
//var dbPass = Environment.GetEnvironmentVariable("AZURE_SQL_PASSWORD");
string finalConn;
if (builder.Environment.IsDevelopment())
{
    // 本地数据库
    finalConn = builder.Configuration.GetConnectionString("DefaultConnection");
}
else
{
    // Azure 数据库
    var rawConn = builder.Configuration.GetConnectionString("DefaultConnection");
    var dbServer = Environment.GetEnvironmentVariable("AZURE_SQL_SERVER");
    var dbName = Environment.GetEnvironmentVariable("AZURE_SQL_DB");
    var dbUser = Environment.GetEnvironmentVariable("AZURE_SQL_USER");
    var dbPass = Environment.GetEnvironmentVariable("AZURE_SQL_PASSWORD");

    finalConn = rawConn
        .Replace("%AZURE_SQL_SERVER%", dbServer)
        .Replace("%AZURE_SQL_DB%", dbName)
        .Replace("%AZURE_SQL_USER%", dbUser)
        .Replace("%AZURE_SQL_PASSWORD%", dbPass);
}

// Blob storage settings
//builder.Services.Configure<BlobSettings>(options =>
//{
//    options.ConnectionString = Environment.GetEnvironmentVariable("AZURE_BLOB_CONNECTION_STRING");
//    options.ContainerName = "uploads";
//});


// Build final connection string
//string finalConn;
//if (!string.IsNullOrEmpty(dbUser) && !string.IsNullOrEmpty(dbPass))
//{
//    finalConn = rawConn
//        .Replace("%AZURE_SQL_SERVER%", dbServer)
//        .Replace("%AZURE_SQL_DB%", dbName)
//        .Replace("%AZURE_SQL_USER%", dbUser)
//        .Replace("%AZURE_SQL_PASSWORD%", dbPass);
//}
//else
//{
//    // Fallback to Windows authentication if user/pass not provided
//    finalConn = $"Server={dbServer};Database={dbName};Trusted_Connection=True;Encrypt=False;";
//}

// Register DbContexts
builder.Services.AddDbContext<LovelyFishContext>(options =>
    options.UseSqlServer(finalConn, sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(finalConn, sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Identity (User management, still needed for registration/login)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 注入 JwtSettings（从环境变量）
builder.Services.Configure<JwtSettings>(options =>
{
    options.Secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "默认本地秘钥";
    options.ExpireMinutes = 60; // 可自定义
});

// JWT Authentication (for both local and production)
var jwtSettings = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<JwtSettings>>().Value;
var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});


// CORS
var frontendBaseUrl = builder.Configuration["FRONTEND_BASE_URL"];
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

// JWT Authentication globally enabled
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
