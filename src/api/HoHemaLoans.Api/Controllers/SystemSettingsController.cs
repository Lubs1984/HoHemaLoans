using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;

namespace HoHemaLoans.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SystemSettingsController> _logger;

    public SystemSettingsController(ApplicationDbContext context, ILogger<SystemSettingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get current system settings
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SystemSettings>> GetSettings()
    {
        try
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                // Create default settings if none exist
                settings = new SystemSettings
                {
                    InterestRatePercentage = 5.0m,
                    AdminFee = 50.0m,
                    MaxLoanPercentage = 20.0m,
                    MinLoanAmount = 100.0m,
                    MaxLoanAmount = 10000.0m,
                    LastModifiedDate = DateTime.UtcNow
                };
                
                _context.SystemSettings.Add(settings);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("[SETTINGS] Created default system settings");
            }
            
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SETTINGS] Error retrieving system settings");
            return StatusCode(500, new { message = "Error retrieving system settings" });
        }
    }

    /// <summary>
    /// Update system settings (Admin only)
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SystemSettings>> UpdateSettings([FromBody] SystemSettingsUpdateDto dto)
    {
        try
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                return NotFound(new { message = "System settings not found" });
            }

            // Validate input ranges
            if (dto.InterestRatePercentage < 0 || dto.InterestRatePercentage > 100)
            {
                return BadRequest(new { message = "Interest rate must be between 0 and 100" });
            }

            if (dto.AdminFee < 0)
            {
                return BadRequest(new { message = "Admin fee cannot be negative" });
            }

            if (dto.MaxLoanPercentage < 1 || dto.MaxLoanPercentage > 100)
            {
                return BadRequest(new { message = "Max loan percentage must be between 1 and 100" });
            }

            if (dto.MinLoanAmount < 0 || dto.MinLoanAmount >= dto.MaxLoanAmount)
            {
                return BadRequest(new { message = "Invalid loan amount range" });
            }

            // Update settings
            settings.InterestRatePercentage = dto.InterestRatePercentage;
            settings.AdminFee = dto.AdminFee;
            settings.MaxLoanPercentage = dto.MaxLoanPercentage;
            settings.MinLoanAmount = dto.MinLoanAmount;
            settings.MaxLoanAmount = dto.MaxLoanAmount;
            settings.LastModifiedDate = DateTime.UtcNow;
            settings.LastModifiedBy = User.Identity?.Name;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[SETTINGS] Updated by {User}: Interest={Interest}%, AdminFee={AdminFee}, MaxLoan={MaxLoan}%",
                User.Identity?.Name, 
                settings.InterestRatePercentage, 
                settings.AdminFee, 
                settings.MaxLoanPercentage
            );

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SETTINGS] Error updating system settings");
            return StatusCode(500, new { message = "Error updating system settings" });
        }
    }

    /// <summary>
    /// Initialize system settings (creates default if missing) - No auth required for setup
    /// </summary>
    [HttpPost("initialize")]
    [AllowAnonymous]
    public async Task<ActionResult<SystemSettings>> InitializeSettings()
    {
        try
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            
            if (settings != null)
            {
                return Ok(new { message = "Settings already initialized", settings });
            }

            // Create default settings
            settings = new SystemSettings
            {
                InterestRatePercentage = 5.0m,
                AdminFee = 50.0m,
                MaxLoanPercentage = 50.0m,  // 50% of monthly earnings
                MinLoanAmount = 100.0m,
                MaxLoanAmount = 10000.0m,
                LastModifiedDate = DateTime.UtcNow,
                LastModifiedBy = "system"
            };
            
            _context.SystemSettings.Add(settings);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("[SETTINGS] Initialized default system settings");
            
            return Ok(new { message = "Settings initialized successfully", settings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SETTINGS] Error initializing system settings: {Message}", ex.Message);
            return StatusCode(500, new { 
                message = "Error initializing system settings",
                error = ex.Message,
                details = ex.InnerException?.Message 
            });
        }
    }

    /// <summary>
    /// Calculate loan details based on earnings
    /// </summary>
    [HttpPost("calculate")]
    public async Task<ActionResult<LoanCalculationResult>> CalculateLoan([FromBody] LoanCalculationRequest request)
    {
        try
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                return NotFound(new { message = "System settings not configured" });
            }

            // Calculate monthly earnings
            var monthlyEarnings = request.HoursWorked * request.HourlyRate;

            // Calculate max loan amount
            var maxLoanAmount = monthlyEarnings * (settings.MaxLoanPercentage / 100);

            // Ensure requested amount is within limits
            var loanAmount = Math.Min(request.RequestedAmount, maxLoanAmount);
            loanAmount = Math.Max(loanAmount, settings.MinLoanAmount);
            loanAmount = Math.Min(loanAmount, settings.MaxLoanAmount);

            // Calculate interest and total
            var interestAmount = loanAmount * (settings.InterestRatePercentage / 100);
            var totalRepayment = loanAmount + interestAmount + settings.AdminFee;

            var result = new LoanCalculationResult
            {
                MonthlyEarnings = monthlyEarnings,
                MaxLoanAmount = maxLoanAmount,
                ApprovedLoanAmount = loanAmount,
                InterestRate = settings.InterestRatePercentage,
                InterestAmount = interestAmount,
                AdminFee = settings.AdminFee,
                TotalRepayment = totalRepayment,
                IsWithinLimits = request.RequestedAmount <= maxLoanAmount
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SETTINGS] Error calculating loan: {Message}", ex.Message);
            return StatusCode(500, new { 
                message = "Error calculating loan",
                error = ex.Message,
                details = ex.InnerException?.Message 
            });
        }
    }
}

public class SystemSettingsUpdateDto
{
    public decimal InterestRatePercentage { get; set; }
    public decimal AdminFee { get; set; }
    public decimal MaxLoanPercentage { get; set; }
    public decimal MinLoanAmount { get; set; }
    public decimal MaxLoanAmount { get; set; }
}

public class LoanCalculationRequest
{
    public decimal HoursWorked { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal RequestedAmount { get; set; }
}

public class LoanCalculationResult
{
    public decimal MonthlyEarnings { get; set; }
    public decimal MaxLoanAmount { get; set; }
    public decimal ApprovedLoanAmount { get; set; }
    public decimal InterestRate { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal AdminFee { get; set; }
    public decimal TotalRepayment { get; set; }
    public bool IsWithinLimits { get; set; }
}
