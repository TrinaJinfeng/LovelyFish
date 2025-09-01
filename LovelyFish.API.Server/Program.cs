using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Data;
using System.Text.Json;
using LovelyFish.API.Server.Models;
using Microsoft.AspNetCore.Identity;
using LovelyFish.API.Server.Data;
using DotNetEnv;
using LovelyFish.API.Server.Services;

var builder = WebApplication.CreateBuilder(args);


// ���� .env �ļ������ؿ����ã�

Env.Load(); // �������û�� .env Ҳ���ᱨ��


// ����ϵͳ��ȡ��ʽ
// ���ȶ�ȡ appsettings.json �ٶ�ȡ��������

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

//EmailService ���� IOptions<EmailSettings> ��ȡ���á�

//����Ĭ��ֵ�����䡢������Ϣ����д�� EmailSettings �

//������ֻ������� SendEmail��������д HttpClient �߼���

// ע�� EmailSettings
builder.Services.Configure<EmailSettings>(options =>
{
    options.BrevoApiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY");
    options.FromEmail = Environment.GetEnvironmentVariable("FROM_EMAIL");
    options.FromName = Environment.GetEnvironmentVariable("FROM_NAME");
    options.BankName = Environment.GetEnvironmentVariable("BANK_NAME");
    options.AccountNumber = Environment.GetEnvironmentVariable("ACCOUNT_NUMBER");
    options.AccountName = Environment.GetEnvironmentVariable("ACCOUNT_NAME");
});

// ע�� EmailService
builder.Services.AddSingleton<EmailService>();


//Add services to the container. ������

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
       // options.JsonSerializerOptions.WriteIndented = true; // (��ѡ) ��ʽ����� JSON
    });
// Swagger 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ע���Լ������ݿ�������
builder.Services.AddDbContext<LovelyFishContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ��� Identity ���õ� DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ��� Identity ���� ָ���û��ͽ�ɫ��ʹ�� ApplicationUser ��Ϊ�û����ͣ�
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ���� Identity Cookie ��֤��ȫ����
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest  //�����ؿ��� OK������Ҫ�ĳ� Always��
        : CookieSecurePolicy.Always;
    options.Cookie.Name = ".LovelyFish.AuthCookie";

    // ����ʱ SameSite ���� None������ Cookie �������ȥ
    options.Cookie.SameSite = SameSiteMode.None;

    options.ExpireTimeSpan = TimeSpan.FromHours(1);

    options.SlidingExpiration = true;

    options.LoginPath = "/api/account/login";
    options.LogoutPath = "/api/account/logout";

    options.Events.OnRedirectToLogin = context =>
    {
        // ����� API �����򷵻� 401 �������ض���
        if (context.Request.Path.StartsWithSegments("/api") && context.Response.StatusCode == 200)
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        // �����������Ĭ���ض���
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// ���� CORS������ React ǰ��Я�� Cookie
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // ����Я�� Cookie
    });
});


var app = builder.Build();

// ������������ Swagger
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

    //1. ��Ʒ���� Seeder
    var context = services.GetRequiredService<LovelyFishContext>();
    context.Database.EnsureCreated();
    Console.WriteLine("[Seed] ��Ʒ���ӷ�����ʼִ��");
    DataSeeder.Seed(services);

    //2. ����Ա�˺� Seeder
    Console.WriteLine("[Seed] IdentitySeeder ��ʼִ��");
    await IdentitySeeder.SeedAdminAsync(services);  // ����IdentitySeeder
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
    //            Category = "����",
    //            Features = "Just test"
    //        };

    //        context.Products.Add(product);
    //        context.SaveChanges();
    //        Console.WriteLine("[Test] �ɹ�������Բ�Ʒ��");
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine("[Test] ����ʧ�ܣ�" + ex.Message);
    //        if (ex.InnerException != null)
    //            Console.WriteLine("Inner: " + ex.InnerException.Message);
    //    }


app.Run();

