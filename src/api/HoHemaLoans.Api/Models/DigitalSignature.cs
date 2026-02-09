using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoHemaLoans.Api.Models;

/// <summary>
/// Represents a digital signature for a contract using WhatsApp PIN verification
/// Provides audit trail for NCR compliance
/// </summary>
public class DigitalSignature
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ContractId { get; set; }

    [ForeignKey(nameof(ContractId))]
    public Contract? Contract { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// Signature method: WhatsAppPIN, OTP, BiometricHash
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string SignatureMethod { get; set; } = "WhatsAppPIN";

    /// <summary>
    /// Hashed PIN (6-digit) sent via WhatsApp
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string SignatureHash { get; set; } = string.Empty;

    /// <summary>
    /// Salt used for PIN hashing
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Salt { get; set; } = string.Empty;

    /// <summary>
    /// Phone number where PIN was sent (for audit)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// IP address from which signature was submitted
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string for device identification
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Geolocation data if available (latitude, longitude)
    /// </summary>
    [MaxLength(100)]
    public string? GeoLocation { get; set; }

    /// <summary>
    /// When the PIN was generated and sent
    /// </summary>
    [Required]
    public DateTime PinSentAt { get; set; }

    /// <summary>
    /// When the PIN expires (typically 10 minutes after generation)
    /// </summary>
    [Required]
    public DateTime PinExpiresAt { get; set; }

    /// <summary>
    /// When the signature was successfully verified
    /// </summary>
    public DateTime? SignedAt { get; set; }

    /// <summary>
    /// Number of failed verification attempts
    /// </summary>
    public int FailedAttempts { get; set; } = 0;

    /// <summary>
    /// Whether the signature is valid
    /// </summary>
    [Required]
    public bool IsValid { get; set; } = false;

    /// <summary>
    /// Additional metadata for audit trail
    /// </summary>
    public string? AuditMetadata { get; set; }

    /// <summary>
    /// Full name of signer at time of signing (for verification)
    /// </summary>
    [MaxLength(200)]
    public string? SignerName { get; set; }

    /// <summary>
    /// ID number of signer at time of signing (for verification)
    /// </summary>
    [MaxLength(50)]
    public string? SignerIdNumber { get; set; }
}

/// <summary>
/// Signature methods enum for reference
/// </summary>
public static class SignatureMethods
{
    public const string WhatsAppPIN = "WhatsAppPIN";
    public const string SMSOTP = "SMSOTP";
    public const string EmailOTP = "EmailOTP";
    public const string BiometricHash = "BiometricHash";
}
