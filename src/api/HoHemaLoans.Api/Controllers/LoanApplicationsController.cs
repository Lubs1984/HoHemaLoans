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
    private readonly IOmnichannelLoanService _omnichannelService;

    public LoanApplicationsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<LoanApplicationsController> logger,
        IOmnichannelLoanService omnichannelService)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _omnichannelService = omnichannelService;
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

        var targetChannel = Enum.TryParse<LoanApplicationChannel>(dto.TargetChannel, out var ch)
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
public class ResumeApplicationDto
{
    public string TargetChannel { get; set; } = "Web";
}
