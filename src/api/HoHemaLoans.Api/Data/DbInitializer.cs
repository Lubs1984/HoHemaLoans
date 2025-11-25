using HoHemaLoans.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HoHemaLoans.Api.Data
{
    /// <summary>
    /// Database initializer to seed test data and users
    /// </summary>
    public static class DbInitializer
    {
        /// <summary>
        /// Initialize the database with seed data and test users
        /// </summary>
        public static async Task InitializeAsync(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                // Create database if it doesn't exist
                await context.Database.EnsureCreatedAsync();

                // Seed test users if they don't exist
                await SeedTestUsersAsync(userManager);
            }
        }

        private static async Task SeedTestUsersAsync(UserManager<ApplicationUser> userManager)
        {
            // Test user 1: Employee
            var testUser1 = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "john.doe@example.com",
                Email = "john.doe@example.com",
                EmailConfirmed = true,
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "+27812345678",
                IdNumber = "9001010001234",
                DateOfBirth = new DateTime(1990, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                Address = "123 Main Street, Cape Town, 8000",
                MonthlyIncome = 25000,
                IsVerified = true
            };

            // Test user 2: Casual Employee
            var testUser2 = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "jane.smith@example.com",
                Email = "jane.smith@example.com",
                EmailConfirmed = true,
                FirstName = "Jane",
                LastName = "Smith",
                PhoneNumber = "+27821234567",
                IdNumber = "8512055678901",
                DateOfBirth = new DateTime(1985, 12, 5, 0, 0, 0, DateTimeKind.Utc),
                Address = "456 Oak Avenue, Johannesburg, 2000",
                MonthlyIncome = 18500,
                IsVerified = true
            };

            // Test user 3: Demo User
            var testUser3 = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "demo@example.com",
                Email = "demo@example.com",
                EmailConfirmed = true,
                FirstName = "Demo",
                LastName = "User",
                PhoneNumber = "+27831234567",
                IdNumber = "7603015678901",
                DateOfBirth = new DateTime(1976, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                Address = "789 Test Lane, Durban, 4000",
                MonthlyIncome = 22000,
                IsVerified = true
            };

            // Test user 4: Quick Test User
            var testUser4 = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "test@example.com",
                Email = "test@example.com",
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "Account",
                PhoneNumber = "+27809876543",
                IdNumber = "9504156789012",
                DateOfBirth = new DateTime(1995, 4, 15, 0, 0, 0, DateTimeKind.Utc),
                Address = "321 Demo Road, Pretoria, 0001",
                MonthlyIncome = 20000,
                IsVerified = true
            };

            // Create users with password
            var testUsers = new[]
            {
                (testUser1, "TestPassword123!"),
                (testUser2, "TestPassword123!"),
                (testUser3, "TestPassword123!"),
                (testUser4, "TestPassword123!")
            };

            foreach (var (user, password) in testUsers)
            {
                // Check if user already exists
                if (!string.IsNullOrEmpty(user.Email))
                {
                    var existingUser = await userManager.FindByEmailAsync(user.Email);
                    if (existingUser == null)
                    {
                        var result = await userManager.CreateAsync(user, password);
                        if (result.Succeeded)
                        {
                            Console.WriteLine($"✅ Created test user: {user.Email}");
                        }
                        else
                        {
                            Console.WriteLine($"❌ Failed to create test user {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"⏭️  Test user already exists: {user.Email}");
                    }
                }
            }
        }
    }
}
