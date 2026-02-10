using Microsoft.EntityFrameworkCore;
using HoHemaLoans.Api.Models;
using HoHemaLoans.Api.Data;

namespace HoHemaLoans.Api.Services;

public interface INCRComplianceService
{
    Task<NCRConfiguration> GetNCRConfigurationAsync();
    Task<NCRComplianceResult> ValidateInterestRateAsync(decimal interestRate, decimal loanAmount);
    Task<NCRComplianceResult> ValidateFeesAsync(decimal initiationFee, decimal monthlyServiceFee, decimal loanAmount);
    Task<NCRComplianceResult> ValidateLoanTermsAsync(int termInMonths, decimal loanAmount);
    Task<NCRComplianceResult> ValidateAffordabilityAsync(decimal monthlyIncome, decimal monthlyExpenses, decimal proposedInstallment);
    Task<bool> IsWithinCoolingOffPeriodAsync(Guid loanApplicationId);
    Task<NCRComplianceResult> ValidateFullComplianceAsync(LoanCalculation calculation, decimal monthlyIncome, decimal monthlyExpenses);
    Task LogNCRActionAsync(string entityType, string entityId, NCRAuditAction action, string userId, string? details = null);
    Task<Form39Data> GenerateForm39DataAsync(Guid loanApplicationId);
    Task<PreAgreementStatementData> GeneratePreAgreementStatementAsync(LoanCalculation calculation, ApplicationUser user);
    Task<CoolingOffResult> CancelLoanWithinCoolingOffAsync(Guid loanApplicationId, string userId, string reason);
    Task<List<ConsumerComplaint>> GetActiveComplaintsAsync();
    Task<ConsumerComplaint> CreateComplaintAsync(CreateComplaintRequest request);
    Task<ConsumerComplaint> UpdateComplaintAsync(int complaintId, UpdateComplaintRequest request);
}

public class NCRComplianceService : INCRComplianceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NCRComplianceService> _logger;

    public NCRComplianceService(ApplicationDbContext context, ILogger<NCRComplianceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NCRConfiguration> GetNCRConfigurationAsync()
    {
        var config = await _context.NCRConfigurations.FirstOrDefaultAsync();
        if (config == null)
        {
            // Create default configuration
            config = new NCRConfiguration();
            _context.NCRConfigurations.Add(config);
            await _context.SaveChangesAsync();
        }
        return config;
    }

    public async Task<NCRComplianceResult> ValidateInterestRateAsync(decimal interestRate, decimal loanAmount)
    {
        var config = await GetNCRConfigurationAsync();
        
        if (!config.EnforceNCRCompliance)
        {
            return NCRComplianceResult.Success();
        }

        if (interestRate > config.MaxInterestRatePerAnnum)
        {
            return NCRComplianceResult.Failure(
                "INTEREST_RATE_EXCEEDS_NCR_CAP",
                $"Interest rate of {interestRate:F2}% exceeds the NCR maximum of {config.MaxInterestRatePerAnnum:F2}% per annum"
            );
        }

        return NCRComplianceResult.Success();
    }

    public async Task<NCRComplianceResult> ValidateFeesAsync(decimal initiationFee, decimal monthlyServiceFee, decimal loanAmount)
    {
        var config = await GetNCRConfigurationAsync();
        
        if (!config.EnforceNCRCompliance)
        {
            return NCRComplianceResult.Success();
        }

        var errors = new List<string>();

        // Check initiation fee
        var maxInitiationFee = Math.Min(
            config.MaxInitiationFee,
            loanAmount * (config.InitiationFeePercentage / 100)
        );

        if (initiationFee > maxInitiationFee)
        {
            errors.Add($"Initiation fee of R{initiationFee:F2} exceeds the NCR limit of R{maxInitiationFee:F2}");
        }

        // Check monthly service fee
        if (monthlyServiceFee > config.MaxMonthlyServiceFee)
        {
            errors.Add($"Monthly service fee of R{monthlyServiceFee:F2} exceeds the NCR limit of R{config.MaxMonthlyServiceFee:F2}");
        }

        if (errors.Any())
        {
            return NCRComplianceResult.Failure("FEE_EXCEEDS_NCR_CAP", string.Join("; ", errors));
        }

        return NCRComplianceResult.Success();
    }

    public async Task<NCRComplianceResult> ValidateLoanTermsAsync(int termInMonths, decimal loanAmount)
    {
        var config = await GetNCRConfigurationAsync();
        
        if (!config.EnforceNCRCompliance)
        {
            return NCRComplianceResult.Success();
        }

        var errors = new List<string>();

        if (loanAmount < config.MinLoanAmount)
        {
            errors.Add($"Loan amount of R{loanAmount:F2} is below the minimum of R{config.MinLoanAmount:F2}");
        }

        if (loanAmount > config.MaxLoanAmount)
        {
            errors.Add($"Loan amount of R{loanAmount:F2} exceeds the maximum of R{config.MaxLoanAmount:F2}");
        }

        if (termInMonths < config.MinLoanTermMonths)
        {
            errors.Add($"Loan term of {termInMonths} months is below the minimum of {config.MinLoanTermMonths} months");
        }

        if (termInMonths > config.MaxLoanTermMonths)
        {
            errors.Add($"Loan term of {termInMonths} months exceeds the maximum of {config.MaxLoanTermMonths} months");
        }

        if (errors.Any())
        {
            return NCRComplianceResult.Failure("LOAN_TERMS_NON_COMPLIANT", string.Join("; ", errors));
        }

        return NCRComplianceResult.Success();
    }

    public async Task<NCRComplianceResult> ValidateAffordabilityAsync(decimal monthlyIncome, decimal monthlyExpenses, decimal proposedInstallment)
    {
        var config = await GetNCRConfigurationAsync();
        
        if (!config.EnforceNCRCompliance)
        {
            return NCRComplianceResult.Success();
        }

        var disposableIncome = monthlyIncome - monthlyExpenses;
        var debtToIncomeRatio = (proposedInstallment / monthlyIncome) * 100;
        var remainingIncome = disposableIncome - proposedInstallment;

        var errors = new List<string>();

        if (debtToIncomeRatio > config.MaxDebtToIncomeRatio)
        {
            errors.Add($"Debt-to-income ratio of {debtToIncomeRatio:F2}% exceeds the NCR limit of {config.MaxDebtToIncomeRatio:F2}%");
        }

        if (remainingIncome < config.MinSafetyBuffer)
        {
            errors.Add($"Remaining income of R{remainingIncome:F2} is below the minimum safety buffer of R{config.MinSafetyBuffer:F2}");
        }

        if (proposedInstallment > disposableIncome)
        {
            errors.Add("Proposed installment exceeds disposable income");
        }

        if (errors.Any())
        {
            return NCRComplianceResult.Failure("AFFORDABILITY_NON_COMPLIANT", string.Join("; ", errors));
        }

        return NCRComplianceResult.Success();
    }

    public async Task<bool> IsWithinCoolingOffPeriodAsync(Guid loanApplicationId)
    {
        var config = await GetNCRConfigurationAsync();
        
        if (!config.AllowCoolingOffCancellation)
        {
            return false;
        }

        var loanApplication = await _context.LoanApplications
            .FirstOrDefaultAsync(la => la.Id == loanApplicationId);

        if (loanApplication?.SignedAt == null)
        {
            return false;
        }

        var coolingOffEndDate = loanApplication.SignedAt.Value.AddDays(config.CoolingOffPeriodDays);
        return DateTime.UtcNow <= coolingOffEndDate;
    }

    public async Task<NCRComplianceResult> ValidateFullComplianceAsync(LoanCalculation calculation, decimal monthlyIncome, decimal monthlyExpenses)
    {
        var errors = new List<string>();

        // Validate interest rate
        var interestResult = await ValidateInterestRateAsync(calculation.InterestRate, calculation.LoanAmount);
        if (!interestResult.IsCompliant)
        {
            errors.Add(interestResult.ErrorMessage!);
        }

        // Validate fees
        var feesResult = await ValidateFeesAsync(calculation.InitiationFee, calculation.MonthlyServiceFee, calculation.LoanAmount);
        if (!feesResult.IsCompliant)
        {
            errors.Add(feesResult.ErrorMessage!);
        }

        // Validate loan terms
        var termsResult = await ValidateLoanTermsAsync(calculation.TermInMonths, calculation.LoanAmount);
        if (!termsResult.IsCompliant)
        {
            errors.Add(termsResult.ErrorMessage!);
        }

        // Validate affordability
        var affordabilityResult = await ValidateAffordabilityAsync(monthlyIncome, monthlyExpenses, calculation.MonthlyInstallment);
        if (!affordabilityResult.IsCompliant)
        {
            errors.Add(affordabilityResult.ErrorMessage!);
        }

        if (errors.Any())
        {
            return NCRComplianceResult.Failure("FULL_COMPLIANCE_FAILED", string.Join("; ", errors));
        }

        return NCRComplianceResult.Success();
    }

    public async Task LogNCRActionAsync(string entityType, string entityId, NCRAuditAction action, string userId, string? details = null)
    {
        var auditLog = new NCRAuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = userId,
            Details = details,
            ComplianceRelated = true
        };

        _context.NCRAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("NCR Action logged: {Action} for {EntityType} {EntityId} by user {UserId}", 
            action, entityType, entityId, userId);
    }

    public async Task<Form39Data> GenerateForm39DataAsync(Guid loanApplicationId)
    {
        var loanApplication = await _context.LoanApplications
            .Include(la => la.User)
            .Include(la => la.LoanCalculation)
            .Include(la => la.PersonalInformation)
            .Include(la => la.FinancialInformation)
            .FirstOrDefaultAsync(la => la.Id == loanApplicationId);

        if (loanApplication == null)
        {
            throw new InvalidOperationException("Loan application not found");
        }

        var config = await GetNCRConfigurationAsync();

        return new Form39Data
        {
            // Credit Provider Details
            CreditProviderName = "Ho Hema Loans (Pty) Ltd",
            NCRCPRegistrationNumber = config.NCRCPRegistrationNumber ?? "PENDING",
            CreditProviderAddress = "123 Business Park, Cape Town, South Africa",
            
            // Consumer Details
            ConsumerName = $"{loanApplication.User.FirstName} {loanApplication.User.LastName}",
            ConsumerIdNumber = loanApplication.PersonalInformation?.IdNumber ?? "",
            ConsumerAddress = $"{loanApplication.PersonalInformation?.HomeAddress}",
            ConsumerPhone = loanApplication.User.PhoneNumber ?? "",
            ConsumerEmail = loanApplication.User.Email ?? "",
            
            // Credit Agreement Details
            AgreementDate = loanApplication.SignedAt ?? DateTime.UtcNow,
            PrincipalDebt = loanApplication.LoanCalculation?.LoanAmount ?? 0,
            InterestRate = loanApplication.LoanCalculation?.InterestRate ?? 0,
            InitiationFee = loanApplication.LoanCalculation?.InitiationFee ?? 0,
            MonthlyServiceFee = loanApplication.LoanCalculation?.MonthlyServiceFee ?? 0,
            TermInMonths = loanApplication.LoanCalculation?.TermInMonths ?? 0,
            MonthlyInstallment = loanApplication.LoanCalculation?.MonthlyInstallment ?? 0,
            TotalAmountPayable = loanApplication.LoanCalculation?.TotalAmountPayable ?? 0,
            
            // Affordability Assessment
            MonthlyIncome = loanApplication.FinancialInformation?.MonthlyIncome ?? 0,
            MonthlyExpenses = loanApplication.FinancialInformation?.MonthlyExpenses ?? 0,
            DebtToIncomeRatio = CalculateDebtToIncomeRatio(
                loanApplication.FinancialInformation?.MonthlyIncome ?? 0,
                loanApplication.LoanCalculation?.MonthlyInstallment ?? 0
            ),
            
            // Legal Requirements
            CoolingOffPeriodDays = config.CoolingOffPeriodDays,
            ComplaintsProcedure = "Complaints can be lodged through our website, phone, or email. Unresolved complaints may be escalated to the NCR.",
            
            GeneratedAt = DateTime.UtcNow,
            LoanApplicationId = loanApplicationId
        };
    }

    public async Task<PreAgreementStatementData> GeneratePreAgreementStatementAsync(LoanCalculation calculation, ApplicationUser user)
    {
        var config = await GetNCRConfigurationAsync();

        return new PreAgreementStatementData
        {
            ConsumerName = $"{user.FirstName} {user.LastName}",
            ConsumerEmail = user.Email ?? "",
            
            LoanAmount = calculation.LoanAmount,
            InterestRate = calculation.InterestRate,
            TermInMonths = calculation.TermInMonths,
            
            InitiationFee = calculation.InitiationFee,
            MonthlyServiceFee = calculation.MonthlyServiceFee,
            MonthlyInstallment = calculation.MonthlyInstallment,
            TotalAmountPayable = calculation.TotalAmountPayable,
            
            CoolingOffPeriod = $"You have {config.CoolingOffPeriodDays} days from signing to cancel this agreement.",
            
            ImportantNotices = new List<string>
            {
                "This is a credit agreement as defined in the National Credit Act.",
                $"The maximum interest rate allowed by law is {config.MaxInterestRatePerAnnum:F2}% per annum.",
                "You have the right to apply for debt review if you are over-indebted.",
                "You have the right to receive a statement of account on request.",
                "Complaints can be escalated to the National Credit Regulator if not resolved.",
                "Early settlement may reduce the total cost of credit."
            },
            
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<CoolingOffResult> CancelLoanWithinCoolingOffAsync(Guid loanApplicationId, string userId, string reason)
    {
        var loanApplication = await _context.LoanApplications
            .FirstOrDefaultAsync(la => la.Id == loanApplicationId);

        if (loanApplication == null)
        {
            return new CoolingOffResult
            {
                IsSuccessful = false,
                ErrorMessage = "Loan application not found"
            };
        }

        var isWithinPeriod = await IsWithinCoolingOffPeriodAsync(loanApplicationId);
        if (!isWithinPeriod)
        {
            return new CoolingOffResult
            {
                IsSuccessful = false,
                ErrorMessage = "Cooling-off period has expired"
            };
        }

        // Create cancellation record
        var cancellation = new LoanCancellation
        {
            LoanApplicationId = loanApplicationId,
            UserId = userId,
            CancellationReason = reason,
            IsWithinCoolingOffPeriod = true
        };

        _context.LoanCancellations.Add(cancellation);

        // Update loan application status
        loanApplication.Status = LoanStatus.Cancelled;
        loanApplication.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log the action
        await LogNCRActionAsync("LoanApplication", loanApplicationId.ToString(), NCRAuditAction.LoanCancelled, userId, 
            $"Cancelled within cooling-off period: {reason}");

        return new CoolingOffResult
        {
            IsSuccessful = true,
            CancellationId = cancellation.Id,
            RefundAmount = 0 // Usually no fees charged within cooling-off period
        };
    }

    public async Task<List<ConsumerComplaint>> GetActiveComplaintsAsync()
    {
        return await _context.ConsumerComplaints
            .Include(c => c.User)
            .Include(c => c.LoanApplication)
            .Include(c => c.AssignedToUser)
            .Include(c => c.Notes)
            .Where(c => c.Status != ComplaintStatus.Closed)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<ConsumerComplaint> CreateComplaintAsync(CreateComplaintRequest request)
    {
        var complaint = new ConsumerComplaint
        {
            UserId = request.UserId,
            LoanApplicationId = request.LoanApplicationId,
            Subject = request.Subject,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority
        };

        _context.ConsumerComplaints.Add(complaint);
        await _context.SaveChangesAsync();

        // Log the complaint creation
        await LogNCRActionAsync("Complaint", complaint.Id.ToString(), NCRAuditAction.ComplaintCreated, request.UserId, 
            $"Category: {request.Category}, Priority: {request.Priority}");

        return complaint;
    }

    public async Task<ConsumerComplaint> UpdateComplaintAsync(int complaintId, UpdateComplaintRequest request)
    {
        var complaint = await _context.ConsumerComplaints
            .Include(c => c.Notes)
            .FirstOrDefaultAsync(c => c.Id == complaintId);

        if (complaint == null)
        {
            throw new InvalidOperationException("Complaint not found");
        }

        complaint.Status = request.Status;
        complaint.Priority = request.Priority;
        complaint.AssignedToUserId = request.AssignedToUserId;
        complaint.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Resolution))
        {
            complaint.Resolution = request.Resolution;
            complaint.ResolvedAt = DateTime.UtcNow;
            complaint.ResolvedBy = request.ResolvedBy;
        }

        if (!string.IsNullOrEmpty(request.Note))
        {
            var note = new ComplaintNote
            {
                ComplaintId = complaintId,
                Note = request.Note,
                CreatedBy = request.UpdatedBy,
                IsPublic = request.IsNotePublic
            };
            complaint.Notes.Add(note);
        }

        await _context.SaveChangesAsync();
        return complaint;
    }

    private static decimal CalculateDebtToIncomeRatio(decimal monthlyIncome, decimal monthlyInstallment)
    {
        if (monthlyIncome == 0) return 0;
        return (monthlyInstallment / monthlyIncome) * 100;
    }
}

// Supporting classes
public class NCRComplianceResult
{
    public bool IsCompliant { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static NCRComplianceResult Success() => new() { IsCompliant = true };
    public static NCRComplianceResult Failure(string errorCode, string errorMessage) => 
        new() { IsCompliant = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}

public class Form39Data
{
    // Credit Provider
    public string CreditProviderName { get; set; } = string.Empty;
    public string NCRCPRegistrationNumber { get; set; } = string.Empty;
    public string CreditProviderAddress { get; set; } = string.Empty;
    
    // Consumer
    public string ConsumerName { get; set; } = string.Empty;
    public string ConsumerIdNumber { get; set; } = string.Empty;
    public string ConsumerAddress { get; set; } = string.Empty;
    public string ConsumerPhone { get; set; } = string.Empty;
    public string ConsumerEmail { get; set; } = string.Empty;
    
    // Agreement
    public DateTime AgreementDate { get; set; }
    public decimal PrincipalDebt { get; set; }
    public decimal InterestRate { get; set; }
    public decimal InitiationFee { get; set; }
    public decimal MonthlyServiceFee { get; set; }
    public int TermInMonths { get; set; }
    public decimal MonthlyInstallment { get; set; }
    public decimal TotalAmountPayable { get; set; }
    
    // Affordability
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal DebtToIncomeRatio { get; set; }
    
    // Legal
    public int CoolingOffPeriodDays { get; set; }
    public string ComplaintsProcedure { get; set; } = string.Empty;
    
    public DateTime GeneratedAt { get; set; }
    public Guid LoanApplicationId { get; set; }
}

public class PreAgreementStatementData
{
    public string ConsumerName { get; set; } = string.Empty;
    public string ConsumerEmail { get; set; } = string.Empty;
    
    public decimal LoanAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int TermInMonths { get; set; }
    
    public decimal InitiationFee { get; set; }
    public decimal MonthlyServiceFee { get; set; }
    public decimal MonthlyInstallment { get; set; }
    public decimal TotalAmountPayable { get; set; }
    
    public string CoolingOffPeriod { get; set; } = string.Empty;
    public List<string> ImportantNotices { get; set; } = new();
    
    public DateTime GeneratedAt { get; set; }
}

public class CoolingOffResult
{
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public int? CancellationId { get; set; }
    public decimal RefundAmount { get; set; }
}

public class CreateComplaintRequest
{
    public string UserId { get; set; } = string.Empty;
    public Guid? LoanApplicationId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplaintCategory Category { get; set; }
    public ComplaintPriority Priority { get; set; } = ComplaintPriority.Medium;
}

public class UpdateComplaintRequest
{
    public ComplaintStatus Status { get; set; }
    public ComplaintPriority Priority { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? Resolution { get; set; }
    public string? ResolvedBy { get; set; }
    public string? Note { get; set; }
    public bool IsNotePublic { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}