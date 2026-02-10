using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HoHemaLoans.Api.Services;

/// <summary>
/// Service to handle profile verification based on uploaded documents
/// </summary>
public interface IProfileVerificationService
{
    Task<UserVerificationStatusDto> GetVerificationStatusAsync(string userId);
    Task<bool> UpdateUserVerificationStatusAsync(string userId);
}

public class ProfileVerificationService : IProfileVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ProfileVerificationService> _logger;

    // Required documents for full verification
    private readonly List<DocumentType> _requiredDocuments = new()
    {
        DocumentType.IdDocument,
        DocumentType.ProofOfAddress
    };

    public ProfileVerificationService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<ProfileVerificationService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<UserVerificationStatusDto> GetVerificationStatusAsync(string userId)
    {
        var documents = await _context.UserDocuments
            .Where(d => d.UserId == userId && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new ArgumentException($"User not found: {userId}");
        }

        var documentDtos = documents.Select(d => new DocumentDto
        {
            Id = d.Id,
            UserId = d.UserId,
            UserName = $"{user.FirstName} {user.LastName}",
            DocumentType = d.DocumentType,
            DocumentTypeName = d.DocumentType.ToString(),
            FileName = d.FileName,
            FileSize = d.FileSize,
            ContentType = d.ContentType,
            Status = d.Status,
            StatusName = d.Status.ToString(),
            RejectionReason = d.RejectionReason,
            VerifiedByUserId = d.VerifiedByUserId,
            UploadedAt = d.UploadedAt,
            VerifiedAt = d.VerifiedAt,
            Notes = d.Notes
        }).ToList();

        var approvedDocuments = documents.Count(d => d.Status == DocumentStatus.Approved);
        var pendingDocuments = documents.Count(d => d.Status == DocumentStatus.Pending);
        var rejectedDocuments = documents.Count(d => d.Status == DocumentStatus.Rejected);

        // Check which required documents are missing (not uploaded at all, or all rejected)
        // Pending and Approved documents count as "uploaded" - the user has done their part
        var uploadedDocumentTypes = documents
            .Where(d => d.Status == DocumentStatus.Approved || d.Status == DocumentStatus.Pending)
            .Select(d => d.DocumentType)
            .ToHashSet();

        var missingDocuments = _requiredDocuments
            .Where(reqDoc => !uploadedDocumentTypes.Contains(reqDoc))
            .Select(d => d.ToString())
            .ToList();

        return new UserVerificationStatusDto
        {
            IsVerified = user.IsVerified,
            TotalDocuments = documents.Count,
            ApprovedDocuments = approvedDocuments,
            PendingDocuments = pendingDocuments,
            RejectedDocuments = rejectedDocuments,
            Documents = documentDtos,
            MissingDocuments = missingDocuments
        };
    }

    public async Task<bool> UpdateUserVerificationStatusAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return false;
            }

            var approvedDocuments = await _context.UserDocuments
                .Where(d => d.UserId == userId && d.Status == DocumentStatus.Approved && !d.IsDeleted)
                .Select(d => d.DocumentType)
                .Distinct()
                .ToListAsync();

            // Check if all required documents are approved
            var hasAllRequiredDocuments = _requiredDocuments.All(reqDoc => approvedDocuments.Contains(reqDoc));

            // Update user verification status
            if (hasAllRequiredDocuments && !user.IsVerified)
            {
                user.IsVerified = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("User {UserId} verified successfully", userId);
                return true;
            }
            else if (!hasAllRequiredDocuments && user.IsVerified)
            {
                // If required documents are no longer approved, remove verification
                user.IsVerified = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("User {UserId} verification removed due to missing documents", userId);
                return false;
            }

            return user.IsVerified;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating verification status for user {UserId}", userId);
            throw;
        }
    }
}
