using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;
using HoHemaLoans.Api.Services;
using System.ComponentModel.DataAnnotations;
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
    private readonly IOmnichannelLoanService _omnichannelService;
    private readonly IAffordabilityService _affordabilityService;

    public LoanApplicationsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<LoanApplicationsController> logger,
        IOmnichannelLoanService omnichannelService,
        IAffordabilityService affordabilityService)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _omnichannelService = omnichannelService;
        _affordabilityService = affordabilityService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoanApplication>>> GetLoanApplications()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var applications = await _omnichannelService.GetUserApplicationsAsync(userId);
        return Ok(applications);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LoanApplication>> GetLoanApplication(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var application = await _omnichannelService.GetApplicationAsync(id, userId);

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

        var channel = Enum.TryParse<LoanApplicationChannel>(dto.ChannelOrigin, out var ch) 
            ? ch 
            : LoanApplicationChannel.Web;

        var application = await _omnichannelService.CreateDraftApplicationAsync(
            userId, 
            channel, 
            phoneNumber: null);

        return CreatedAtAction(nameof(GetLoanApplication), new { id = application.Id }, application);
    }

    /// <summary>
    /// Update a specific step in the application wizard
    /// Syncs affordability and calculates loan terms automatically
    /// </summary>
    [HttpPut("{id}/step/{stepNumber}")]
    public async Task<ActionResult<LoanApplication>> UpdateApplicationStep(Guid id, int stepNumber, [FromBody] UpdateStepDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        try
        {
            // Build step data dictionary from DTO
            var stepData = new Dictionary<string, object>();
            
            if (dto.Amount.HasValue)
                stepData["amount"] = dto.Amount.Value;
            if (dto.TermMonths.HasValue)
                stepData["termMonths"] = dto.TermMonths.Value;
            if (!string.IsNullOrEmpty(dto.Purpose))
                stepData["purpose"] = dto.Purpose;
            if (!string.IsNullOrEmpty(dto.BankName))
                stepData["bankName"] = dto.BankName;
            if (!string.IsNullOrEmpty(dto.AccountNumber))
                stepData["accountNumber"] = dto.AccountNumber;
            if (!string.IsNullOrEmpty(dto.AccountHolderName))
                stepData["accountHolderName"] = dto.AccountHolderName;

            // Use omnichannel service for update (handles affordability sync)
            var application = await _omnichannelService.UpdateApplicationStepAsync(
                id, 
                userId, 
                stepNumber, 
                stepData);

            return Ok(application);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
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

        try
        {
            var application = await _omnichannelService.SubmitApplicationAsync(id, userId, dto.Otp);
            return Ok(application);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Resume an in-progress application from a different channel.
    /// Enables seamless switching between Web and WhatsApp.
    /// </summary>
    [HttpPost("resume")]
    public async Task<ActionResult<LoanApplication>> ResumeApplication([FromBody] ResumeApplicationDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var targetChannel = Enum.TryParse<LoanApplicationChannel>(dto.Channel, out var ch)
            ? ch
            : LoanApplicationChannel.Web;

        var application = await _omnichannelService.ResumeFromChannelAsync(
            userId, 
            targetChannel, 
            phoneNumber: null);

        if (application == null)
            return NotFound("No draft application found to resume");

        return Ok(application);
    }

    /// <summary>
    /// Create a new worker-based loan application
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<LoanApplication>> CreateWorkerLoanApplication(CreateWorkerLoanDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        // Validate repayment day range
        if (dto.RepaymentDay < 25 || dto.RepaymentDay > 31)
        {
            return BadRequest(new { message = "Repayment day must be between 25 and 31" });
        }

        try
        {
            // Check affordability before creating loan
            var affordabilityAssessment = await _affordabilityService.CalculateAffordabilityAsync(userId);
            var canAfford = await _affordabilityService.CanAffordLoanAsync(userId, dto.Amount, dto.TotalAmount);
            
            if (!canAfford)
            {
                _logger.LogWarning(
                    "[LOAN] User {UserId} failed affordability check for loan amount {Amount}, Status: {Status}",
                    userId, dto.Amount, affordabilityAssessment.AffordabilityStatus
                );
                
                // Still create the application but mark it for review
                // Admin can override affordability decision
            }

            // Calculate expected repayment date
            var today = DateTime.UtcNow;
            var repaymentDate = new DateTime(
                today.Month == 12 ? today.Year + 1 : today.Year,
                today.Month == 12 ? 1 : today.Month + 1,
                Math.Min(dto.RepaymentDay, DateTime.DaysInMonth(
                    today.Month == 12 ? today.Year + 1 : today.Year,
                    today.Month == 12 ? 1 : today.Month + 1))
            );

            var application = new LoanApplication
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = dto.Amount,
                TotalAmount = dto.TotalAmount,
                TermMonths = dto.TermMonths,
                Purpose = dto.Purpose,
                Status = LoanStatus.Pending,
                InterestRate = dto.AppliedInterestRate / 100m,
                MonthlyPayment = dto.TotalAmount, // Single payment
                ApplicationDate = DateTime.UtcNow,
                ChannelOrigin = LoanApplicationChannel.Web,
                WebInitiatedDate = DateTime.UtcNow,
                CurrentStep = 7, // Completed all steps
                
                // Worker earnings fields
                HoursWorked = dto.HoursWorked,
                HourlyRate = dto.HourlyRate,
                MonthlyEarnings = dto.MonthlyEarnings,
                MaxLoanAmount = dto.MaxLoanAmount,
                AppliedInterestRate = dto.AppliedInterestRate,
                AppliedAdminFee = dto.AppliedAdminFee,
                RepaymentDay = dto.RepaymentDay,
                ExpectedRepaymentDate = repaymentDate,
                HasIncomeExpenseChanged = dto.HasIncomeExpenseChanged,
                IsAffordabilityIncluded = true,
                
                // Affordability tracking
                AffordabilityStatus = affordabilityAssessment.AffordabilityStatus,
                PassedAffordabilityCheck = canAfford,
                AffordabilityNotes = affordabilityAssessment.AssessmentNotes
            };

            _context.LoanApplications.Add(application);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[LOAN] Worker loan application created: {ApplicationId} for user {UserId}, Amount: R{Amount}",
                application.Id, userId, application.Amount
            );

            return CreatedAtAction(nameof(GetLoanApplication), new { id = application.Id }, application);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LOAN] Error creating worker loan application for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to create loan application" });
        }
    }

    [HttpPost("legacy")]
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

public class CreateWorkerLoanDto
{
    [Required]
    [Range(0.1, 1000, ErrorMessage = "Hours worked must be between 0.1 and 1000")]
    public decimal HoursWorked { get; set; }
    
    [Required]
    [Range(1, 10000, ErrorMessage = "Hourly rate must be between R1 and R10,000")]
    public decimal HourlyRate { get; set; }
    
    [Required]
    public decimal MonthlyEarnings { get; set; }
    
    [Required]
    [Range(1, 1000000, ErrorMessage = "Loan amount must be between R1 and R1,000,000")]
    public decimal Amount { get; set; }
    
    public decimal MaxLoanAmount { get; set; }
    public decimal AppliedInterestRate { get; set; }
    public decimal AppliedAdminFee { get; set; }
    public decimal TotalAmount { get; set; }
    
    [Required]
    [Range(25, 31, ErrorMessage = "Repayment day must be between 25 and 31")]
    public int RepaymentDay { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Purpose must be between 3 and 100 characters")]
    public string Purpose { get; set; } = string.Empty;
    
    public bool HasIncomeExpenseChanged { get; set; }
    
    [Required]
    [Range(1, 1, ErrorMessage = "Worker loans are single-payment only (1 month term)")]
    public int TermMonths { get; set; }
}
