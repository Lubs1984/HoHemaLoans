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
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;
    private readonly IProfileVerificationService _verificationService;
    private readonly ContractService _contractService;
    private readonly IBulkUserImportService _bulkUserImportService;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger,
        IProfileVerificationService verificationService,
        ContractService contractService,
        IBulkUserImportService bulkUserImportService)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _verificationService = verificationService;
        _contractService = contractService;
        _bulkUserImportService = bulkUserImportService;
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
        var loan = await _context.LoanApplications
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == id);
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

        // Automatically generate contract for approved loan
        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
            var contract = await _contractService.GenerateCreditAgreementAsync(loan.Id, adminUserId);
            
            _logger.LogInformation("Contract {ContractId} automatically generated for approved loan {LoanId}", 
                contract.Id, loan.Id);
            
            return Ok(new { 
                message = "Loan approved successfully and contract generated", 
                loanId = loan.Id,
                contractId = contract.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate contract for approved loan {LoanId}", loan.Id);
            return Ok(new { 
                message = "Loan approved successfully, but contract generation failed. Contract can be generated later.", 
                loanId = loan.Id 
            });
        }
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

    // ============= DOCUMENT VERIFICATION =============

    /// <summary>
    /// Get all documents pending verification
    /// </summary>
    [HttpGet("documents/pending")]
    public async Task<ActionResult<List<DocumentDto>>> GetPendingDocuments()
    {
        try
        {
            var documents = await _context.UserDocuments
                .Include(d => d.User)
                .Where(d => d.Status == DocumentStatus.Pending && !d.IsDeleted)
                .OrderBy(d => d.UploadedAt)
                .Select(d => new DocumentDto
                {
                    Id = d.Id,
                    UserId = d.UserId,
                    UserName = d.User != null ? $"{d.User.FirstName} {d.User.LastName}" : "",
                    DocumentType = d.DocumentType,
                    DocumentTypeName = d.DocumentType.ToString(),
                    FileName = d.FileName,
                    FileSize = d.FileSize,
                    ContentType = d.ContentType,
                    FileContentBase64 = d.FileContentBase64,
                    Status = d.Status,
                    StatusName = d.Status.ToString(),
                    UploadedAt = d.UploadedAt,
                    Notes = d.Notes
                })
                .ToListAsync();

            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending documents");
            return StatusCode(500, "Error retrieving pending documents");
        }
    }

    /// <summary>
    /// Get all documents for a specific user
    /// </summary>
    [HttpGet("documents/user/{userId}")]
    public async Task<ActionResult<List<DocumentDto>>> GetUserDocuments(string userId)
    {
        try
        {
            var documents = await _context.UserDocuments
                .Include(d => d.User)
                .Include(d => d.VerifiedBy)
                .Where(d => d.UserId == userId && !d.IsDeleted)
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new DocumentDto
                {
                    Id = d.Id,
                    UserId = d.UserId,
                    UserName = d.User != null ? $"{d.User.FirstName} {d.User.LastName}" : "",
                    DocumentType = d.DocumentType,
                    DocumentTypeName = d.DocumentType.ToString(),
                    FileName = d.FileName,
                    FileSize = d.FileSize,
                    FileContentBase64 = d.FileContentBase64,
                    ContentType = d.ContentType,
                    Status = d.Status,
                    StatusName = d.Status.ToString(),
                    RejectionReason = d.RejectionReason,
                    VerifiedByUserId = d.VerifiedByUserId,
                    VerifiedByUserName = d.VerifiedBy != null ? $"{d.VerifiedBy.FirstName} {d.VerifiedBy.LastName}" : null,
                    UploadedAt = d.UploadedAt,
                    VerifiedAt = d.VerifiedAt,
                    Notes = d.Notes
                })
                .ToListAsync();

            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user documents for user {UserId}", userId);
            return StatusCode(500, "Error retrieving user documents");
        }
    }

    /// <summary>
    /// Approve or reject a document
    /// </summary>
    [HttpPost("documents/{id}/verify")]
    public async Task<ActionResult> VerifyDocument(Guid id, [FromBody] VerifyDocumentAdminDto dto)
    {
        try
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminUserId))
                return Unauthorized();

            var document = await _context.UserDocuments
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (document == null)
                return NotFound("Document not found");

            if (dto.Status == DocumentStatus.Rejected && string.IsNullOrWhiteSpace(dto.RejectionReason))
                return BadRequest("Rejection reason is required when rejecting a document");

            document.Status = dto.Status;
            document.RejectionReason = dto.RejectionReason;
            document.VerifiedByUserId = adminUserId;
            document.VerifiedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(dto.Notes))
            {
                document.Notes = dto.Notes;
            }

            _context.UserDocuments.Update(document);
            await _context.SaveChangesAsync();

            // Update user verification status
            await _verificationService.UpdateUserVerificationStatusAsync(document.UserId);

            _logger.LogInformation("Document {DocumentId} {Status} by admin {AdminId}", 
                id, dto.Status, adminUserId);

            return Ok(new { message = $"Document {dto.Status.ToString().ToLower()} successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying document {DocumentId}", id);
            return StatusCode(500, "Error verifying document");
        }
    }

    /// <summary>
    /// Get user verification status (admin view)
    /// </summary>
    [HttpGet("users/{userId}/verification-status")]
    public async Task<ActionResult<UserVerificationStatusDto>> GetUserVerificationStatus(string userId)
    {
        try
        {
            var status = await _verificationService.GetVerificationStatusAsync(userId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving verification status for user {UserId}", userId);
            return StatusCode(500, "Error retrieving verification status");
        }
    }

    // ============= DISBURSEMENT =============

    /// <summary>
    /// Mark a loan as disbursed
    /// </summary>
    [HttpPost("loans/{id}/disburse")]
    public async Task<ActionResult> DisburseLoan(Guid id, [FromBody] DisburseLoanDto dto)
    {
        var loan = await _context.LoanApplications.FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null)
            return NotFound("Loan application not found");

        if (loan.Status != LoanStatus.Approved)
            return BadRequest("Only approved loans can be disbursed");

        loan.Status = LoanStatus.Disbursed;
        // Note: Add DisbursementDate field to LoanApplication model if needed
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            loan.Notes += $"\nDisbursement: {dto.Notes}";
        }

        _context.LoanApplications.Update(loan);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Loan disbursed successfully", loan.Id });
    }

    // ============= WHATSAPP MANAGEMENT (existing code continues below) =============

    /// <summary>
    /// Get loans ready for disbursement (approved but not yet disbursed)
    /// </summary>
    [HttpGet("loans/ready-for-payout")]
    public async Task<ActionResult<object>> GetLoansReadyForPayout(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.LoanApplications
            .Include(l => l.User)
            .Where(l => l.Status == LoanStatus.Approved)
            .OrderBy(l => l.ApprovalDate);

        var totalCount = await query.CountAsync();
        var pageCount = (int)Math.Ceiling(totalCount / (double)pageSize);

        var loans = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id,
                l.Amount,
                l.InterestRate,
                l.TermMonths,
                l.MonthlyPayment,
                l.TotalAmount,
                l.Status,
                l.ApplicationDate,
                l.ApprovalDate,
                l.BankName,
                l.AccountNumber,
                l.AccountHolderName,
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

        return Ok(new
        {
            totalCount,
            pageCount,
            currentPage = page,
            pageSize,
            data = loans
        });
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
        _logger.LogInformation("Admin requesting WhatsApp conversations. Page={Page}, PageSize={PageSize}, Search={Search}", 
            page, pageSize, search);

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
        
        _logger.LogInformation("Found {Count} total WhatsApp conversations", totalCount);

        var conversations = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Subject,
                c.Status,
                c.Type,
                c.CreatedAt,
                c.UpdatedAt,
                Contact = new
                {
                    c.Contact.Id,
                    c.Contact.PhoneNumber,
                    c.Contact.DisplayName
                },
                MessageCount = c.Messages.Count,
                UnreadCount = c.Messages.Count(m => m.Direction == MessageDirection.Inbound && m.Status != MessageStatus.Read),
                LastMessage = c.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => new 
                    { 
                        m.Id, 
                        m.MessageText, 
                        m.Direction, 
                        m.CreatedAt, 
                        m.Status,
                        m.Type
                    })
                    .FirstOrDefault(),
                Messages = c.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(20)
                    .Select(m => new 
                    { 
                        m.Id, 
                        m.MessageText, 
                        m.Direction, 
                        m.CreatedAt, 
                        m.Status,
                        m.Type,
                        m.MediaUrl,
                        m.MediaType,
                        m.DeliveredAt,
                        m.ReadAt
                    })
                    .ToList()
            })
            .ToListAsync();

        var pageCount = (int)Math.Ceiling((double)totalCount / pageSize);

        _logger.LogInformation("Returning {Count} conversations for page {Page}", conversations.Count, page);

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
        _logger.LogInformation("Admin sending WhatsApp message. ConversationId={ConversationId}, ContentLength={Length}", 
            dto.ConversationId, dto.Content?.Length ?? 0);

        if (!int.TryParse(dto.ConversationId, out var conversationId))
        {
            _logger.LogWarning("Invalid conversation ID format: {ConversationId}", dto.ConversationId);
            return BadRequest("Invalid conversation ID");
        }

        var conversation = await _context.WhatsAppConversations
            .Include(c => c.Contact)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null)
        {
            _logger.LogWarning("Conversation not found: {ConversationId}", conversationId);
            return NotFound("Conversation not found");
        }

        // Determine message direction (outbound for admin sending)
        var direction = MessageDirection.Outbound;
        
        // Determine message status (sent)
        var status = MessageStatus.Sent;

        var message = new WhatsAppMessage
        {
            ConversationId = conversationId,
            ContactId = conversation.ContactId,
            MessageText = dto.Content,
            Type = MessageType.Text,
            Direction = direction,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        _context.WhatsAppMessages.Add(message);
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Message saved to database. MessageId={MessageId}, To={PhoneNumber}", 
            message.Id, conversation.Contact.PhoneNumber);

        return Ok(new { 
            message = "Message sent", 
            messageId = message.Id,
            conversationId = conversationId,
            timestamp = message.CreatedAt
        });
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
            .ToListAsync();

        var userData = new List<object>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            userData.Add(new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.PhoneNumber,
                u.IdNumber,
                u.MonthlyIncome,
                u.IsVerified,
                u.StreetAddress,
                u.City,
                u.Province,
                u.PostalCode,
                u.EmployerName,
                u.EmploymentType,
                u.BusinessId,
                BusinessName = u.BusinessId.HasValue 
                    ? _context.Businesses.Where(b => b.Id == u.BusinessId).Select(b => b.Name).FirstOrDefault() ?? ""
                    : "",
                u.CreatedAt,
                Roles = roles
            });
        }

        var pageCount = (int)Math.Ceiling((double)totalCount / pageSize);

        return Ok(new
        {
            totalCount,
            pageCount,
            currentPage = page,
            pageSize,
            data = userData
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

    /// <summary>
    /// Update a user's details and roles
    /// </summary>
    [HttpPut("users/{id}")]
    public async Task<ActionResult> UpdateUser(string id, [FromBody] AdminUpdateUserDto dto)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound("User not found");

            // Update basic fields
            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                user.FirstName = dto.FirstName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.LastName))
                user.LastName = dto.LastName.Trim();
            if (dto.PhoneNumber != null)
                user.PhoneNumber = dto.PhoneNumber.Trim();
            if (dto.Email != null)
            {
                user.Email = dto.Email.Trim();
                user.UserName = dto.Email.Trim();
                user.NormalizedEmail = dto.Email.Trim().ToUpperInvariant();
                user.NormalizedUserName = dto.Email.Trim().ToUpperInvariant();
            }
            if (dto.IdNumber != null)
                user.IdNumber = dto.IdNumber.Trim();
            if (dto.IsVerified.HasValue)
                user.IsVerified = dto.IsVerified.Value;

            // Address
            if (dto.StreetAddress != null) user.StreetAddress = dto.StreetAddress;
            if (dto.City != null) user.City = dto.City;
            if (dto.Province != null) user.Province = dto.Province;
            if (dto.PostalCode != null) user.PostalCode = dto.PostalCode;

            // Employment
            if (dto.EmployerName != null) user.EmployerName = dto.EmployerName;
            if (dto.EmploymentType != null) user.EmploymentType = dto.EmploymentType;

            // Business assignment
            if (dto.BusinessId.HasValue)
            {
                var business = await _context.Businesses.FindAsync(dto.BusinessId.Value);
                if (business != null)
                {
                    user.BusinessId = dto.BusinessId.Value;
                    user.EmployerName = business.Name; // Keep legacy field in sync
                }
            }
            else if (dto.BusinessId == null && dto.ClearBusiness == true)
            {
                user.BusinessId = null;
            }

            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                return BadRequest(new { message = "Failed to update user", errors });
            }

            // Reset password if provided
            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    var pwErrors = passwordResult.Errors.Select(e => e.Description).ToArray();
                    return BadRequest(new { message = "Failed to set password: " + string.Join(", ", pwErrors), errors = pwErrors });
                }
                _logger.LogInformation("[ADMIN] Password reset for user {UserId} by admin", id);
            }

            // Update roles if provided
            if (dto.Roles != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                var rolesToRemove = currentRoles.Except(dto.Roles).ToList();
                var rolesToAdd = dto.Roles.Except(currentRoles).ToList();

                if (rolesToRemove.Any())
                    await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (rolesToAdd.Any())
                    await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var loans = await _context.LoanApplications
                .Where(l => l.UserId == id)
                .Select(l => new { l.Id, l.Amount, l.Status, l.ApplicationDate })
                .ToListAsync();

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.IdNumber,
                user.MonthlyIncome,
                user.IsVerified,
                user.StreetAddress,
                user.City,
                user.Province,
                user.PostalCode,
                user.EmployerName,
                user.EmploymentType,
                user.CreatedAt,
                Roles = roles,
                LoanApplications = loans
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new { message = "Error updating user" });
        }
    }

    // ============= BULK USER IMPORT =============

    /// <summary>
    /// Validate bulk user import file
    /// </summary>
    [HttpPost("users/bulk-import/validate")]
    [RequestSizeLimit(10485760)] // 10MB limit
    public async Task<ActionResult<BulkImportValidationResult>> ValidateBulkUserImport(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only CSV files are supported");
        }

        if (file.Length > 10485760) // 10MB
        {
            return BadRequest("File size exceeds 10MB limit");
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _bulkUserImportService.ValidateImportDataAsync(stream, file.FileName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating bulk import file {FileName}", file.FileName);
            return StatusCode(500, "Error processing file");
        }
    }

    /// <summary>
    /// Import validated users
    /// </summary>
    [HttpPost("users/bulk-import/import")]
    public async Task<ActionResult<BulkImportResult>> ImportBulkUsers([FromBody] List<BulkUserImportDto> users)
    {
        if (users == null || !users.Any())
        {
            return BadRequest("No users provided for import");
        }

        try
        {
            var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var result = await _bulkUserImportService.ImportUsersAsync(users, adminUserId);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing bulk users");
            return StatusCode(500, "Error importing users");
        }
    }

    /// <summary>
    /// Get bulk import CSV template
    /// </summary>
    [HttpGet("users/bulk-import/template")]
    public ActionResult GetBulkImportTemplate()
    {
        var csvContent = "Email,FirstName,LastName,IdNumber,DateOfBirth,Address,PhoneNumber,MonthlyIncome\n" +
                        "john.doe@example.com,John,Doe,8001015009087,1980-01-01,\"123 Main St, Johannesburg\",+27821234567,15000\n" +
                        "jane.smith@example.com,Jane,Smith,8505205009088,1985-05-20,\"456 Oak Ave, Cape Town\",+27829876543,18000";

        var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
        return File(bytes, "text/csv", "bulk_import_template.csv");
    }

    // ============= DEDUCTION SCHEDULE =============

    /// <summary>
    /// Generate deduction schedule for a disbursed loan
    /// </summary>
    [HttpPost("deductions/generate/{loanId}")]
    public async Task<ActionResult> GenerateDeductionSchedule(Guid loanId)
    {
        var loan = await _context.LoanApplications
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == loanId);
        if (loan == null) return NotFound("Loan not found");
        if (loan.Status != LoanStatus.Disbursed)
            return BadRequest("Deduction schedule can only be generated for disbursed loans");

        // Check if schedule already exists
        var existing = await _context.DeductionScheduleEntries
            .AnyAsync(d => d.LoanApplicationId == loanId);
        if (existing) return BadRequest("Deduction schedule already exists for this loan");

        var entries = new List<DeductionScheduleEntry>();
        var termMonths = loan.TermMonths > 0 ? loan.TermMonths : 1;
        var monthlyPayment = loan.MonthlyPayment;
        var totalInterest = loan.TotalAmount - loan.Amount;
        var monthlyInterest = totalInterest / termMonths;
        var monthlyPrincipal = loan.Amount / termMonths;
        var adminFee = loan.AppliedAdminFee ?? 0;

        // Determine start date based on RepaymentDay
        var repaymentDay = loan.RepaymentDay ?? 25;
        var now = DateTime.UtcNow;
        var firstDue = new DateTime(now.Year, now.Month, Math.Min(repaymentDay, DateTime.DaysInMonth(now.Year, now.Month)), 0, 0, 0, DateTimeKind.Utc);
        if (firstDue <= now) firstDue = firstDue.AddMonths(1);

        for (int i = 1; i <= termMonths; i++)
        {
            var dueDate = firstDue.AddMonths(i - 1);
            // Adjust for months with fewer days
            var maxDay = DateTime.DaysInMonth(dueDate.Year, dueDate.Month);
            dueDate = new DateTime(dueDate.Year, dueDate.Month, Math.Min(repaymentDay, maxDay), 0, 0, 0, DateTimeKind.Utc);

            entries.Add(new DeductionScheduleEntry
            {
                LoanApplicationId = loanId,
                UserId = loan.UserId,
                InstallmentNumber = i,
                DueDate = dueDate,
                PrincipalAmount = Math.Round(monthlyPrincipal, 2),
                InterestAmount = Math.Round(monthlyInterest, 2),
                AdminFeeAmount = i == 1 ? adminFee : 0,
                TotalAmount = Math.Round(monthlyPayment + (i == 1 ? adminFee : 0), 2),
                Status = DeductionStatus.Scheduled
            });
        }

        _context.DeductionScheduleEntries.AddRange(entries);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Generated {entries.Count} deduction entries", count = entries.Count });
    }

    /// <summary>
    /// Get all deduction schedules with filters
    /// </summary>
    [HttpGet("deductions")]
    public async Task<ActionResult> GetDeductions(
        [FromQuery] string? status = null,
        [FromQuery] Guid? loanId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.DeductionScheduleEntries
            .Include(d => d.LoanApplication)
            .Include(d => d.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<DeductionStatus>(status, true, out var st))
            query = query.Where(d => d.Status == st);
        if (loanId.HasValue)
            query = query.Where(d => d.LoanApplicationId == loanId.Value);
        if (fromDate.HasValue)
            query = query.Where(d => d.DueDate >= fromDate.Value.ToUniversalTime());
        if (toDate.HasValue)
            query = query.Where(d => d.DueDate <= toDate.Value.ToUniversalTime());

        // Auto-mark overdue entries
        var now = DateTime.UtcNow;
        var overdueEntries = await _context.DeductionScheduleEntries
            .Where(d => d.DueDate < now && d.Status == DeductionStatus.Scheduled)
            .ToListAsync();
        if (overdueEntries.Any())
        {
            foreach (var entry in overdueEntries)
                entry.Status = DeductionStatus.Overdue;
            await _context.SaveChangesAsync();
        }

        var totalCount = await query.CountAsync();
        var entries = await query
            .OrderBy(d => d.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                d.Id,
                d.LoanApplicationId,
                d.InstallmentNumber,
                d.DueDate,
                d.PrincipalAmount,
                d.InterestAmount,
                d.AdminFeeAmount,
                d.TotalAmount,
                Status = d.Status.ToString(),
                d.PaidDate,
                d.PaidAmount,
                d.PaymentReference,
                d.BankTransactionId,
                d.Notes,
                Borrower = d.User != null ? $"{d.User.FirstName} {d.User.LastName}" : "Unknown",
                BorrowerEmail = d.User != null ? d.User.Email : null,
                LoanAmount = d.LoanApplication != null ? d.LoanApplication.Amount : 0
            })
            .ToListAsync();

        // Summary stats
        var allEntries = _context.DeductionScheduleEntries.AsQueryable();
        if (loanId.HasValue) allEntries = allEntries.Where(d => d.LoanApplicationId == loanId.Value);
        
        var summary = new
        {
            totalScheduled = await allEntries.CountAsync(d => d.Status == DeductionStatus.Scheduled),
            totalPaid = await allEntries.CountAsync(d => d.Status == DeductionStatus.Paid),
            totalOverdue = await allEntries.CountAsync(d => d.Status == DeductionStatus.Overdue),
            totalFailed = await allEntries.CountAsync(d => d.Status == DeductionStatus.Failed),
            amountExpected = await allEntries.Where(d => d.Status == DeductionStatus.Scheduled || d.Status == DeductionStatus.Overdue).SumAsync(d => d.TotalAmount),
            amountCollected = await allEntries.Where(d => d.Status == DeductionStatus.Paid).SumAsync(d => d.PaidAmount ?? d.TotalAmount)
        };

        return Ok(new { totalCount, page, pageSize, summary, entries });
    }

    /// <summary>
    /// Get deduction schedule for a specific loan
    /// </summary>
    [HttpGet("deductions/loan/{loanId}")]
    public async Task<ActionResult> GetLoanDeductions(Guid loanId)
    {
        var entries = await _context.DeductionScheduleEntries
            .Where(d => d.LoanApplicationId == loanId)
            .OrderBy(d => d.InstallmentNumber)
            .Select(d => new
            {
                d.Id,
                d.InstallmentNumber,
                d.DueDate,
                d.PrincipalAmount,
                d.InterestAmount,
                d.AdminFeeAmount,
                d.TotalAmount,
                Status = d.Status.ToString(),
                d.PaidDate,
                d.PaidAmount,
                d.PaymentReference,
                d.Notes
            })
            .ToListAsync();

        return Ok(entries);
    }

    /// <summary>
    /// Mark a deduction as paid
    /// </summary>
    [HttpPost("deductions/{id}/mark-paid")]
    public async Task<ActionResult> MarkDeductionPaid(Guid id, [FromBody] MarkDeductionPaidDto dto)
    {
        var entry = await _context.DeductionScheduleEntries.FindAsync(id);
        if (entry == null) return NotFound("Deduction entry not found");

        entry.Status = DeductionStatus.Paid;
        entry.PaidDate = dto.PaidDate?.ToUniversalTime() ?? DateTime.UtcNow;
        entry.PaidAmount = dto.Amount ?? entry.TotalAmount;
        entry.PaymentReference = dto.PaymentReference;
        entry.Notes = dto.Notes;
        entry.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Deduction marked as paid" });
    }

    /// <summary>
    /// Update deduction status (failed, reversed, etc.)
    /// </summary>
    [HttpPut("deductions/{id}/status")]
    public async Task<ActionResult> UpdateDeductionStatus(Guid id, [FromBody] UpdateDeductionStatusDto dto)
    {
        var entry = await _context.DeductionScheduleEntries.FindAsync(id);
        if (entry == null) return NotFound("Deduction entry not found");

        if (!Enum.TryParse<DeductionStatus>(dto.Status, true, out var newStatus))
            return BadRequest("Invalid status");

        entry.Status = newStatus;
        entry.Notes = dto.Notes ?? entry.Notes;
        entry.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Deduction status updated to {newStatus}" });
    }

    /// <summary>
    /// Get loans that have been disbursed but don't have deduction schedules yet
    /// </summary>
    [HttpGet("deductions/unscheduled-loans")]
    public async Task<ActionResult> GetUnscheduledLoans()
    {
        var scheduledLoanIds = await _context.DeductionScheduleEntries
            .Select(d => d.LoanApplicationId)
            .Distinct()
            .ToListAsync();

        var unscheduledLoans = await _context.LoanApplications
            .Include(l => l.User)
            .Where(l => l.Status == LoanStatus.Disbursed && !scheduledLoanIds.Contains(l.Id))
            .Select(l => new
            {
                l.Id,
                l.Amount,
                l.TotalAmount,
                l.MonthlyPayment,
                l.TermMonths,
                l.InterestRate,
                l.RepaymentDay,
                l.ApplicationDate,
                l.ApprovalDate,
                Borrower = l.User != null ? $"{l.User.FirstName} {l.User.LastName}" : "Unknown"
            })
            .ToListAsync();

        return Ok(unscheduledLoans);
    }

    // ============= BANK RECONCILIATION (FNB) =============

    /// <summary>
    /// Upload FNB bank statement CSV for reconciliation
    /// </summary>
    [HttpPost("bank-recon/upload")]
    public async Task<ActionResult> UploadBankStatement(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");
        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only CSV files are supported");

        var importBatchId = Guid.NewGuid().ToString("N")[..12];
        var transactions = new List<BankTransaction>();

        using var reader = new StreamReader(file.OpenReadStream());
        var lineNumber = 0;
        var headerRow = -1;
        var colDate = -1; var colAmount = -1; var colBalance = -1;
        var colDescription = -1; var colReference = -1;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;
            lineNumber++;

            // Parse CSV (handle quoted fields)
            var fields = ParseCsvLine(line);

            // Auto-detect header row by looking for common FNB column names
            if (headerRow < 0)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    var f = fields[i].Trim().ToLower();
                    if (f.Contains("date")) colDate = i;
                    else if (f == "amount" || f.Contains("amount")) colAmount = i;
                    else if (f.Contains("balance")) colBalance = i;
                    else if (f.Contains("description") || f.Contains("desc")) colDescription = i;
                    else if (f.Contains("reference") || f.Contains("ref")) colReference = i;
                }
                if (colDate >= 0 && colAmount >= 0)
                {
                    headerRow = lineNumber;
                    continue;
                }
                // If no header found, try common FNB format: Date, Amount, Balance, Description
                if (lineNumber > 5)
                {
                    colDate = 0; colAmount = 1; colBalance = 2; colDescription = 3;
                    headerRow = 0; // No header, data starts from line 1
                }
                else continue;
            }

            if (fields.Length <= colDate || fields.Length <= colAmount) continue;

            // Parse date
            if (!DateTime.TryParse(fields[colDate].Trim().Trim('"'), out var txDate)) continue;
            
            // Parse amount
            var amountStr = fields[colAmount].Trim().Trim('"').Replace(" ", "").Replace("R", "").Replace(",", "");
            if (!decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount)) continue;

            // Parse balance
            decimal? balance = null;
            if (colBalance >= 0 && colBalance < fields.Length)
            {
                var balStr = fields[colBalance].Trim().Trim('"').Replace(" ", "").Replace("R", "").Replace(",", "");
                if (decimal.TryParse(balStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bal))
                    balance = bal;
            }

            var description = colDescription >= 0 && colDescription < fields.Length 
                ? fields[colDescription].Trim().Trim('"') : "";
            var reference = colReference >= 0 && colReference < fields.Length 
                ? fields[colReference].Trim().Trim('"') : null;

            transactions.Add(new BankTransaction
            {
                TransactionDate = DateTime.SpecifyKind(txDate, DateTimeKind.Utc),
                Amount = Math.Abs(amount),
                Balance = balance,
                Description = description,
                Reference = reference,
                Type = amount >= 0 ? TransactionType.Credit : TransactionType.Debit,
                Category = amount >= 0 ? TransactionCategory.LoanRepayment : TransactionCategory.LoanDisbursement,
                ImportBatchId = importBatchId,
                SourceFileName = file.FileName
            });
        }

        if (!transactions.Any())
            return BadRequest("No valid transactions found in the CSV file. Please ensure it contains Date and Amount columns.");

        _context.BankTransactions.AddRange(transactions);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Imported {transactions.Count} transactions",
            importBatchId,
            count = transactions.Count,
            credits = transactions.Count(t => t.Type == TransactionType.Credit),
            debits = transactions.Count(t => t.Type == TransactionType.Debit)
        });
    }

    /// <summary>
    /// Get bank transactions with filters
    /// </summary>
    [HttpGet("bank-recon/transactions")]
    public async Task<ActionResult> GetBankTransactions(
        [FromQuery] string? matchStatus = null,
        [FromQuery] string? type = null,
        [FromQuery] string? importBatchId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.BankTransactions.AsQueryable();

        if (!string.IsNullOrEmpty(matchStatus) && Enum.TryParse<MatchStatus>(matchStatus, true, out var ms))
            query = query.Where(t => t.MatchStatus == ms);
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<TransactionType>(type, true, out var tt))
            query = query.Where(t => t.Type == tt);
        if (!string.IsNullOrEmpty(importBatchId))
            query = query.Where(t => t.ImportBatchId == importBatchId);
        if (fromDate.HasValue)
            query = query.Where(t => t.TransactionDate >= fromDate.Value.ToUniversalTime());
        if (toDate.HasValue)
            query = query.Where(t => t.TransactionDate <= toDate.Value.ToUniversalTime());

        var totalCount = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.TransactionDate,
                t.Amount,
                t.Balance,
                t.Description,
                t.Reference,
                Type = t.Type.ToString(),
                Category = t.Category.ToString(),
                MatchStatus = t.MatchStatus.ToString(),
                t.MatchedDeductionId,
                t.MatchedLoanId,
                t.ImportBatchId,
                t.SourceFileName,
                t.Notes,
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(new { totalCount, page, pageSize, transactions });
    }

    /// <summary>
    /// Auto-match bank transactions against deduction schedule
    /// </summary>
    [HttpPost("bank-recon/auto-match")]
    public async Task<ActionResult> AutoMatchTransactions()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var unmatchedCredits = await _context.BankTransactions
            .Where(t => t.MatchStatus == MatchStatus.Unmatched && t.Type == TransactionType.Credit)
            .ToListAsync();

        var overdueAndScheduled = await _context.DeductionScheduleEntries
            .Include(d => d.User)
            .Where(d => d.Status == DeductionStatus.Scheduled || d.Status == DeductionStatus.Overdue)
            .ToListAsync();

        var matched = 0;
        foreach (var tx in unmatchedCredits)
        {
            // Try to match by amount and reference
            var match = overdueAndScheduled.FirstOrDefault(d =>
            {
                // Amount match (within R1 tolerance)
                if (Math.Abs(d.TotalAmount - tx.Amount) > 1) return false;

                // If we have a reference, try to match against borrower name or loan reference
                if (!string.IsNullOrEmpty(tx.Description) && d.User != null)
                {
                    var desc = tx.Description.ToLower();
                    var name = $"{d.User.FirstName} {d.User.LastName}".ToLower();
                    if (desc.Contains(name) || desc.Contains(d.User.LastName?.ToLower() ?? ""))
                        return true;
                }

                // Match by amount only if date is close to due date (within 5 days)
                if (Math.Abs((tx.TransactionDate - d.DueDate).TotalDays) <= 5)
                    return true;

                return false;
            });

            if (match != null)
            {
                tx.MatchStatus = MatchStatus.AutoMatched;
                tx.MatchedDeductionId = match.Id;
                tx.MatchedByUserId = userId;
                tx.MatchedAt = DateTime.UtcNow;
                tx.Category = TransactionCategory.LoanRepayment;

                match.Status = DeductionStatus.Paid;
                match.PaidDate = tx.TransactionDate;
                match.PaidAmount = tx.Amount;
                match.PaymentReference = tx.Reference ?? tx.Description;
                match.BankTransactionId = tx.Id;
                match.UpdatedAt = DateTime.UtcNow;

                // Remove from candidates so it doesn't match again
                overdueAndScheduled.Remove(match);
                matched++;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new
        {
            message = $"Auto-matched {matched} transactions",
            matched,
            remaining = unmatchedCredits.Count - matched
        });
    }

    /// <summary>
    /// Manually match a bank transaction to a deduction entry
    /// </summary>
    [HttpPost("bank-recon/manual-match")]
    public async Task<ActionResult> ManualMatchTransaction([FromBody] ManualMatchDto dto)
    {
        var tx = await _context.BankTransactions.FindAsync(dto.TransactionId);
        if (tx == null) return NotFound("Transaction not found");

        var deduction = await _context.DeductionScheduleEntries.FindAsync(dto.DeductionId);
        if (deduction == null) return NotFound("Deduction entry not found");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        tx.MatchStatus = MatchStatus.ManuallyMatched;
        tx.MatchedDeductionId = deduction.Id;
        tx.MatchedByUserId = userId;
        tx.MatchedAt = DateTime.UtcNow;
        tx.Category = TransactionCategory.LoanRepayment;

        deduction.Status = DeductionStatus.Paid;
        deduction.PaidDate = tx.TransactionDate;
        deduction.PaidAmount = tx.Amount;
        deduction.PaymentReference = tx.Reference ?? tx.Description;
        deduction.BankTransactionId = tx.Id;
        deduction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Transaction manually matched" });
    }

    /// <summary>
    /// Ignore a bank transaction (not loan-related)
    /// </summary>
    [HttpPost("bank-recon/{id}/ignore")]
    public async Task<ActionResult> IgnoreTransaction(Guid id)
    {
        var tx = await _context.BankTransactions.FindAsync(id);
        if (tx == null) return NotFound("Transaction not found");

        tx.MatchStatus = MatchStatus.Ignored;
        tx.Category = TransactionCategory.Other;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Transaction ignored" });
    }

    /// <summary>
    /// End of day reconciliation summary
    /// </summary>
    [HttpGet("bank-recon/daily-summary")]
    public async Task<ActionResult> GetDailySummary([FromQuery] DateTime? date = null)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;
        var nextDate = targetDate.AddDays(1);
        var utcTarget = DateTime.SpecifyKind(targetDate, DateTimeKind.Utc);
        var utcNext = DateTime.SpecifyKind(nextDate, DateTimeKind.Utc);

        // Expected deductions due today
        var expectedDeductions = await _context.DeductionScheduleEntries
            .Include(d => d.User)
            .Where(d => d.DueDate >= utcTarget && d.DueDate < utcNext)
            .Select(d => new
            {
                d.Id,
                d.LoanApplicationId,
                d.InstallmentNumber,
                d.TotalAmount,
                Status = d.Status.ToString(),
                d.PaidAmount,
                Borrower = d.User != null ? $"{d.User.FirstName} {d.User.LastName}" : "Unknown"
            })
            .ToListAsync();

        // Bank transactions for the day
        var dayTransactions = await _context.BankTransactions
            .Where(t => t.TransactionDate >= utcTarget && t.TransactionDate < utcNext)
            .ToListAsync();

        // All overdue deductions
        var overdueCount = await _context.DeductionScheduleEntries
            .CountAsync(d => d.DueDate < utcTarget && (d.Status == DeductionStatus.Scheduled || d.Status == DeductionStatus.Overdue));

        var totalExpected = expectedDeductions.Sum(d => d.TotalAmount);
        var totalReceived = dayTransactions.Where(t => t.Type == TransactionType.Credit).Sum(t => t.Amount);
        var totalPaidOut = dayTransactions.Where(t => t.Type == TransactionType.Debit).Sum(t => t.Amount);
        var totalMatched = dayTransactions.Count(t => t.MatchStatus != MatchStatus.Unmatched);
        var totalUnmatched = dayTransactions.Count(t => t.MatchStatus == MatchStatus.Unmatched);

        return Ok(new
        {
            date = targetDate.ToString("yyyy-MM-dd"),
            deductions = new
            {
                expected = expectedDeductions.Count,
                totalExpected,
                paid = expectedDeductions.Count(d => d.Status == "Paid"),
                outstanding = expectedDeductions.Count(d => d.Status != "Paid"),
                overdueTotal = overdueCount,
                items = expectedDeductions
            },
            bankActivity = new
            {
                totalTransactions = dayTransactions.Count,
                totalReceived,
                totalPaidOut,
                netFlow = totalReceived - totalPaidOut,
                matched = totalMatched,
                unmatched = totalUnmatched
            },
            variance = totalReceived - totalExpected
        });
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var inQuotes = false;
        var field = "";
        foreach (var c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (c == ',' && !inQuotes) { fields.Add(field); field = ""; continue; }
            field += c;
        }
        fields.Add(field);
        return fields.ToArray();
    }

    // ============= BUSINESS / EMPLOYER MANAGEMENT =============

    /// <summary>
    /// Get all businesses
    /// </summary>
    [HttpGet("businesses")]
    public async Task<ActionResult> GetBusinesses(
        [FromQuery] bool? activeOnly = null,
        [FromQuery] string? search = null)
    {
        var query = _context.Businesses.AsQueryable();

        if (activeOnly == true)
            query = query.Where(b => b.IsActive);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(b =>
                b.Name.Contains(search) ||
                b.RegistrationNumber.Contains(search) ||
                b.ContactPerson.Contains(search));
        }

        var businesses = await query
            .OrderBy(b => b.Name)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.RegistrationNumber,
                b.ContactPerson,
                b.ContactEmail,
                b.ContactPhone,
                b.City,
                b.Province,
                b.PayrollDay,
                b.MaxLoanPercentage,
                b.InterestRate,
                b.AdminFee,
                b.IsActive,
                b.CreatedAt,
                EmployeeCount = b.Employees.Count()
            })
            .ToListAsync();

        return Ok(businesses);
    }

    /// <summary>
    /// Get a single business with its employees
    /// </summary>
    [HttpGet("businesses/{id}")]
    public async Task<ActionResult> GetBusiness(Guid id)
    {
        var business = await _context.Businesses
            .Include(b => b.Employees)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (business == null)
            return NotFound("Business not found");

        var employees = business.Employees.Select(e => new
        {
            e.Id,
            e.FirstName,
            e.LastName,
            e.Email,
            e.PhoneNumber,
            e.IdNumber,
            e.EmployeeNumber,
            e.PayrollReference,
            e.EmploymentType,
            e.MonthlyIncome,
            e.IsVerified,
            e.CreatedAt
        }).OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ToList();

        return Ok(new
        {
            business.Id,
            business.Name,
            business.RegistrationNumber,
            business.ContactPerson,
            business.ContactEmail,
            business.ContactPhone,
            business.Address,
            business.City,
            business.Province,
            business.PostalCode,
            business.PayrollContactName,
            business.PayrollContactEmail,
            business.PayrollDay,
            business.MaxLoanPercentage,
            business.InterestRate,
            business.AdminFee,
            business.IsActive,
            business.Notes,
            business.CreatedAt,
            business.UpdatedAt,
            Employees = employees,
            EmployeeCount = employees.Count
        });
    }

    /// <summary>
    /// Create a new business
    /// </summary>
    [HttpPost("businesses")]
    public async Task<ActionResult> CreateBusiness([FromBody] CreateBusinessDto dto)
    {
        var business = new Business
        {
            Name = dto.Name.Trim(),
            RegistrationNumber = dto.RegistrationNumber?.Trim() ?? "",
            ContactPerson = dto.ContactPerson?.Trim() ?? "",
            ContactEmail = dto.ContactEmail?.Trim() ?? "",
            ContactPhone = dto.ContactPhone?.Trim() ?? "",
            Address = dto.Address?.Trim() ?? "",
            City = dto.City?.Trim() ?? "",
            Province = dto.Province?.Trim() ?? "",
            PostalCode = dto.PostalCode?.Trim() ?? "",
            PayrollContactName = dto.PayrollContactName?.Trim() ?? "",
            PayrollContactEmail = dto.PayrollContactEmail?.Trim() ?? "",
            PayrollDay = dto.PayrollDay ?? 25,
            MaxLoanPercentage = dto.MaxLoanPercentage ?? 30,
            InterestRate = dto.InterestRate,
            AdminFee = dto.AdminFee,
            IsActive = true,
            Notes = dto.Notes?.Trim() ?? ""
        };

        _context.Businesses.Add(business);
        await _context.SaveChangesAsync();

        _logger.LogInformation("[ADMIN] Created business {BusinessName} ({BusinessId})", business.Name, business.Id);

        return Ok(new { business.Id, business.Name, message = "Business created successfully" });
    }

    /// <summary>
    /// Update a business
    /// </summary>
    [HttpPut("businesses/{id}")]
    public async Task<ActionResult> UpdateBusiness(Guid id, [FromBody] UpdateBusinessDto dto)
    {
        var business = await _context.Businesses.FindAsync(id);
        if (business == null)
            return NotFound("Business not found");

        if (dto.Name != null) business.Name = dto.Name.Trim();
        if (dto.RegistrationNumber != null) business.RegistrationNumber = dto.RegistrationNumber.Trim();
        if (dto.ContactPerson != null) business.ContactPerson = dto.ContactPerson.Trim();
        if (dto.ContactEmail != null) business.ContactEmail = dto.ContactEmail.Trim();
        if (dto.ContactPhone != null) business.ContactPhone = dto.ContactPhone.Trim();
        if (dto.Address != null) business.Address = dto.Address.Trim();
        if (dto.City != null) business.City = dto.City.Trim();
        if (dto.Province != null) business.Province = dto.Province.Trim();
        if (dto.PostalCode != null) business.PostalCode = dto.PostalCode.Trim();
        if (dto.PayrollContactName != null) business.PayrollContactName = dto.PayrollContactName.Trim();
        if (dto.PayrollContactEmail != null) business.PayrollContactEmail = dto.PayrollContactEmail.Trim();
        if (dto.PayrollDay.HasValue) business.PayrollDay = dto.PayrollDay.Value;
        if (dto.MaxLoanPercentage.HasValue) business.MaxLoanPercentage = dto.MaxLoanPercentage.Value;
        if (dto.InterestRate.HasValue) business.InterestRate = dto.InterestRate.Value;
        if (dto.AdminFee.HasValue) business.AdminFee = dto.AdminFee.Value;
        if (dto.IsActive.HasValue) business.IsActive = dto.IsActive.Value;
        if (dto.Notes != null) business.Notes = dto.Notes.Trim();

        business.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("[ADMIN] Updated business {BusinessName} ({BusinessId})", business.Name, business.Id);

        return Ok(new { business.Id, business.Name, message = "Business updated successfully" });
    }

    /// <summary>
    /// Get employees of a specific business
    /// </summary>
    [HttpGet("businesses/{id}/employees")]
    public async Task<ActionResult> GetBusinessEmployees(Guid id)
    {
        var business = await _context.Businesses.FindAsync(id);
        if (business == null)
            return NotFound("Business not found");

        var employees = await _userManager.Users
            .Where(u => u.BusinessId == id)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.PhoneNumber,
                u.IdNumber,
                u.EmployeeNumber,
                u.PayrollReference,
                u.EmploymentType,
                u.MonthlyIncome,
                u.IsVerified,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(employees);
    }

    /// <summary>
    /// Assign a user to a business
    /// </summary>
    [HttpPost("businesses/{id}/employees")]
    public async Task<ActionResult> AssignUserToBusiness(Guid id, [FromBody] AssignUserToBusinessDto dto)
    {
        var business = await _context.Businesses.FindAsync(id);
        if (business == null)
            return NotFound("Business not found");

        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null)
            return NotFound("User not found");

        user.BusinessId = id;
        user.EmployerName = business.Name; // Keep legacy field in sync
        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("[ADMIN] Assigned user {UserId} to business {BusinessName} ({BusinessId})", dto.UserId, business.Name, id);

        return Ok(new { message = $"User assigned to {business.Name}" });
    }

    /// <summary>
    /// Remove a user from a business
    /// </summary>
    [HttpDelete("businesses/{businessId}/employees/{userId}")]
    public async Task<ActionResult> RemoveUserFromBusiness(Guid businessId, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found");

        if (user.BusinessId != businessId)
            return BadRequest("User is not assigned to this business");

        user.BusinessId = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("[ADMIN] Removed user {UserId} from business {BusinessId}", userId, businessId);

        return Ok(new { message = "User removed from business" });
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

public class DisburseLoanDto
{
    public string? Notes { get; set; }
}

public class SendWhatsAppMessageDto
{
    public string ConversationId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class VerifyDocumentAdminDto
{
    public DocumentStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
}

public class AdminUpdateUserDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? IdNumber { get; set; }
    public bool? IsVerified { get; set; }
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? EmployerName { get; set; }
    public string? EmploymentType { get; set; }
    public Guid? BusinessId { get; set; }
    public bool? ClearBusiness { get; set; }
    public List<string>? Roles { get; set; }
    public string? NewPassword { get; set; }
}

public class MarkDeductionPaidDto
{
    public decimal? Amount { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }
}

public class UpdateDeductionStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ManualMatchDto
{
    public Guid TransactionId { get; set; }
    public Guid DeductionId { get; set; }
}

public class CreateBusinessDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? PayrollContactName { get; set; }
    public string? PayrollContactEmail { get; set; }
    public int? PayrollDay { get; set; }
    public decimal? MaxLoanPercentage { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal? AdminFee { get; set; }
    public string? Notes { get; set; }
}

public class UpdateBusinessDto
{
    public string? Name { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public string? PayrollContactName { get; set; }
    public string? PayrollContactEmail { get; set; }
    public int? PayrollDay { get; set; }
    public decimal? MaxLoanPercentage { get; set; }
    public decimal? InterestRate { get; set; }
    public decimal? AdminFee { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}

public class AssignUserToBusinessDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;
}
