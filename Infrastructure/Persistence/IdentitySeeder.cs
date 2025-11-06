using Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            string[] roles = { "Admin", "User" };

            // 1. Ensure roles exist (create them if not)
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new Role
                    {
                      Name=roleName
                    };

                    var result = await roleManager.CreateAsync(role);
                    if (!result.Succeeded)
                    {
                        Console.WriteLine($" Failed to create role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                    else
                    {
                        Console.WriteLine($"Role {roleName} created.");
                    }
                }
            }

            // ✅ 2. Create a default admin user if none exists
            var adminEmail = "admin@gacwms.local";
            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                var newAdmin = new User
                {
                    Id = Guid.NewGuid(),
                    UserId="admin001",
                    FullName = "System Admin",
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var userResult = await userManager.CreateAsync(newAdmin, "Admin@12345");
                if (userResult.Succeeded)
                {
                    Console.WriteLine("Admin user created.");

                    // ✅ 3. Ensure role exists again before assigning
                    if (!await roleManager.RoleExistsAsync("Admin"))
                    {
                        Console.WriteLine("Admin role not found — creating again.");
                        await roleManager.CreateAsync(new Role
                        {
                            Name = "Admin",
                            NormalizedName = "ADMIN"
                        });
                    }

                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                    Console.WriteLine("Admin user assigned to 'Admin' role.");
                }
                else
                {
                    Console.WriteLine($"Failed to create admin user: {string.Join(", ", userResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                Console.WriteLine("Admin user already exists.");
            }
        }

    }
}
