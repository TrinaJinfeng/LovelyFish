using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Data;
using System.Text.Json;
using LovelyFish.API.Server.Models;
using Microsoft.AspNetCore.Identity;
using LovelyFish.API.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container. ������

builder.Services.AddControllers();
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
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // �� HTTPS ����
    options.Cookie.Name = ".LovelyFish.AuthCookie";

    // ����ʱ SameSite ���� None������ Cookie �������ȥ
    options.Cookie.SameSite = SameSiteMode.None;

    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;

    options.LoginPath = "/api/account/login";
    options.LogoutPath = "/api/account/logout";
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
// ��̬�ļ���ǰ�� React �������
app.UseDefaultFiles();
app.UseStaticFiles();

// ·�� & CORS
app.UseRouting();
app.UseCors("AllowReactApp");


// ��֤�м����������
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

    // ȷ�����ݿ����
    context.Database.EnsureCreated();
    Console.WriteLine("[Seed] ���ӷ�����ʼִ��");
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
}

app.Run();


//��� Identity Cookie �����ã�ȷ����

//Cookie �� HttpOnly�����ܱ� JS ����

//ֻͨ�� HTTPS ����

//���� SameSite Ϊ Lax �� Strict ��ֹ CSRF

//�ʵ����ù���ʱ��
//�Ķ��ص�
//SameSite = None

//���ԭ���� Lax�����ڿ�������� Cookie ʱ�ᱻ�����ֱ�Ӿܾ���

//������Ժ�Ҫ�ñ����������վ����ͬ�������������� None + Secure = Always��

//AllowCredentials() ���� WithOrigins()

//WithOrigins("*") + AllowCredentials() �ǲ�����ģ��������ȫ���ƣ���

//������ȷд������������б�

//SecurePolicy = Always

//��֤ Cookie ֻ���� HTTPS �ϴ��䡣

//���ؿ���ʱ���û HTTPS��������ʱ�� CookieSecurePolicy.SameAsRequest��

//�������֤˳��

//�����ǣ�

//scss
//����
//�༭
//app.UseRouting();
//app.UseCors();
//app.UseAuthentication();
//app.UseAuthorization();
//����汾��Ч��
//��¼ʱ����˻�ͨ�� Identity ���� .LovelyFish.AuthCookie����������浽 Cookie��

//ˢ��ҳ��ʱ Cookie ���Զ����ϣ������Զ�����֤��

//�ǳ�ʱ HttpContext.SignOutAsync() ����� Cookie���û���ȫ�˳���

//�����Ǳ��ؿ������ǽ������ߵ��������������������վ���ܵ�¼/�ǳ�������