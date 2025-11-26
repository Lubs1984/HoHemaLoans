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
            ApplicationDate = DateTime.UtcNow
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