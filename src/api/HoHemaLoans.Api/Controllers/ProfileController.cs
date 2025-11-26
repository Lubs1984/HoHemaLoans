using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;
using HoHemaLoans.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HoHemaLoans.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAffordabilityService _affordabilityService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        ApplicationDbContext context,
        IAffordabilityService affordabilityService,
        ILogger<ProfileController> logger)
    {
        _context = context;
        _affordabilityService = affordabilityService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's ID from JWT token
    /// </summary>
    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    #region Income Endpoints

    /// <summary>
    /// Get all income records for current user
    /// </summary>
    [HttpGet("income")]
    public async Task<ActionResult<IEnumerable<IncomeDto>>> GetIncomes()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var incomes = await _context.Incomes
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new IncomeDto
                {
                    Id = i.Id,
                    SourceType = i.SourceType,
                    Description = i.Description,
                    MonthlyAmount = i.MonthlyAmount,
                    Frequency = i.Frequency,
                    Notes = i.Notes,
                    IsVerified = i.IsVerified,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt
                })
                .ToListAsync();

            return Ok(incomes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting incomes");
            return StatusCode(500, new { message = "Error retrieving income records" });
        }
    }

    /// <summary>
    /// Add a new income source
    /// </summary>
    [HttpPost("income")]
    public async Task<ActionResult<IncomeDto>> AddIncome([FromBody] CreateIncomeDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.SourceType) || string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest("Source type and description are required");

            if (dto.MonthlyAmount <= 0)
                return BadRequest("Monthly amount must be greater than zero");

            var income = new Income
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SourceType = dto.SourceType,
                Description = dto.Description,
                MonthlyAmount = dto.MonthlyAmount,
                Frequency = dto.Frequency ?? "Monthly",
                Notes = dto.Notes,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Incomes.Add(income);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Income added for user {userId}: {dto.SourceType}");

            return CreatedAtAction(nameof(GetIncomes), new IncomeDto
            {
                Id = income.Id,
                SourceType = income.SourceType,
                Description = income.Description,
                MonthlyAmount = income.MonthlyAmount,
                Frequency = income.Frequency,
                Notes = income.Notes,
                IsVerified = income.IsVerified,
                CreatedAt = income.CreatedAt,
                UpdatedAt = income.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding income");
            return StatusCode(500, new { message = "Error adding income record" });
        }
    }

    /// <summary>
    /// Update an income record
    /// </summary>
    [HttpPut("income/{id}")]
    public async Task<IActionResult> UpdateIncome(Guid id, [FromBody] UpdateIncomeDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var income = await _context.Incomes.FindAsync(id);
            if (income == null)
                return NotFound("Income record not found");

            if (income.UserId != userId)
                return Forbid();

            income.SourceType = dto.SourceType ?? income.SourceType;
            income.Description = dto.Description ?? income.Description;
            income.MonthlyAmount = dto.MonthlyAmount > 0 ? dto.MonthlyAmount : income.MonthlyAmount;
            income.Frequency = dto.Frequency ?? income.Frequency;
            income.Notes = dto.Notes;
            income.UpdatedAt = DateTime.UtcNow;

            _context.Incomes.Update(income);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Income updated for user {userId}: {id}");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating income");
            return StatusCode(500, new { message = "Error updating income record" });
        }
    }

    /// <summary>
    /// Delete an income record
    /// </summary>
    [HttpDelete("income/{id}")]
    public async Task<IActionResult> DeleteIncome(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var income = await _context.Incomes.FindAsync(id);
            if (income == null)
                return NotFound("Income record not found");

            if (income.UserId != userId)
                return Forbid();

            _context.Incomes.Remove(income);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Income deleted for user {userId}: {id}");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting income");
            return StatusCode(500, new { message = "Error deleting income record" });
        }
    }

    #endregion

    #region Expense Endpoints

    /// <summary>
    /// Get all expense records for current user
    /// </summary>
    [HttpGet("expense")]
    public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetExpenses()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var expenses = await _context.Expenses
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new ExpenseDto
                {
                    Id = e.Id,
                    Category = e.Category,
                    Description = e.Description,
                    MonthlyAmount = e.MonthlyAmount,
                    Frequency = e.Frequency,
                    Notes = e.Notes,
                    IsEssential = e.IsEssential,
                    IsFixed = e.IsFixed,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt
                })
                .ToListAsync();

            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses");
            return StatusCode(500, new { message = "Error retrieving expense records" });
        }
    }

    /// <summary>
    /// Add a new expense
    /// </summary>
    [HttpPost("expense")]
    public async Task<ActionResult<ExpenseDto>> AddExpense([FromBody] CreateExpenseDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Category) || string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest("Category and description are required");

            if (dto.MonthlyAmount <= 0)
                return BadRequest("Monthly amount must be greater than zero");

            var expense = new Expense
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Category = dto.Category,
                Description = dto.Description,
                MonthlyAmount = dto.MonthlyAmount,
                Frequency = dto.Frequency ?? "Monthly",
                Notes = dto.Notes,
                IsEssential = dto.IsEssential,
                IsFixed = dto.IsFixed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Expense added for user {userId}: {dto.Category}");

            return CreatedAtAction(nameof(GetExpenses), new ExpenseDto
            {
                Id = expense.Id,
                Category = expense.Category,
                Description = expense.Description,
                MonthlyAmount = expense.MonthlyAmount,
                Frequency = expense.Frequency,
                Notes = expense.Notes,
                IsEssential = expense.IsEssential,
                IsFixed = expense.IsFixed,
                CreatedAt = expense.CreatedAt,
                UpdatedAt = expense.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding expense");
            return StatusCode(500, new { message = "Error adding expense record" });
        }
    }

    /// <summary>
    /// Update an expense record
    /// </summary>
    [HttpPut("expense/{id}")]
    public async Task<IActionResult> UpdateExpense(Guid id, [FromBody] UpdateExpenseDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
                return NotFound("Expense record not found");

            if (expense.UserId != userId)
                return Forbid();

            expense.Category = dto.Category ?? expense.Category;
            expense.Description = dto.Description ?? expense.Description;
            expense.MonthlyAmount = dto.MonthlyAmount > 0 ? dto.MonthlyAmount : expense.MonthlyAmount;
            expense.Frequency = dto.Frequency ?? expense.Frequency;
            expense.Notes = dto.Notes;
            expense.IsEssential = dto.IsEssential;
            expense.IsFixed = dto.IsFixed;
            expense.UpdatedAt = DateTime.UtcNow;

            _context.Expenses.Update(expense);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Expense updated for user {userId}: {id}");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense");
            return StatusCode(500, new { message = "Error updating expense record" });
        }
    }

    /// <summary>
    /// Delete an expense record
    /// </summary>
    [HttpDelete("expense/{id}")]
    public async Task<IActionResult> DeleteExpense(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
                return NotFound("Expense record not found");

            if (expense.UserId != userId)
                return Forbid();

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Expense deleted for user {userId}: {id}");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense");
            return StatusCode(500, new { message = "Error deleting expense record" });
        }
    }

    #endregion

    #region Affordability Endpoints

    /// <summary>
    /// Calculate and get affordability assessment for current user
    /// </summary>
    [HttpGet("affordability")]
    public async Task<ActionResult<AffordabilityAssessmentDto>> GetAffordability()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var assessment = await _affordabilityService.CalculateAffordabilityAsync(userId);

            return Ok(new AffordabilityAssessmentDto
            {
                Id = assessment.Id,
                GrossMonthlyIncome = assessment.GrossMonthlyIncome,
                NetMonthlyIncome = assessment.NetMonthlyIncome,
                TotalMonthlyExpenses = assessment.TotalMonthlyExpenses,
                EssentialExpenses = assessment.EssentialExpenses,
                NonEssentialExpenses = assessment.NonEssentialExpenses,
                DebtToIncomeRatio = assessment.DebtToIncomeRatio,
                AvailableFunds = assessment.AvailableFunds,
                ExpenseToIncomeRatio = assessment.ExpenseToIncomeRatio,
                AffordabilityStatus = assessment.AffordabilityStatus,
                AssessmentNotes = assessment.AssessmentNotes,
                MaxRecommendedLoanAmount = assessment.MaxRecommendedLoanAmount,
                AssessmentDate = assessment.AssessmentDate,
                ExpiryDate = assessment.ExpiryDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating affordability");
            return StatusCode(500, new { message = "Error calculating affordability assessment" });
        }
    }

    /// <summary>
    /// Get maximum recommended loan amount based on affordability
    /// </summary>
    [HttpGet("affordability/max-loan")]
    public async Task<ActionResult<decimal>> GetMaxRecommendedLoanAmount()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var maxAmount = await _affordabilityService.GetMaxRecommendedLoanAmountAsync(userId);
            return Ok(new { maxRecommendedLoanAmount = maxAmount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting max loan amount");
            return StatusCode(500, new { message = "Error calculating maximum loan amount" });
        }
    }

    #endregion
}
