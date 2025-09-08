using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Data;
using System.Text.Json;
using LovelyFish.API.Server.Models;
using Microsoft.AspNetCore.Identity;
using LovelyFish.API.Server.Data;
using DotNetEnv;
using LovelyFish.API.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// -------------------
// Load .env file (for local development)
// -------------------
Env.Load(); // If no .env exists locally, it won't throw an error

// -------------------
// Configuration sources
// -------------------
// Read appsettings.json first, then environment variables
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// -------------------
// Inject EmailSettings from environment variables
// -------------------
builder.Services.Configure<EmailSettings>(options =>
{
    options.BrevoApiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY");
    options.FromEmail = Environment.GetEnvironmentVariable("FROM_EMAIL");
    options.FromName = Environment.GetEnvironmentVariable("FROM_NAME");
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

// -------------------
// Identity configuration
// -------------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.Name = ".LovelyFish.AuthCookie";
    options.Cookie.SameSite = SameSiteMode.None;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
    options.LoginPath = "/api/account/login";
    options.LogoutPath = "/api/account/logout";

    // API request login redirect handling
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api") && context.Response.StatusCode == 200)
        {
            context.Response.StatusCode = 401;
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

    Console.WriteLine("[Seed] IdentitySeeder started");
    await IdentitySeeder.SeedAdminAsync(services);
}

app.Run();
