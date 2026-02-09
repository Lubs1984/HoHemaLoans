using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;
using HoHemaLoans.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace HoHemaLoans.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAffordabilityService _affordabilityService;
    private readonly ILogger<ProfileController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(
        ApplicationDbContext context,
        IAffordabilityService affordabilityService,
        ILogger<ProfileController> logger,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _affordabilityService = affordabilityService;
        _logger = logger;
        _userManager = userManager;
    }

    /// <summary>
    /// Get current user's ID from JWT token
    /// </summary>
    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    #region Profile Endpoints

    /// <summary>
    /// Get the current user's profile
    /// </summary>
    [HttpGet]
    [HttpGet("~/api/users/profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(MapToUserProfileDto(user, roles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user profile");
            return StatusCode(500, new { message = "Error fetching profile" });
        }
    }

    /// <summary>
    /// Update the current user's profile
    /// </summary>
    [HttpPut]
    [HttpPut("~/api/users/profile")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found");

            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
                return BadRequest("First name and last name are required");

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? user.PhoneNumber : dto.PhoneNumber.Trim();
            user.StreetAddress = dto.StreetAddress ?? string.Empty;
            user.Suburb = dto.Suburb ?? string.Empty;
            user.City = dto.City ?? string.Empty;
            user.Province = dto.Province ?? string.Empty;
            user.PostalCode = dto.PostalCode ?? string.Empty;
            user.EmployerName = dto.EmployerName ?? string.Empty;
            user.EmployeeNumber = dto.EmployeeNumber ?? string.Empty;
            user.PayrollReference = dto.PayrollReference ?? string.Empty;
            user.EmploymentType = dto.EmploymentType ?? string.Empty;
            user.BankName = dto.BankName ?? string.Empty;
            user.AccountType = dto.AccountType ?? string.Empty;
            user.AccountNumber = dto.AccountNumber ?? string.Empty;
            user.BranchCode = dto.BranchCode ?? string.Empty;
            user.NextOfKinName = dto.NextOfKinName ?? string.Empty;
            user.NextOfKinRelationship = dto.NextOfKinRelationship ?? string.Empty;
            user.NextOfKinPhone = dto.NextOfKinPhone ?? string.Empty;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                _logger.LogWarning("Profile update failed for user {UserId}: {Errors}", userId, string.Join(", ", errors));
                return BadRequest(new { message = "Failed to update profile", errors });
            }

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(MapToUserProfileDto(user, roles));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, new { message = "Error updating profile" });
        }
    }

    private static UserProfileDto MapToUserProfileDto(ApplicationUser user, IEnumerable<string> roles)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IdNumber = user.IdNumber,
            DateOfBirth = user.DateOfBirth,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            StreetAddress = user.StreetAddress,
            Suburb = user.Suburb,
            City = user.City,
            Province = user.Province,
            PostalCode = user.PostalCode,
            EmployerName = user.EmployerName,
            EmployeeNumber = user.EmployeeNumber,
            PayrollReference = user.PayrollReference,
            EmploymentType = user.EmploymentType,
            BankName = user.BankName,
            AccountType = user.AccountType,
            AccountNumber = user.AccountNumber,
            BranchCode = user.BranchCode,
            NextOfKinName = user.NextOfKinName,
            NextOfKinRelationship = user.NextOfKinRelationship,
            NextOfKinPhone = user.NextOfKinPhone,
            IsVerified = user.IsVerified,
            MonthlyIncome = user.MonthlyIncome,
            Roles = roles
        };
    }

    #endregion

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

public class UpdateProfileDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
    public string? StreetAddress { get; set; }
    public string? Suburb { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? EmployerName { get; set; }
    public string? EmployeeNumber { get; set; }
    public string? PayrollReference { get; set; }
    public string? EmploymentType { get; set; }
    public string? BankName { get; set; }
    public string? AccountType { get; set; }
    public string? AccountNumber { get; set; }
    public string? BranchCode { get; set; }
    public string? NextOfKinName { get; set; }
    public string? NextOfKinRelationship { get; set; }
    public string? NextOfKinPhone { get; set; }
}

public class UserProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public decimal MonthlyIncome { get; set; }
    public bool IsVerified { get; set; }
    public IEnumerable<string>? Roles { get; set; }

    // Address
    public string StreetAddress { get; set; } = string.Empty;
    public string Suburb { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;

    // Employment
    public string EmployerName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string PayrollReference { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;

    // Banking
    public string BankName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;

    // Next of kin
    public string NextOfKinName { get; set; } = string.Empty;
    public string NextOfKinRelationship { get; set; } = string.Empty;
    public string NextOfKinPhone { get; set; } = string.Empty;
}
