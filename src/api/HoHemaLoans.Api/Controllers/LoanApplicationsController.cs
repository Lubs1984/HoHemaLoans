using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;
using System.Security.Claims;

namespace HoHemaLoans.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LoanApplicationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LoanApplicationsController> _logger;

    public LoanApplicationsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<LoanApplicationsController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoanApplication>>> GetLoanApplications()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var applications = await _context.LoanApplications
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.ApplicationDate)
            .ToListAsync();

        return Ok(applications);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LoanApplication>> GetLoanApplication(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var application = await _context.LoanApplications
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

        if (application == null)
            return NotFound();

        return Ok(application);
    }

    /// <summary>
    /// Create a draft loan application (multi-step wizard)
    /// </summary>
    [HttpPost("draft")]
    public async Task<ActionResult<LoanApplication>> CreateDraftApplication(CreateDraftApplicationDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var application = new LoanApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = LoanStatus.Draft,
            ApplicationDate = DateTime.UtcNow,
            ChannelOrigin = Enum.TryParse<LoanApplicationChannel>(dto.ChannelOrigin, out var channel) 
                ? channel 
                : LoanApplicationChannel.Web,
            CurrentStep = 0,
            StepData = new Dictionary<string, object>(),
            WebInitiatedDate = DateTime.UtcNow
        };

        _context.LoanApplications.Add(application);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLoanApplication), new { id = application.Id }, application);
    }

    /// <summary>
    /// Update a specific step in the application wizard
    /// </summary>
    [HttpPut("{id}/step/{stepNumber}")]
    public async Task<ActionResult<LoanApplication>> UpdateApplicationStep(Guid id, int stepNumber, [FromBody] UpdateStepDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var application = await _context.LoanApplications
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

        if (application == null)
            return NotFound("Application not found");

        if (application.Status != LoanStatus.Draft)
            return BadRequest("Only draft applications can be updated");

        // Update step data
        application.CurrentStep = stepNumber;
        
        // Parse step data JSON string to Dictionary
        try
        {
            if (!string.IsNullOrEmpty(dto.StepData))
            {
                application.StepData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(dto.StepData);
            }
        }
        catch
        {
            // If parsing fails, keep existing StepData
        }

        // Update specific fields based on step number
        switch (stepNumber)
        {
            case 0: // Loan Amount
                if (dto.Amount.HasValue)
                    application.Amount = dto.Amount.Value;
                break;
            case 1: // Term Months
                if (dto.TermMonths.HasValue)
                    application.TermMonths = dto.TermMonths.Value;
                break;
            case 2: // Purpose
                if (!string.IsNullOrEmpty(dto.Purpose))
                    application.Purpose = dto.Purpose;
                break;
            case 3: // Affordability Review
                application.IsAffordabilityIncluded = true;
                break;
            case 4: // Preview Terms - Calculate final amounts
                if (application.Amount > 0 && application.TermMonths > 0)
                {
                    application.InterestRate = CalculateInterestRate(application.Amount, application.TermMonths);
                    application.MonthlyPayment = CalculateMonthlyPayment(application.Amount, application.InterestRate, application.TermMonths);
                    application.TotalAmount = application.MonthlyPayment * application.TermMonths;
                }
                break;
            case 5: // Bank Details
                if (!string.IsNullOrEmpty(dto.BankName))
                    application.BankName = dto.BankName;
                if (!string.IsNullOrEmpty(dto.AccountNumber))
                    application.AccountNumber = dto.AccountNumber;
                if (!string.IsNullOrEmpty(dto.AccountHolderName))
                    application.AccountHolderName = dto.AccountHolderName;
                break;
        }

        _context.LoanApplications.Update(application);
        await _context.SaveChangesAsync();

        return Ok(application);
    }

    /// <summary>
    /// Submit a loan application (final step with OTP verification)
    /// </summary>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult<LoanApplication>> SubmitApplication(Guid id, [FromBody] SubmitApplicationDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var application = await _context.LoanApplications
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

        if (application == null)
            return NotFound("Application not found");

        if (application.Status != LoanStatus.Draft)
            return BadRequest("Only draft applications can be submitted");

        // Validate required fields
        if (application.Amount <= 0)
            return BadRequest("Loan amount is required");
        if (application.TermMonths <= 0)
            return BadRequest("Loan term is required");
        if (string.IsNullOrEmpty(application.Purpose))
            return BadRequest("Loan purpose is required");
        if (string.IsNullOrEmpty(application.BankName) || string.IsNullOrEmpty(application.AccountNumber))
            return BadRequest("Bank details are required");

        // TODO: Validate OTP (dto.Otp) - For now, we'll skip this
        // In production, you would verify the OTP against a stored value

        // Update status to Pending for admin review
        application.Status = LoanStatus.Pending;
        application.ApplicationDate = DateTime.UtcNow;
        application.CurrentStep = 7; // Completed all steps

        // Calculate final amounts if not already done
        if (application.InterestRate == 0)
        {
            application.InterestRate = CalculateInterestRate(application.Amount, application.TermMonths);
            application.MonthlyPayment = CalculateMonthlyPayment(application.Amount, application.InterestRate, application.TermMonths);
            application.TotalAmount = application.MonthlyPayment * application.TermMonths;
        }

        _context.LoanApplications.Update(application);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Loan application {id} submitted by user {userId}");

        return Ok(application);
    }

    [HttpPost]
    public async Task<ActionResult<LoanApplication>> CreateLoanApplication(CreateLoanApplicationDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        // Calculate loan terms
        var interestRate = CalculateInterestRate(dto.Amount, dto.TermMonths);
        var monthlyPayment = CalculateMonthlyPayment(dto.Amount, interestRate, dto.TermMonths);
        var totalAmount = monthlyPayment * dto.TermMonths;

        var application = new LoanApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = dto.Amount,
            TermMonths = dto.TermMonths,
            Purpose = dto.Purpose,
            Status = LoanStatus.Pending,
            InterestRate = interestRate,
            MonthlyPayment = monthlyPayment,
            TotalAmount = totalAmount,
            ApplicationDate = DateTime.UtcNow,
            ChannelOrigin = Enum.TryParse<LoanApplicationChannel>(dto.ChannelOrigin, out var channel) 
                ? channel 
                : LoanApplicationChannel.Web,
            CurrentStep = dto.CurrentStep,
            WebInitiatedDate = DateTime.UtcNow
        };

        _context.LoanApplications.Add(application);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLoanApplication), new { id = application.Id }, application);
    }

    private decimal CalculateInterestRate(decimal amount, int termMonths)
    {
        // Simple interest rate calculation based on amount and term
        var baseRate = 0.12m; // 12% base rate
        
        if (amount > 100000) baseRate -= 0.01m; // Lower rate for higher amounts
        if (termMonths > 24) baseRate += 0.005m; // Slightly higher for longer terms
        
        return Math.Max(0.08m, Math.Min(0.18m, baseRate)); // Between 8% and 18%
    }

    private decimal CalculateMonthlyPayment(decimal principal, decimal annualRate, int months)
    {
        var monthlyRate = annualRate / 12;
        if (monthlyRate == 0) return principal / months;
        
        var payment = principal * (monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), months)) /
                     ((decimal)Math.Pow((double)(1 + monthlyRate), months) - 1);
        
        return Math.Round(payment, 2);
    }
}

public class CreateDraftApplicationDto
{
    public string ChannelOrigin { get; set; } = "Web";
}

public class UpdateStepDto
{
    public string StepData { get; set; } = "{}";
    public decimal? Amount { get; set; }
    public int? TermMonths { get; set; }
    public string? Purpose { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountHolderName { get; set; }
}

public class SubmitApplicationDto
{
    public string? Otp { get; set; }
}