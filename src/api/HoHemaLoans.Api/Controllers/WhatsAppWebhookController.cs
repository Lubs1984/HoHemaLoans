using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;
using HoHemaLoans.Api.Services;

namespace HoHemaLoans.Api.Controllers;

/// <summary>
/// Webhook controller for receiving and processing WhatsApp messages from Meta
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IWhatsAppService whatsAppService,
        ApplicationDbContext context,
        ILogger<WhatsAppWebhookController> logger)
    {
        _whatsAppService = whatsAppService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET endpoint for webhook verification with Meta
    /// </summary>
    [HttpGet("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyWebhook(
        [FromQuery] string mode,
        [FromQuery] string token,
        [FromQuery] string challenge)
    {
        _logger.LogInformation("Webhook verification request. Mode: {Mode}, Token present: {TokenPresent}",
            mode, !string.IsNullOrEmpty(token));

        var isValid = await _whatsAppService.VerifyWebhookAsync(mode, token, challenge);

        if (isValid)
        {
            return Ok(challenge);
        }

        return Unauthorized("Webhook verification failed");
    }

    /// <summary>
    /// POST endpoint for receiving messages and status updates from Meta
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveWebhook()
    {
        try
        {
            // Read and verify signature (optional but recommended)
            var signature = Request.Headers["X-Hub-Signature-256"].ToString();
            if (!string.IsNullOrEmpty(signature))
            {
                if (!await VerifySignatureAsync(signature))
                {
                    _logger.LogWarning("Webhook signature verification failed");
                    return Unauthorized("Invalid signature");
                }
            }

            // Parse the webhook payload
            var payload = await _whatsAppService.ParseWebhookAsync(Request.Body);

            if (payload == null || payload.Entry == null || payload.Entry.Count == 0)
            {
                _logger.LogWarning("Empty or invalid webhook payload");
                return Ok(); // Return OK anyway to acknowledge receipt
            }

            // Process each entry in the webhook
            foreach (var entry in payload.Entry)
            {
                if (entry.Changes == null) continue;

                foreach (var change in entry.Changes)
                {
                    if (change.Value == null) continue;

                    // Process incoming messages
                    if (change.Value.Messages != null)
                    {
                        await ProcessIncomingMessagesAsync(change.Value);
                    }

                    // Process message status updates (delivered, read, failed, etc.)
                    if (change.Value.Statuses != null)
                    {
                        await ProcessMessageStatusesAsync(change.Value);
                    }
                }
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Process incoming messages from customers
    /// </summary>
    private async Task ProcessIncomingMessagesAsync(WebhookValue value)
    {
        if (value.Metadata == null || value.Messages == null)
            return;

        var metadata = value.Metadata;
        var senderInfo = value.Contacts?.FirstOrDefault();

        foreach (var message in value.Messages)
        {
            try
            {
                var phoneNumber = message.From ?? string.Empty;
                var messageId = message.Id ?? Guid.NewGuid().ToString();
                var timestamp = long.TryParse(message.Timestamp, out var ts)
                    ? UnixTimeStampToDateTime(ts)
                    : DateTime.UtcNow;

                // Get or create contact
                var contact = await GetOrCreateContactAsync(phoneNumber, senderInfo?.Profile?.Name);

                // Get or create conversation (or link to existing loan application)
                var conversation = await GetOrCreateConversationAsync(contact.Id);

                // Determine message type and content
                var messageType = message.Type switch
                {
                    "text" => MessageType.Text,
                    "image" => MessageType.Image,
                    "document" => MessageType.Document,
                    "audio" => MessageType.Audio,
                    "video" => MessageType.Video,
                    "button" => MessageType.Template,
                    "interactive" => MessageType.Interactive,
                    _ => MessageType.Text
                };

                var messageText = message.Type switch
                {
                    "text" => message.Text?.Body ?? string.Empty,
                    "button" => message.Button?.Text ?? "[Button pressed]",
                    "interactive" => GetInteractiveMessageText(message.Interactive),
                    _ => $"[{message.Type} message]"
                };

                // Create and store the message
                var whatsAppMessage = new WhatsAppMessage
                {
                    ConversationId = conversation.Id,
                    ContactId = contact.Id,
                    WhatsAppMessageId = messageId,
                    MessageText = messageText,
                    Type = messageType,
                    Direction = MessageDirection.Inbound,
                    Status = MessageStatus.Received,
                    CreatedAt = timestamp,
                    MediaUrl = GetMediaUrl(message),
                    MediaType = message.Type,
                    MediaCaption = GetMediaCaption(message)
                };

                _context.WhatsAppMessages.Add(whatsAppMessage);

                // Update conversation timestamp
                conversation.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Processed incoming message from {PhoneNumber}. Type: {MessageType}, Content: {Content}",
                    phoneNumber, messageType, messageText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing individual message");
            }
        }
    }

    /// <summary>
    /// Process message status updates (sent, delivered, read, failed)
    /// </summary>
    private async Task ProcessMessageStatusesAsync(WebhookValue value)
    {
        if (value.Statuses == null)
            return;

        foreach (var status in value.Statuses)
        {
            try
            {
                var messageId = status.Id;
                var statusValue = status.Status;

                // Find the message in database
                var message = await _context.WhatsAppMessages
                    .FirstOrDefaultAsync(m => m.WhatsAppMessageId == messageId);

                if (message == null)
                {
                    _logger.LogWarning("Message {MessageId} not found for status update: {Status}",
                        messageId, statusValue);
                    continue;
                }

                // Update message status
                message.Status = statusValue switch
                {
                    "sent" => MessageStatus.Sent,
                    "delivered" => MessageStatus.Delivered,
                    "read" => MessageStatus.Read,
                    "failed" => MessageStatus.Failed,
                    _ => message.Status
                };

                // Update timestamps
                var timestamp = long.TryParse(status.Timestamp, out var ts)
                    ? UnixTimeStampToDateTime(ts)
                    : DateTime.UtcNow;

                if (statusValue == "delivered")
                    message.DeliveredAt = timestamp;
                else if (statusValue == "read")
                    message.ReadAt = timestamp;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated message {MessageId} status to {Status}",
                    messageId, statusValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message status update");
            }
        }
    }

    /// <summary>
    /// Get or create a WhatsApp contact from the database
    /// </summary>
    private async Task<WhatsAppContact> GetOrCreateContactAsync(string phoneNumber, string? displayName)
    {
        var contact = await _context.WhatsAppContacts
            .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);

        if (contact != null)
            return contact;

        // Create new contact
        contact = new WhatsAppContact
        {
            PhoneNumber = phoneNumber,
            DisplayName = displayName ?? phoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.WhatsAppContacts.Add(contact);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new contact for phone number: {PhoneNumber}", phoneNumber);

        return contact;
    }

    /// <summary>
    /// Get or create a conversation for the contact
    /// </summary>
    private async Task<WhatsAppConversation> GetOrCreateConversationAsync(int contactId)
    {
        // Look for an open conversation with this contact
        var conversation = await _context.WhatsAppConversations
            .FirstOrDefaultAsync(c =>
                c.ContactId == contactId &&
                c.Status == ConversationStatus.Open);

        if (conversation != null)
            return conversation;

        // Create new conversation
        conversation = new WhatsAppConversation
        {
            ContactId = contactId,
            Subject = "Incoming WhatsApp Conversation",
            Type = ConversationType.General,
            Status = ConversationStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        _context.WhatsAppConversations.Add(conversation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new conversation for contact {ContactId}", contactId);

        return conversation;
    }

    /// <summary>
    /// Verify webhook signature from Meta (optional security measure)
    /// </summary>
    private async Task<bool> VerifySignatureAsync(string signature)
    {
        try
        {
            // Read request body
            Request.Body.Position = 0;
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            // Extract signature parts
            var parts = signature.Split('=');
            if (parts.Length != 2 || parts[0] != "sha256")
                return false;

            var receivedSignature = parts[1];

            // In a real implementation, you would use your app secret here
            // For now, we'll just log it
            _logger.LogInformation("Signature verification requested but not fully implemented");

            return true; // Placeholder - implement with your app secret
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signature");
            return false;
        }
    }

    /// <summary>
    /// Helper methods for extracting data from webhook messages
    /// </summary>
    private static string GetMediaUrl(WebhookMessage message)
    {
        return message.Type switch
        {
            "image" => message.Image?.Id ?? string.Empty,
            "document" => message.Document?.Id ?? string.Empty,
            "audio" => message.Audio?.Id ?? string.Empty,
            "video" => message.Video?.Id ?? string.Empty,
            _ => string.Empty
        };
    }

    private static string GetMediaCaption(WebhookMessage message)
    {
        return message.Type switch
        {
            "image" => message.Image?.Caption ?? string.Empty,
            "document" => message.Document?.Caption ?? string.Empty,
            "video" => message.Video?.Caption ?? string.Empty,
            _ => string.Empty
        };
    }

    private static string GetInteractiveMessageText(WebhookInteractive? interactive)
    {
        if (interactive == null)
            return "[Interactive message]";

        if (interactive.ListReply != null)
            return $"Selected: {interactive.ListReply.Title}";

        if (interactive.ButtonReply != null)
            return $"Clicked: {interactive.ButtonReply.Title}";

        return "[Interactive message]";
    }

    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp);
        return dateTime;
    }
}
