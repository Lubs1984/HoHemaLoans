using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoHemaLoans.Api.Models;

/// <summary>
/// NCR Configuration for compliance parameters and limits
/// </summary>
public class NCRConfiguration
{
    [Key]
    public int Id { get; set; }

    // Interest Rate Caps (per annum)
    [Column(TypeName = "decimal(5,2)")]
    public decimal MaxInterestRatePerAnnum { get; set; } = 27.5m; // NCR maximum

    [Column(TypeName = "decimal(5,2)")]
    public decimal DefaultInterestRatePerAnnum { get; set; } = 24.0m;

    // Fee Caps
    [Column(TypeName = "decimal(10,2)")]
    public decimal MaxInitiationFee { get; set; } = 1140.00m; // NCR maximum

    [Column(TypeName = "decimal(5,2)")]
    public decimal InitiationFeePercentage { get; set; } = 15.0m; // Percentage of loan amount

    [Column(TypeName = "decimal(10,2)")]
    public decimal MaxMonthlyServiceFee { get; set; } = 60.00m; // NCR maximum

    [Column(TypeName = "decimal(10,2)")]
    public decimal DefaultMonthlyServiceFee { get; set; } = 50.00m;

    // Affordability thresholds
    [Column(TypeName = "decimal(5,2)")]
    public decimal MaxDebtToIncomeRatio { get; set; } = 35.0m; // 35% NCR limit

    [Column(TypeName = "decimal(10,2)")]
    public decimal MinSafetyBuffer { get; set; } = 500.00m; // Minimum remaining income

    // Loan amount limits
    [Column(TypeName = "decimal(15,2)")]
    public decimal MinLoanAmount { get; set; } = 500.00m;

    [Column(TypeName = "decimal(15,2)")]
    public decimal MaxLoanAmount { get; set; } = 300000.00m;

    // Loan term limits (in months)
    public int MinLoanTermMonths { get; set; } = 6;
    public int MaxLoanTermMonths { get; set; } = 60;

    // Cooling-off period (in days)
    public int CoolingOffPeriodDays { get; set; } = 5;

    // NCR Registration details
    public string? NCRCPRegistrationNumber { get; set; }
    public string? ComplianceOfficerName { get; set; }
    public string? ComplianceOfficerEmail { get; set; }

    // Document retention (in years)
    public int DocumentRetentionYears { get; set; } = 5;

    // System settings
    public bool EnforceNCRCompliance { get; set; } = true;
    public bool AllowCoolingOffCancellation { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Loan Application Cancellation for cooling-off period
/// </summary>
public class LoanCancellation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid LoanApplicationId { get; set; }
    public LoanApplication? LoanApplication { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required]
    public string CancellationReason { get; set; } = string.Empty;

    public DateTime CancelledAt { get; set; } = DateTime.UtcNow;
    
    public bool IsWithinCoolingOffPeriod { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? RefundReference { get; set; }
    public DateTime? RefundProcessedAt { get; set; }

    public string? Notes { get; set; }
}

/// <summary>
/// Consumer Complaint for NCR compliance
/// </summary>
public class ConsumerComplaint
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public Guid? LoanApplicationId { get; set; }
    public LoanApplication? LoanApplication { get; set; }

    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public ComplaintCategory Category { get; set; }

    [Required]
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;

    public ComplaintPriority Priority { get; set; } = ComplaintPriority.Medium;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }

    public string? Resolution { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }

    // NCR escalation
    public bool EscalatedToNCR { get; set; }
    public DateTime? EscalatedAt { get; set; }
    public string? NCRReferenceNumber { get; set; }

    public List<ComplaintNote> Notes { get; set; } = new List<ComplaintNote>();
}

/// <summary>
/// Complaint follow-up notes
/// </summary>
public class ComplaintNote
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ComplaintId { get; set; }
    public ConsumerComplaint? Complaint { get; set; }

    [Required]
    public string Note { get; set; } = string.Empty;

    [Required]
    public string CreatedBy { get; set; } = string.Empty;
    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsPublic { get; set; } = false; // Visible to consumer
}

/// <summary>
/// NCR Audit Log for compliance tracking
/// </summary>
public class NCRAuditLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string EntityType { get; set; } = string.Empty; // LoanApplication, Contract, etc.

    [Required]
    public string EntityId { get; set; } = string.Empty;

    [Required]
    public NCRAuditAction Action { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public string? Details { get; set; } // JSON details of the action

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }

    // NCR specific fields
    public bool ComplianceRelated { get; set; }
    public string? ComplianceNotes { get; set; }
}

// Enums
public enum ComplaintCategory
{
    CreditAgreement,
    InterestRates,
    Fees,
    CustomerService,
    PaymentIssues,
    Documentation,
    Affordability,
    DebtCollection,
    Privacy,
    Other
}

public enum ComplaintStatus
{
    Open,
    InProgress,
    PendingCustomer,
    Resolved,
    Closed,
    EscalatedToNCR
}

public enum ComplaintPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum NCRAuditAction
{
    LoanApplicationCreated,
    LoanApplicationApproved,
    LoanApplicationRejected,
    ContractGenerated,
    ContractSigned,
    LoanDisbursed,
    PaymentReceived,
    LoanCancelled,
    ComplaintCreated,
    ComplianceViolation,
    SettingsChanged,
    UserRegistered,
    AffordabilityAssessed
}