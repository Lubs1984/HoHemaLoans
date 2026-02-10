using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;
using HoHemaLoans.Api.Services;
using System.Security.Claims;

namespace HoHemaLoans.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentStorageService _storageService;
    private readonly IProfileVerificationService _verificationService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        ApplicationDbContext context,
        IDocumentStorageService storageService,
        IProfileVerificationService verificationService,
        ILogger<DocumentsController> logger)
    {
        _context = context;
        _storageService = storageService;
        _verificationService = verificationService;
        _logger = logger;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    /// <summary>
    /// Upload a new document
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentDto>> UploadDocument([FromForm] IFormFile file, [FromForm] string documentType, [FromForm] string? notes)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            if (!Enum.TryParse<DocumentType>(documentType, out var docType))
                return BadRequest("Invalid document type");

            // Store all documents as BASE64 in database for easy retrieval and preview
            string? base64Content = null;
            string filePath = string.Empty;
            
            base64Content = await _storageService.ConvertToBase64Async(file);
            filePath = $"base64_{userId}_{docType}"; // Placeholder path

            // Save document record to database
            var document = new UserDocument
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DocumentType = docType,
                FileName = file.FileName,
                FilePath = filePath,
                FileSize = file.Length,
                ContentType = file.ContentType,
                FileContentBase64 = base64Content,
                Status = DocumentStatus.Pending,
                UploadedAt = DateTime.UtcNow,
                Notes = notes
            };

            _context.UserDocuments.Add(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document uploaded: {DocumentId} for user {UserId}", document.Id, userId);

            var user = await _context.Users.FindAsync(userId);
            var documentDto = new DocumentDto
            {
                Id = document.Id,
                UserId = document.UserId,
                UserName = user != null ? $"{user.FirstName} {user.LastName}" : "",
                DocumentType = document.DocumentType,
                DocumentTypeName = document.DocumentType.ToString(),
                FileName = document.FileName,
                FileSize = document.FileSize,
                ContentType = document.ContentType,
                Status = document.Status,
                StatusName = document.Status.ToString(),
                UploadedAt = document.UploadedAt,
                Notes = document.Notes
            };

            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, documentDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, $"Error uploading document: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all documents for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<DocumentDto>>> GetUserDocuments()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

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
            _logger.LogError(ex, "Error retrieving documents");
            return StatusCode(500, "Error retrieving documents");
        }
    }

    /// <summary>
    /// Get a specific document by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentDto>> GetDocument(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var document = await _context.UserDocuments
                .Include(d => d.User)
                .Include(d => d.VerifiedBy)
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (document == null)
                return NotFound();

            // User can only access their own documents (unless admin)
            if (document.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            var documentDto = new DocumentDto
            {
                Id = document.Id,
                UserId = document.UserId,
                UserName = document.User != null ? $"{document.User.FirstName} {document.User.LastName}" : "",
                DocumentType = document.DocumentType,
                DocumentTypeName = document.DocumentType.ToString(),
                FileName = document.FileName,
                FileContentBase64 = document.FileContentBase64,
                FileSize = document.FileSize,
                ContentType = document.ContentType,
                Status = document.Status,
                StatusName = document.Status.ToString(),
                RejectionReason = document.RejectionReason,
                VerifiedByUserId = document.VerifiedByUserId,
                VerifiedByUserName = document.VerifiedBy != null ? $"{document.VerifiedBy.FirstName} {document.VerifiedBy.LastName}" : null,
                UploadedAt = document.UploadedAt,
                VerifiedAt = document.VerifiedAt,
                Notes = document.Notes
            };

            return Ok(documentDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId}", id);
            return StatusCode(500, "Error retrieving document");
        }
    }

    /// <summary>
    /// Download a document file
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadDocument(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var document = await _context.UserDocuments
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (document == null)
                return NotFound();

            // User can only download their own documents (unless admin)
            if (document.UserId != userId && !User.IsInRole("Admin"))
                return Forbid();

            // If document is stored as BASE64, convert back to stream
            if (!string.IsNullOrEmpty(document.FileContentBase64))
            {
                var bytes = Convert.FromBase64String(document.FileContentBase64);
                var stream = new MemoryStream(bytes);
                return File(stream, document.ContentType, document.FileName);
            }

            // Otherwise, get from file system
            var fileStream = await _storageService.GetDocumentAsync(document.FilePath);
            return File(fileStream, document.ContentType, document.FileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound("Document file not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", id);
            return StatusCode(500, "Error downloading document");
        }
    }

    /// <summary>
    /// Delete a document
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var document = await _context.UserDocuments
                .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

            if (document == null)
                return NotFound();

            // User can only delete their own documents
            if (document.UserId != userId)
                return Forbid();

            // Soft delete
            document.IsDeleted = true;
            await _context.SaveChangesAsync();

            // Update verification status
            await _verificationService.UpdateUserVerificationStatusAsync(userId);

            _logger.LogInformation("Document deleted: {DocumentId} by user {UserId}", id, userId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", id);
            return StatusCode(500, "Error deleting document");
        }
    }

    /// <summary>
    /// Get current user's verification status
    /// </summary>
    [HttpGet("verification-status")]
    public async Task<ActionResult<UserVerificationStatusDto>> GetVerificationStatus()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var status = await _verificationService.GetVerificationStatusAsync(userId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving verification status");
            return StatusCode(500, "Error retrieving verification status");
        }
    }
}
