using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoHemaLoans.Api.Models;

/// <summary>
/// Represents a document uploaded by a user for verification purposes
/// </summary>
public class UserDocument
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public DocumentType DocumentType { get; set; }
    
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;
    
    [Required]
    public long FileSize { get; set; }
    
    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;
    
    /// <summary>
    /// BASE64 encoded file content for ID documents and Passports
    /// Allows direct database storage and display without file system
    /// </summary>
    public string? FileContentBase64 { get; set; }
    
    [Required]
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    
    [StringLength(500)]
    public string? RejectionReason { get; set; }
    
    public string? VerifiedByUserId { get; set; }
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? VerifiedAt { get; set; }
    
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    public bool IsDeleted { get; set; } = false;
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
    
    [ForeignKey(nameof(VerifiedByUserId))]
    public virtual ApplicationUser? VerifiedBy { get; set; }
}

/// <summary>
/// Types of documents that can be uploaded for verification
/// </summary>
public enum DocumentType
{
    IdDocument = 1,
    ProofOfAddress = 2,
    BankStatement = 3,
    Payslip = 4,
    EmploymentLetter = 5,
    Other = 99
}

/// <summary>
/// Status of document verification
/// </summary>
public enum DocumentStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}
