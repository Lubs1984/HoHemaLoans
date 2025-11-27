using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoHemaLoans.Api.Models;

public class LoanApplication
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    [Required]
    public int TermMonths { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Purpose { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public LoanStatus Status { get; set; } = LoanStatus.Pending;
    
    [Column(TypeName = "decimal(5,4)")]
    public decimal InterestRate { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlyPayment { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    
    public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? ApprovalDate { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Omnichannel fields
    [StringLength(20)]
    public LoanApplicationChannel ChannelOrigin { get; set; } = LoanApplicationChannel.Web;
    
    public DateTime? WhatsAppInitiatedDate { get; set; }
    
    public DateTime? WebInitiatedDate { get; set; }
    
    public int CurrentStep { get; set; } = 0;
    
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object>? StepData { get; set; }
    
    [StringLength(100)]
    public string? WhatsAppSessionId { get; set; }
    
    public bool IsAffordabilityIncluded { get; set; } = false;
    
    [StringLength(20)]
    public string? BankName { get; set; }
    
    [StringLength(50)]
    public string? AccountNumber { get; set; }
    
    [StringLength(100)]
    public string? AccountHolderName { get; set; }
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
}

public enum LoanStatus
{
    Draft,
    Pending,
    UnderReview,
    Approved,
    Rejected,
    Disbursed,
    Closed
}

public enum LoanApplicationChannel
{
    Web,
    WhatsApp
}

public enum ApplicationStep
{
    LoanAmount = 0,
    TermMonths = 1,
    Purpose = 2,
    AffordabilityReview = 3,
    PreviewTerms = 4,
    BankDetails = 5,
    DigitalSignature = 6
}