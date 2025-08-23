using LovelyFish.API.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace LovelyFish.API.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string adminEmail = "admin@example.com";
            string adminPassword = "Admin123!";

            // 1️⃣ 确保 Admin 角色存在
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
                Console.WriteLine("[IdentitySeeder] 已创建角色 Admin");
            }

            // 2️⃣ 确保 Admin 用户存在
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("[IdentitySeeder] 已创建管理员账号 admin@example.com");
                }
                else
                {
                    Console.WriteLine("[IdentitySeeder] 创建管理员失败: " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                Console.WriteLine("[IdentitySeeder] 管理员账号已存在");
            }
        }
    }
}
