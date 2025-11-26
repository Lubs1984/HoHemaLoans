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
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // ============= DASHBOARD STATS =============

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("dashboard/stats")]
    public async Task<ActionResult<object>> GetDashboardStats()
    {
        var totalApplications = await _context.LoanApplications.CountAsync();
        var pendingApplications = await _context.LoanApplications
            .CountAsync(l => l.Status == LoanStatus.Pending);
        var approvedApplications = await _context.LoanApplications
            .CountAsync(l => l.Status == LoanStatus.Approved);
        var rejectedApplications = await _context.LoanApplications
            .CountAsync(l => l.Status == LoanStatus.Rejected);
        
        var totalUsers = await _userManager.Users.CountAsync();
        var verifiedUsers = await _userManager.Users
            .CountAsync(u => u.IsVerified);
        
        var totalContacts = await _context.WhatsAppContacts.CountAsync();
        var openConversations = 0; // Default to 0 if ConversationStatus is not available

        var stats = new
        {
            applications = new
            {
                total = totalApplications,
                pending = pendingApplications,
                approved = approvedApplications,
                rejected = rejectedApplications
            },
            users = new
            {
                total = totalUsers,
                verified = verifiedUsers
            },
            whatsapp = new
            {
                totalContacts,
                openConversations
            },
            timestamp = DateTime.UtcNow
        };

        return Ok(stats);
    }

    // ============= LOAN APPLICATIONS MANAGEMENT =============

    /// <summary>
    /// Get all loan applications with filtering and pagination
    /// </summary>
    [HttpGet("loans")]
    public async Task<ActionResult<object>> GetAllLoanApplications(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var query = _context.LoanApplications
            .Include(l => l.User)
            .AsQueryable();

        // Filter by status (convert string to enum)
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LoanStatus>(status, true, out var statusEnum))
        {
            query = query.Where(l => l.Status == statusEnum);
        }

        // Search by user name or email
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(l => 
                l.User.FirstName.Contains(search) ||
                l.User.LastName.Contains(search) ||
                l.User.Email.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var applications = await query
            .OrderByDescending(l => l.ApplicationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id,
                l.Amount,
                l.Status,
                l.InterestRate,
                l.TermMonths,
                l.ApplicationDate,
                l.ApprovalDate,
                l.Notes,
                User = new
                {
                    l.User.Id,
                    l.User.FirstName,
                    l.User.LastName,
                    l.User.Email,
                    l.User.PhoneNumber,
                    l.User.MonthlyIncome
                }
            })
            .ToListAsync();

        var pageCount = (int)Math.Ceiling((double)totalCount / pageSize);
        
        return Ok(new
        {
            totalCount,
            pageCount,
            currentPage = page,
            pageSize,
            data = applications
        });
    }

    /// <summary>
    /// Get a specific loan application
    /// </summary>
    [HttpGet("loans/{id}")]
    public async Task<ActionResult> GetLoanApplicationDetail(Guid id)
    {
        var loan = await _context.LoanApplications
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan == null)
            return NotFound("Loan application not found");

        var response = new
        {
            loan.Id,
            loan.Amount,
            loan.Status,
            loan.InterestRate,
            loan.TermMonths,
            loan.Purpose,
            loan.ApplicationDate,
            loan.ApprovalDate,
            loan.Notes,
            loan.MonthlyPayment,
            loan.TotalAmount,
            User = new
            {
                loan.User.Id,
                loan.User.FirstName,
                loan.User.LastName,
                loan.User.Email,
                loan.User.PhoneNumber,
                loan.User.MonthlyIncome,
                loan.User.IdNumber,
                loan.User.Address
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Approve a loan application
    /// </summary>
    [HttpPost("loans/{id}/approve")]
    public async Task<ActionResult> ApproveLoan(Guid id, [FromBody] ApproveLoanDto dto)
    {
        var loan = await _context.LoanApplications.FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null)
            return NotFound("Loan application not found");

        if (loan.Status != LoanStatus.Pending)
            return BadRequest("Only pending loans can be approved");

        // Update loan with approval details
        loan.Status = LoanStatus.Approved;
        loan.InterestRate = dto.InterestRate;
        loan.TermMonths = dto.RepaymentMonths;
        loan.ApprovalDate = DateTime.UtcNow;
        loan.Notes = dto.Notes;

        // Calculate monthly payment
        var monthlyRate = (double)loan.InterestRate / 100 / 12;
        var numPayments = loan.TermMonths;
        if (monthlyRate > 0)
        {
            loan.MonthlyPayment = (decimal)(loan.Amount * (decimal)monthlyRate * (decimal)Math.Pow(1 + monthlyRate, numPayments) 
                / (decimal)(Math.Pow(1 + monthlyRate, numPayments) - 1));
        }
        else
        {
            loan.MonthlyPayment = loan.Amount / numPayments;
        }

        loan.TotalAmount = loan.MonthlyPayment * numPayments;

        _context.LoanApplications.Update(loan);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Loan approved successfully", loan.Id });
    }

    /// <summary>
    /// Reject a loan application
    /// </summary>
    [HttpPost("loans/{id}/reject")]
    public async Task<ActionResult> RejectLoan(Guid id, [FromBody] RejectLoanDto dto)
    {
        var loan = await _context.LoanApplications.FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null)
            return NotFound("Loan application not found");

        if (loan.Status != LoanStatus.Pending)
            return BadRequest("Only pending loans can be rejected");

        loan.Status = LoanStatus.Rejected;
        loan.Notes = dto.Reason;
        loan.ApprovalDate = DateTime.UtcNow;

        _context.LoanApplications.Update(loan);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Loan rejected successfully", loan.Id });
    }

    // ============= WHATSAPP MANAGEMENT =============

    /// <summary>
    /// Get all WhatsApp conversations
    /// </summary>
    [HttpGet("whatsapp/conversations")]
    public async Task<ActionResult<object>> GetConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null)
    {
        var query = _context.WhatsAppConversations
            .Include(c => c.Contact)
            .Include(c => c.Messages)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => 
                c.Contact.DisplayName.Contains(search) ||
                c.Contact.PhoneNumber.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var conversations = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                Contact = new
                {
                    c.Contact.Id,
                    c.Contact.PhoneNumber,
                    c.Contact.DisplayName
                },
                Messages = c.Messages.OrderByDescending(m => m.CreatedAt).Take(10)
                    .Select(m => new { m.Id, m.MessageText, m.Direction, m.CreatedAt, m.Status })
                    .ToList(),
                c.UpdatedAt
            })
            .ToListAsync();

        var pageCount = (int)Math.Ceiling((double)totalCount / pageSize);

        return Ok(new
        {
            totalCount,
            pageCount,
            currentPage = page,
            pageSize,
            data = conversations
        });
    }

    /// <summary>
    /// Send a WhatsApp message
    /// </summary>
    [HttpPost("whatsapp/send-message")]
    public async Task<ActionResult> SendMessage([FromBody] SendWhatsAppMessageDto dto)
    {
        if (!int.TryParse(dto.ConversationId, out var conversationId))
            return BadRequest("Invalid conversation ID");

        var conversation = await _context.WhatsAppConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null)
            return NotFound("Conversation not found");

        // Determine message direction (outbound for admin sending)
        var direction = MessageDirection.Outbound;
        
        // Determine message status (sent)
        var status = MessageStatus.Sent;

        var message = new WhatsAppMessage
        {
            ConversationId = conversationId,
            ContactId = conversation.ContactId,
            MessageText = dto.Content,
            Direction = direction,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        _context.WhatsAppMessages.Add(message);
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Message sent", messageId = message.Id });
    }

    // ============= USER MANAGEMENT =============

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<object>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => 
                u.FirstName.Contains(search) ||
                u.LastName.Contains(search) ||
                u.Email.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.PhoneNumber,
                u.MonthlyIncome,
                u.IsVerified,
                u.CreatedAt
            })
            .ToListAsync();

        var pageCount = (int)Math.Ceiling((double)totalCount / pageSize);

        return Ok(new
        {
            totalCount,
            pageCount,
            currentPage = page,
            pageSize,
            data = users
        });
    }

    /// <summary>
    /// Get a specific user
    /// </summary>
    [HttpGet("users/{id}")]
    public async Task<ActionResult> GetUserDetail(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound("User not found");

        var roles = await _userManager.GetRolesAsync(user);
        var loans = await _context.LoanApplications
            .Where(l => l.UserId == id)
            .Select(l => new
            {
                l.Id,
                l.Amount,
                l.Status,
                l.ApplicationDate
            })
            .ToListAsync();

        var response = new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.MonthlyIncome,
            user.IsVerified,
            user.IdNumber,
            user.Address,
            user.CreatedAt,
            Roles = roles,
            LoanApplications = loans
        };

        return Ok(response);
    }
}

// ============= DTOs =============

public class ApproveLoanDto
{
    public decimal InterestRate { get; set; }
    public int RepaymentMonths { get; set; }
    public string? Notes { get; set; }
}

public class RejectLoanDto
{
    public string? Reason { get; set; }
}

public class SendWhatsAppMessageDto
{
    public string ConversationId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
