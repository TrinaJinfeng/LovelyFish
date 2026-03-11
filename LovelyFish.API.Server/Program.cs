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
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

//Logging
builder.Logging.ClearProviders(); // Remove default logging providers
builder.Logging.AddConsole();     // Keep console logging


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


// Controllers and JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Avoid reference loop issues when serializing navigation properties
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });


// Swagger configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//// Database connection string setup
//var rawConn = builder.Configuration.GetConnectionString("DefaultConnection");

// PostgreSQL Connection
var pgHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
var pgDb = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "lovelyfish";
var pgUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "lovelyfishuser";
var pgPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "StrongPassword123";


// PostgreSQL connection string
string pgConn = $"Host={pgHost};Database={pgDb};Username={pgUser};Password={pgPass}";

// Register DbContexts using PostgreSQL
builder.Services.AddDbContext<LovelyFishContext>(options =>
    options.UseNpgsql(pgConn));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(pgConn));

// Identity & JWT
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT setting
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

// Configure JwtSettings
builder.Services.Configure<JwtSettings>(options =>
{
    options.Secret = jwtSecret ?? throw new InvalidOperationException("JWT_SECRET environment required");
    options.Issuer = jwtIssuer ?? throw new InvalidOperationException("JWT_ISSUER environment required");
    options.Audience = jwtAudience ?? throw new InvalidOperationException("JWT_AUDIENCE environment required");
    options.ExpiryMinutes = 60; // Default expiry time
});

Console.WriteLine($"[DEBUG] JWT Secret length: {Encoding.UTF8.GetBytes(jwtSecret).Length} bytes");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // 
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

    // Prevent auto-redirect to /Account/Login
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
    var uploadsPath = builder.Environment.IsDevelopment()
        ? Path.Combine(Directory.GetCurrentDirectory(), "uploads")
        : "/var/www/lovelyfish-backend/uploads";

    if (!Directory.Exists(uploadsPath))
    {
        Directory.CreateDirectory(uploadsPath);
    }

    options.UploadDirectory = uploadsPath;
});

//var frontendBaseUrl = builder.Configuration["FRONTEND_BASE_URL"];

var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_BASE_URL")
                        ?? builder.Configuration["FRONTEND_BASE_URL"]
                        ?? "http://localhost:3000";


//CORS configuration
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
Console.WriteLine($"Frontend URL: {frontendBaseUrl}");

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

var uploadsPath = builder.Environment.IsDevelopment()
    ? Path.Combine(Directory.GetCurrentDirectory(), "uploads")
    : "/var/www/lovelyfish-backend/uploads";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});
//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider(
//        "/var/www/lovelyfish-backend/uploads"),
//    RequestPath = "/uploads"
//});

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
