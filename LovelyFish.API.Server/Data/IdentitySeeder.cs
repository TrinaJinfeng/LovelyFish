using LovelyFish.API.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LovelyFish.API.Data
{
    public static class IdentitySeeder
    {
        // Seed initial admin user and role
        public static async Task SeedAdminAsync(IServiceProvider services, IOptions<EmailSettings> emailSettings)
        {
            using var scope = services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string adminEmail = emailSettings.Value.AdminEmail;
            string adminName = emailSettings.Value.AdminName;
            string adminPassword = "Admin123!";

            // 1️⃣ Ensure "Admin" role exists
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
                Console.WriteLine("[IdentitySeeder] Admin role created");
            }

            // 2️⃣ Ensure admin user exists
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Name = adminName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    // Assign Admin role to user
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine($"[IdentitySeeder] Admin user created: {adminEmail}");
                }
                else
                {
                    Console.WriteLine("[IdentitySeeder] Failed to create admin: " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                Console.WriteLine("[IdentitySeeder] Admin user already exists");
            }
        }
    }
}
