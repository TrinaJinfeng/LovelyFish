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

var builder = WebApplication.CreateBuilder(args);

// -------------------
// 临时禁用 Azure Blob Trace Listener
// -------------------
builder.Logging.ClearProviders(); // 移除默认日志提供者
builder.Logging.AddConsole();     // 保留控制台日志


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

// inject services
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<BlobService>();
builder.Services.AddScoped<ITokenService, TokenService>();

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

Console.WriteLine("=== Environment Variables ===");
Console.WriteLine($"AZURE_SQL_SERVER: {dbServer}");
Console.WriteLine($"AZURE_SQL_DB:     {dbName}");
Console.WriteLine($"AZURE_SQL_USER:   {dbUser}");
Console.WriteLine("============================");

// Build final connection string
string finalConn = rawConn
        .Replace("%AZURE_SQL_SERVER%", dbServer)
        .Replace("%AZURE_SQL_DB%", dbName)
        .Replace("%AZURE_SQL_USER%", dbUser)
        .Replace("%AZURE_SQL_PASSWORD%", dbPass);


// Register DbContexts
builder.Services.AddDbContext<LovelyFishContext>(options =>
    options.UseSqlServer(finalConn, sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(finalConn, sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Identity configuration (only JWT)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT 配置
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

// Configure JwtSettings
builder.Services.Configure<JwtSettings>(options =>
{
    options.Secret = jwtSecret ?? throw new InvalidOperationException("JWT_SECRET environment variable is required");
    options.Issuer = jwtIssuer ?? throw new InvalidOperationException("JWT_ISSUER environment variable is required");
    options.Audience = jwtAudience ?? throw new InvalidOperationException("JWT_AUDIENCE environment variable is required");
    options.ExpiryMinutes = 60; // Default expiry time
});

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//            ValidIssuer = jwtIssuer,
//            ValidAudience = jwtAudience,
//            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),

//        };
//    });

//    });
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // ?? 必须加上这行
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
    };

    // ?? 阻止自动跳转 /Account/Login
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync("{\"message\": \"Unauthorized\"}");
        }
    };
});


// Blob storage settings
builder.Services.Configure<BlobSettings>(options =>
{
    options.ConnectionString = Environment.GetEnvironmentVariable("AZURE_BLOB_CONNECTION_STRING");
    options.ContainerName = "uploads";
});

var frontendBaseUrl = builder.Configuration["FRONTEND_BASE_URL"];

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
