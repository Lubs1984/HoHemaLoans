using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;
using System.Security.Claims;

namespace HoHemaLoans.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WhatsAppController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WhatsAppController> _logger;
    private readonly IWhatsAppService _whatsAppService;

    public WhatsAppController(ApplicationDbContext context, ILogger<WhatsAppController> logger, IWhatsAppService whatsAppService)
    {
        _context = context;
        _logger = logger;
        _whatsAppService = whatsAppService;
    }

    // GET: api/WhatsApp/contacts
    [HttpGet("contacts")]
    public async Task<ActionResult<IEnumerable<object>>> GetContacts()
    {
        var contacts = await _context.WhatsAppContacts
            .Where(c => c.IsActive)
            .Select(c => new
            {
                c.Id,
                c.PhoneNumber,
                c.DisplayName,
                c.FirstName,
                c.LastName,
                c.CreatedAt,
                HasUser = c.UserId != null,
                MessageCount = c.Messages.Count(),
                ConversationCount = c.Conversations.Count(),
                LastMessageDate = c.Messages.OrderByDescending(m => m.CreatedAt)
                                           .Select(m => m.CreatedAt)
                                           .FirstOrDefault()
            })
            .OrderByDescending(c => c.LastMessageDate)
            .ToListAsync();

        return Ok(contacts);
    }

    // GET: api/WhatsApp/contacts/{phoneNumber}
    [HttpGet("contacts/{phoneNumber}")]
    public async Task<ActionResult<WhatsAppContact>> GetContactByPhone(string phoneNumber)
    {
        var contact = await _context.WhatsAppContacts
            .Include(c => c.User)
            .Include(c => c.Conversations)
            .ThenInclude(conv => conv.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);

        if (contact == null)
        {
            return NotFound($"Contact with phone number {phoneNumber} not found");
        }

        return Ok(contact);
    }

    // POST: api/WhatsApp/contacts
    [HttpPost("contacts")]
    public async Task<ActionResult<WhatsAppContact>> CreateContact(CreateWhatsAppContactRequest request)
    {
        // Check if contact already exists
        var existingContact = await _context.WhatsAppContacts
            .FirstOrDefaultAsync(c => c.PhoneNumber == request.PhoneNumber);

        if (existingContact != null)
        {
            return Conflict($"Contact with phone number {request.PhoneNumber} already exists");
        }

        var contact = new WhatsAppContact
        {
            PhoneNumber = request.PhoneNumber,
            DisplayName = request.DisplayName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.WhatsAppContacts.Add(contact);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created WhatsApp contact for phone number: {PhoneNumber}", request.PhoneNumber);

        return CreatedAtAction(nameof(GetContactByPhone), 
            new { phoneNumber = contact.PhoneNumber }, contact);
    }

    // GET: api/WhatsApp/conversations
    [HttpGet("conversations")]
    public async Task<ActionResult<IEnumerable<object>>> GetConversations(
        [FromQuery] ConversationStatus? status = null,
        [FromQuery] ConversationType? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.WhatsAppConversations
            .Include(c => c.Contact)
            .Include(c => c.LoanApplication)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (type.HasValue)
            query = query.Where(c => c.Type == type.Value);

        var conversations = await query
            .Select(c => new
            {
                c.Id,
                c.Subject,
                c.Status,
                c.Type,
                c.CreatedAt,
                c.UpdatedAt,
                c.ClosedAt,
                Contact = new
                {
                    c.Contact.Id,
                    c.Contact.PhoneNumber,
                    c.Contact.DisplayName
                },
                LoanApplication = c.LoanApplication != null ? new
                {
                    c.LoanApplication.Id,
                    c.LoanApplication.Amount,
                    c.LoanApplication.Status
                } : null,
                MessageCount = c.Messages.Count(),
                LastMessage = c.Messages.OrderByDescending(m => m.CreatedAt)
                                      .Select(m => new
                                      {
                                          m.MessageText,
                                          m.CreatedAt,
                                          m.Direction
                                      })
                                      .FirstOrDefault()
            })
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(conversations);
    }

    // POST: api/WhatsApp/conversations
    [HttpPost("conversations")]
    public async Task<ActionResult<WhatsAppConversation>> CreateConversation(CreateWhatsAppConversationRequest request)
    {
        var contact = await _context.WhatsAppContacts
            .FirstOrDefaultAsync(c => c.PhoneNumber == request.PhoneNumber);

        if (contact == null)
        {
            return BadRequest($"Contact with phone number {request.PhoneNumber} not found. Create contact first.");
        }

        var conversation = new WhatsAppConversation
        {
            ContactId = contact.Id,
            Subject = request.Subject,
            Type = request.Type,
            Status = ConversationStatus.Open,
            LoanApplicationId = request.LoanApplicationId,
            CreatedAt = DateTime.UtcNow
        };

        _context.WhatsAppConversations.Add(conversation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created WhatsApp conversation {ConversationId} for contact {PhoneNumber}", 
            conversation.Id, request.PhoneNumber);

        return CreatedAtAction(nameof(GetConversation), 
            new { conversationId = conversation.Id }, conversation);
    }

    // GET: api/WhatsApp/conversations/{conversationId}
    [HttpGet("conversations/{conversationId}")]
    public async Task<ActionResult<object>> GetConversation(int conversationId)
    {
        var conversation = await _context.WhatsAppConversations
            .Include(c => c.Contact)
            .Include(c => c.LoanApplication)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .ThenInclude(m => m.HandledByUser)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null)
        {
            return NotFound($"Conversation with ID {conversationId} not found");
        }

        var result = new
        {
            conversation.Id,
            conversation.Subject,
            conversation.Status,
            conversation.Type,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            conversation.ClosedAt,
            Contact = new
            {
                conversation.Contact.Id,
                conversation.Contact.PhoneNumber,
                conversation.Contact.DisplayName,
                conversation.Contact.FirstName,
                conversation.Contact.LastName
            },
            LoanApplication = conversation.LoanApplication != null ? new
            {
                conversation.LoanApplication.Id,
                conversation.LoanApplication.Amount,
                conversation.LoanApplication.Status,
                conversation.LoanApplication.ApplicationDate
            } : null,
            Messages = conversation.Messages.Select(m => new
            {
                m.Id,
                m.MessageText,
                m.Type,
                m.Direction,
                m.Status,
                m.CreatedAt,
                m.DeliveredAt,
                m.ReadAt,
                HandledBy = m.HandledByUser != null ? new
                {
                    m.HandledByUser.Id,
                    Name = $"{m.HandledByUser.FirstName} {m.HandledByUser.LastName}",
                    m.HandledByUser.Email
                } : null
            })
        };

        return Ok(result);
    }

    // POST: api/WhatsApp/messages
    [HttpPost("messages")]
    public async Task<ActionResult<WhatsAppMessage>> SendMessage(SendWhatsAppMessageRequest request)
    {
        var conversation = await _context.WhatsAppConversations
            .Include(c => c.Contact)
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId);

        if (conversation == null)
        {
            return BadRequest($"Conversation with ID {request.ConversationId} not found");
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var message = new WhatsAppMessage
        {
            ConversationId = request.ConversationId,
            ContactId = conversation.ContactId,
            MessageText = request.MessageText,
            Type = request.Type ?? MessageType.Text,
            Direction = MessageDirection.Outbound,
            Status = MessageStatus.Sent,
            CreatedAt = DateTime.UtcNow,
            HandledByUserId = userId
        };

        _context.WhatsAppMessages.Add(message);

        // Update conversation timestamp
        conversation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Actually send via WhatsApp API
        var sent = await _whatsAppService.SendMessageAsync(conversation.Contact.PhoneNumber, request.MessageText);
        if (sent)
        {
            message.Status = MessageStatus.Sent;
            await _context.SaveChangesAsync();
            _logger.LogInformation("WhatsApp message delivered to {PhoneNumber}", conversation.Contact.PhoneNumber);
        }
        else
        {
            message.Status = MessageStatus.Failed;
            await _context.SaveChangesAsync();
            _logger.LogWarning("Failed to deliver WhatsApp message to {PhoneNumber}", conversation.Contact.PhoneNumber);
        }

        _logger.LogInformation("Processed outbound WhatsApp message in conversation {ConversationId} to {PhoneNumber}", 
            request.ConversationId, conversation.Contact.PhoneNumber);

        return CreatedAtAction(nameof(GetConversation), 
            new { conversationId = conversation.Id }, new { message.Id, message.MessageText, message.Status, message.Direction, message.CreatedAt });
    }

    // POST: api/WhatsApp/send-direct
    /// <summary>
    /// Send a message directly to a phone number — creates contact and conversation if they don't exist.
    /// </summary>
    [HttpPost("send-direct")]
    public async Task<ActionResult<object>> SendDirect(SendDirectMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.MessageText))
            return BadRequest("PhoneNumber and MessageText are required");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Get or create contact
        var contact = await _context.WhatsAppContacts
            .FirstOrDefaultAsync(c => c.PhoneNumber == request.PhoneNumber);

        if (contact == null)
        {
            contact = new WhatsAppContact
            {
                PhoneNumber = request.PhoneNumber,
                DisplayName = request.DisplayName ?? request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.WhatsAppContacts.Add(contact);
            await _context.SaveChangesAsync();
        }

        // Get or create open conversation
        var conversation = await _context.WhatsAppConversations
            .FirstOrDefaultAsync(c => c.ContactId == contact.Id && c.Status == ConversationStatus.Open);

        if (conversation == null)
        {
            conversation = new WhatsAppConversation
            {
                ContactId = contact.Id,
                Subject = request.Subject ?? "Admin initiated conversation",
                Type = ConversationType.General,
                Status = ConversationStatus.Open,
                CreatedAt = DateTime.UtcNow
            };
            _context.WhatsAppConversations.Add(conversation);
            await _context.SaveChangesAsync();
        }

        // Create message record
        var message = new WhatsAppMessage
        {
            ConversationId = conversation.Id,
            ContactId = contact.Id,
            MessageText = request.MessageText,
            Type = MessageType.Text,
            Direction = MessageDirection.Outbound,
            Status = MessageStatus.Sent,
            CreatedAt = DateTime.UtcNow,
            HandledByUserId = userId
        };

        _context.WhatsAppMessages.Add(message);
        conversation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Send via WhatsApp API
        var sent = await _whatsAppService.SendMessageAsync(contact.PhoneNumber, request.MessageText);
        message.Status = sent ? MessageStatus.Sent : MessageStatus.Failed;
        await _context.SaveChangesAsync();

        _logger.LogInformation("SendDirect: {Status} to {PhoneNumber}", message.Status, contact.PhoneNumber);

        return Ok(new { conversationId = conversation.Id, messageId = message.Id, status = message.Status.ToString(), sent });
    }

    // PUT: api/WhatsApp/conversations/{conversationId}/status
    [HttpPut("conversations/{conversationId}/status")]
    public async Task<IActionResult> UpdateConversationStatus(int conversationId, UpdateConversationStatusRequest request)
    {
        var conversation = await _context.WhatsAppConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null)
        {
            return NotFound($"Conversation with ID {conversationId} not found");
        }

        conversation.Status = request.Status;
        conversation.UpdatedAt = DateTime.UtcNow;

        if (request.Status == ConversationStatus.Closed)
        {
            conversation.ClosedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated conversation {ConversationId} status to {Status}", 
            conversationId, request.Status);

        return Ok(new { message = $"Conversation status updated to {request.Status}" });
    }
}

// Request DTOs
public class CreateWhatsAppContactRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class CreateWhatsAppConversationRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public ConversationType Type { get; set; } = ConversationType.General;
    public Guid? LoanApplicationId { get; set; }
}

public class SendWhatsAppMessageRequest
{
    public int ConversationId { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public MessageType? Type { get; set; }
}

public class SendDirectMessageRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Subject { get; set; }
}

public class UpdateConversationStatusRequest
{
    public ConversationStatus Status { get; set; }
}
// Helper method for processing RESUME commands from WhatsApp
// This would be called from the webhook handler when a message like "RESUME {applicationId}" is received
// For now, this is a placeholder - full WhatsApp webhook integration requires Meta's Business API setup
public class WhatsAppResumeHelper
{
    // Extract application ID from message text like "RESUME abc-123-def"
    public static string? ExtractApplicationId(string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText)) return null;
        
        var parts = messageText.Trim().Split(' ');
        if (parts.Length >= 2 && parts[0].Equals("RESUME", StringComparison.OrdinalIgnoreCase))
        {
            return parts[1];
        }
        
        return null;
    }
}
