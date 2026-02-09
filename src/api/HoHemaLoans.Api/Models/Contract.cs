using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoHemaLoans.Api.Models;

/// <summary>
/// Represents a credit agreement contract for a loan application
/// NCR Compliance: Form 39 - Credit Agreement
/// </summary>
public class Contract
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid LoanApplicationId { get; set; }

    [ForeignKey(nameof(LoanApplicationId))]
    public LoanApplication? LoanApplication { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// Type of contract: CreditAgreement, PreAgreementStatement, AffordabilityAssessmentForm
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ContractType { get; set; } = "CreditAgreement";

    /// <summary>
    /// Full contract content in HTML format for rendering
    /// </summary>
    [Required]
    public string ContractContent { get; set; } = string.Empty;

    /// <summary>
    /// URL or path to the generated PDF document
    /// </summary>
    [MaxLength(500)]
    public string? DocumentPath { get; set; }

    /// <summary>
    /// Status: Draft, Sent, Signed, Expired, Cancelled
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// When the contract was generated
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the contract was sent to the user for signing
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// When the contract was signed
    /// </summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// Contract expiry date (typically 30 days after generation)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Version number for tracking changes
    /// </summary>
    [Required]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Additional metadata (e.g., NCR registration number, compliance data)
    /// </summary>
    public string? Metadata { get; set; }

    // Navigation property
    public DigitalSignature? DigitalSignature { get; set; }
}

/// <summary>
/// Contract types enum for reference
/// </summary>
public static class ContractTypes
{
    public const string CreditAgreement = "CreditAgreement"; // Form 39
    public const string PreAgreementStatement = "PreAgreementStatement";
    public const string AffordabilityAssessmentForm = "AffordabilityAssessmentForm";
    public const string Addendum = "Addendum";
}

/// <summary>
/// Contract status enum for reference
/// </summary>
public static class ContractStatus
{
    public const string Draft = "Draft";
    public const string Sent = "Sent";
    public const string Signed = "Signed";
    public const string Expired = "Expired";
    public const string Cancelled = "Cancelled";
}
