using Microsoft.AspNetCore.Identity;
using HoHemaLoans.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HoHemaLoans.Api.Data;

/// <summary>
/// Seeds test data for 4 different user scenarios to test affordability assessment
/// </summary>
public class TestDataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TestDataSeeder> _logger;

    public TestDataSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<TestDataSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedTestDataAsync()
    {
        _logger.LogInformation("Starting test data seeding...");

        // Scenario 1: High Earner - Excellent Affordability
        await CreateTestUser(
            email: "test.affluent@hohema.com",
            firstName: "Alice",
            lastName: "Affluent",
            scenario: "High earner with minimal debt and expenses",
            incomes: new[]
            {
                new { Amount = 8500m, Category = "Employment", Description = "Senior Developer Salary", Frequency = "monthly", IsEssential = true }
            },
            expenses: new[]
            {
                new { Amount = 1200m, Category = "Housing", Description = "Rent", Frequency = "monthly", IsEssential = true },
                new { Amount = 500m, Category = "Utilities", Description = "Water, Electricity, Internet", Frequency = "monthly", IsEssential = true },
                new { Amount = 800m, Category = "Food", Description = "Groceries", Frequency = "monthly", IsEssential = true },
                new { Amount = 600m, Category = "Transport", Description = "Car payment & fuel", Frequency = "monthly", IsEssential = true },
                new { Amount = 300m, Category = "Communication", Description = "Cell phone", Frequency = "monthly", IsEssential = false },
                new { Amount = 400m, Category = "Insurance", Description = "Medical aid", Frequency = "monthly", IsEssential = true },
                new { Amount = 500m, Category = "Personal", Description = "Entertainment & hobbies", Frequency = "monthly", IsEssential = false }
            },
            expectedAffordability: "Affordable",
            notes: "Low debt-to-income ratio (~47%), high disposable income"
        );

        // Scenario 2: Middle Income Worker - Good Affordability
        await CreateTestUser(
            email: "test.worker@hohema.com",
            firstName: "Bob",
            lastName: "Builder",
            scenario: "Construction worker with average income and expenses",
            incomes: new[]
            {
                new { Amount = 180m, Category = "Employment", Description = "Construction hourly wage (R90/hr × 160hrs)", Frequency = "monthly", IsEssential = true },
                new { Amount = 500m, Category = "Other", Description = "Weekend side jobs", Frequency = "monthly", IsEssential = false }
            },
            expenses: new[]
            {
                new { Amount = 1800m, Category = "Housing", Description = "Rent", Frequency = "monthly", IsEssential = true },
                new { Amount = 400m, Category = "Utilities", Description = "Basic utilities", Frequency = "monthly", IsEssential = true },
                new { Amount = 1200m, Category = "Food", Description = "Groceries for family", Frequency = "monthly", IsEssential = true },
                new { Amount = 500m, Category = "Transport", Description = "Taxi fare", Frequency = "monthly", IsEssential = true },
                new { Amount = 200m, Category = "Communication", Description = "Cell phone", Frequency = "monthly", IsEssential = false },
                new { Amount = 300m, Category = "Dependents", Description = "School fees", Frequency = "monthly", IsEssential = true },
                new { Amount = 150m, Category = "Medical", Description = "Clinic visits", Frequency = "monthly", IsEssential = true }
            },
            expectedAffordability: "LimitedAffordability",
            notes: "Moderate debt-to-income ratio (~78%), limited disposable income"
        );

        // Scenario 3: Struggling Worker - Limited Affordability
        await CreateTestUser(
            email: "test.struggling@hohema.com",
            firstName: "Charlie",
            lastName: "Challenged",
            scenario: "Low-wage worker with high existing debt",
            incomes: new[]
            {
                new { Amount = 3200m, Category = "Employment", Description = "Retail salary", Frequency = "monthly", IsEssential = true }
            },
            expenses: new[]
            {
                new { Amount = 1400m, Category = "Housing", Description = "Rent", Frequency = "monthly", IsEssential = true },
                new { Amount = 500m, Category = "Utilities", Description = "Basic utilities", Frequency = "monthly", IsEssential = true },
                new { Amount = 1000m, Category = "Food", Description = "Groceries", Frequency = "monthly", IsEssential = true },
                new { Amount = 400m, Category = "Transport", Description = "Taxi fare", Frequency = "monthly", IsEssential = true },
                new { Amount = 800m, Category = "Debt", Description = "Existing loan repayment", Frequency = "monthly", IsEssential = true },
                new { Amount = 150m, Category = "Communication", Description = "Cell phone", Frequency = "monthly", IsEssential = false },
                new { Amount = 200m, Category = "Dependents", Description = "Child support", Frequency = "monthly", IsEssential = true },
                new { Amount = 100m, Category = "Medical", Description = "Medication", Frequency = "monthly", IsEssential = true }
            },
            expectedAffordability: "NotAffordable",
            notes: "High debt-to-income ratio (~33% from debt alone), minimal disposable income"
        );

        // Scenario 4: New Worker - Moderate Affordability
        await CreateTestUser(
            email: "test.newworker@hohema.com",
            firstName: "Diana",
            lastName: "Developer",
            scenario: "Junior developer with some student debt",
            incomes: new[]
            {
                new { Amount = 5500m, Category = "Employment", Description = "Junior Developer Salary", Frequency = "monthly", IsEssential = true },
                new { Amount = 800m, Category = "SelfEmployment", Description = "Freelance projects", Frequency = "monthly", IsEssential = false }
            },
            expenses: new[]
            {
                new { Amount = 2500m, Category = "Housing", Description = "Apartment rent", Frequency = "monthly", IsEssential = true },
                new { Amount = 600m, Category = "Utilities", Description = "All utilities", Frequency = "monthly", IsEssential = true },
                new { Amount = 1200m, Category = "Food", Description = "Groceries & eating out", Frequency = "monthly", IsEssential = true },
                new { Amount = 700m, Category = "Transport", Description = "Car payment", Frequency = "monthly", IsEssential = true },
                new { Amount = 500m, Category = "Debt", Description = "Student loan", Frequency = "monthly", IsEssential = true },
                new { Amount = 250m, Category = "Communication", Description = "Phone & internet", Frequency = "monthly", IsEssential = false },
                new { Amount = 400m, Category = "Insurance", Description = "Car & medical", Frequency = "monthly", IsEssential = true },
                new { Amount = 300m, Category = "Personal", Description = "Entertainment", Frequency = "monthly", IsEssential = false }
            },
            expectedAffordability: "LimitedAffordability",
            notes: "Moderate debt-to-income ratio (~19% from debt), decent disposable income"
        );

        _logger.LogInformation("Test data seeding completed successfully!");
    }

    private async Task CreateTestUser(
        string email,
        string firstName,
        string lastName,
        string scenario,
        dynamic[] incomes,
        dynamic[] expenses,
        string expectedAffordability,
        string notes)
    {
        _logger.LogInformation("Creating test user: {Email}", email);

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            _logger.LogInformation("User {Email} already exists, skipping...", email);
            return;
        }

        // Create user
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            IdNumber = GenerateIdNumber(),
            PhoneNumber = GeneratePhoneNumber(),
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, "Test@123");
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to create user {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        _logger.LogInformation("User {Email} created successfully", email);

        // Add incomes
        foreach (var income in incomes)
        {
            var incomeEntity = new Income
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                SourceType = income.Category,
                Description = income.Description,
                MonthlyAmount = income.Amount,
                Frequency = income.Frequency,
                Notes = $"Test data - {scenario}",
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Incomes.Add(incomeEntity);
        }

        // Add expenses
        foreach (var expense in expenses)
        {
            var expenseEntity = new Expense
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Category = expense.Category,
                Description = expense.Description,
                MonthlyAmount = expense.Amount,
                Frequency = expense.Frequency,
                IsEssential = expense.IsEssential,
                Notes = $"Test data - {scenario}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Expenses.Add(expenseEntity);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Added income and expense data for {Email}", email);
        _logger.LogInformation("Scenario: {Scenario}", scenario);
        _logger.LogInformation("Expected Affordability: {Expected}", expectedAffordability);
        _logger.LogInformation("Notes: {Notes}", notes);
    }

    private static string GenerateIdNumber()
    {
        var random = new Random();
        return $"{random.Next(900000, 999999)}{random.Next(5000, 9999)}08{random.Next(0, 2)}";
    }

    private static string GeneratePhoneNumber()
    {
        var random = new Random();
        return $"+27{random.Next(60, 89)}{random.Next(1000000, 9999999)}";
    }
}
