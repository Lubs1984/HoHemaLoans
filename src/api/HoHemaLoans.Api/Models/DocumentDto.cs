namespace HoHemaLoans.Api.Models;

/// <summary>
/// DTO for document information
/// </summary>
public class DocumentDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public string DocumentTypeName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? FileContentBase64 { get; set; }
    public DocumentStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public string? VerifiedByUserId { get; set; }
    public string? VerifiedByUserName { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for uploading a document
/// </summary>
public class UploadDocumentDto
{
    public DocumentType DocumentType { get; set; }
    public IFormFile File { get; set; } = null!;
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for verifying a document (approve/reject)
/// </summary>
public class VerifyDocumentDto
{
    public DocumentStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for user verification status
/// </summary>
public class UserVerificationStatusDto
{
    public bool IsVerified { get; set; }
    public int TotalDocuments { get; set; }
    public int ApprovedDocuments { get; set; }
    public int PendingDocuments { get; set; }
    public int RejectedDocuments { get; set; }
    public List<DocumentDto> Documents { get; set; } = new();
    public List<string> MissingDocuments { get; set; } = new();
}
