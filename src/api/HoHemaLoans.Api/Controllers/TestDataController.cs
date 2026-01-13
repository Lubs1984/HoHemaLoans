using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HoHemaLoans.Api.Data;

namespace HoHemaLoans.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestDataController : ControllerBase
{
    private readonly TestDataSeeder _seeder;
    private readonly ILogger<TestDataController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public TestDataController(
        TestDataSeeder seeder,
        ILogger<TestDataController> logger,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _seeder = seeder;
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    /// <summary>
    /// Seeds test data with 4 different user scenarios
    /// Only available in Development/Non-Production environments
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> SeedTestData()
    {
        // Allow in development or when IsDevelopment is explicitly set
        var isDevelopment = _environment.IsDevelopment() || 
                           _configuration.GetValue<bool>("IsDevelopment", false) ||
                           _environment.EnvironmentName == "Development";
        
        if (!isDevelopment)
        {
            return BadRequest(new { message = "Test data seeding is only available in development environment" });
        }

        try
        {
            _logger.LogInformation("Test data seeding requested");
            await _seeder.SeedTestDataAsync();
            
            return Ok(new
            {
                message = "Test data seeded successfully",
                users = new[]
                {
                    new
                    {
                        email = "test.affluent@hohema.com",
                        password = "Test@123",
                        scenario = "High Earner - Excellent Affordability",
                        expectedStatus = "Affordable"
                    },
                    new
                    {
                        email = "test.worker@hohema.com",
                        password = "Test@123",
                        scenario = "Middle Income Worker - Good Affordability",
                        expectedStatus = "LimitedAffordability"
                    },
                    new
                    {
                        email = "test.struggling@hohema.com",
                        password = "Test@123",
                        scenario = "Struggling Worker - Limited Affordability",
                        expectedStatus = "NotAffordable"
                    },
                    new
                    {
                        email = "test.newworker@hohema.com",
                        password = "Test@123",
                        scenario = "Junior Developer - Moderate Affordability",
                        expectedStatus = "LimitedAffordability"
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed test data");
            return StatusCode(500, new { message = "Failed to seed test data", error = ex.Message });
        }
    }

    /// <summary>
    /// Get test scenarios information without seeding
    /// </summary>
    [HttpGet("scenarios")]
    [AllowAnonymous]
    public IActionResult GetTestScenarios()
    {
        return Ok(new
        {
            scenarios = new[]
            {
                new
                {
                    name = "Alice Affluent",
                    email = "test.affluent@hohema.com",
                    monthlyIncome = 8500m,
                    monthlyExpenses = 4300m,
                    existingDebt = 0m,
                    expectedAffordability = "Affordable",
                    debtToIncomeRatio = "~51%",
                    notes = "High disposable income, no existing debt, excellent affordability"
                },
                new
                {
                    name = "Bob Builder",
                    email = "test.worker@hohema.com",
                    monthlyIncome = 4680m,
                    monthlyExpenses = 4550m,
                    existingDebt = 0m,
                    expectedAffordability = "LimitedAffordability",
                    debtToIncomeRatio = "~97%",
                    notes = "Limited disposable income after essential expenses"
                },
                new
                {
                    name = "Charlie Challenged",
                    email = "test.struggling@hohema.com",
                    monthlyIncome = 3200m,
                    monthlyExpenses = 4550m,
                    existingDebt = 800m,
                    expectedAffordability = "NotAffordable",
                    debtToIncomeRatio = "~142% (25% from existing debt)",
                    notes = "Expenses exceed income, existing debt burden"
                },
                new
                {
                    name = "Diana Developer",
                    email = "test.newworker@hohema.com",
                    monthlyIncome = 6300m,
                    monthlyExpenses = 6450m,
                    existingDebt = 500m,
                    expectedAffordability = "LimitedAffordability",
                    debtToIncomeRatio = "~102% (8% from existing debt)",
                    notes = "Moderate income with student debt, expenses slightly exceed income"
                }
            }
        });
    }
}
