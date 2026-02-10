using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWhatsAppLoanWizardService _wizardService;

    public WhatsAppWebhookController(
        IWhatsAppService whatsAppService,
        ApplicationDbContext context,
        ILogger<WhatsAppWebhookController> logger,
        UserManager<ApplicationUser> userManager,
        IWhatsAppLoanWizardService wizardService)
    {
        _whatsAppService = whatsAppService;
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _wizardService = wizardService;
    }

    /// <summary>
    /// GET endpoint for webhook verification with Meta
    /// Meta sends: hub.mode, hub.verify_token, hub.challenge
    /// </summary>
    [HttpGet("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? hubMode,
        [FromQuery(Name = "hub.verify_token")] string? hubVerifyToken,
        [FromQuery(Name = "hub.challenge")] string? hubChallenge,
        [FromQuery] string? mode,
        [FromQuery] string? token,
        [FromQuery] string? challenge)
    {
        // Support both Meta's format (hub.*) and simple format for testing
        var actualMode = hubMode ?? mode;
        var actualToken = hubVerifyToken ?? token;
        var actualChallenge = hubChallenge ?? challenge;

        _logger.LogInformation("Webhook verification request. Mode: {Mode}, Token present: {TokenPresent}",
            actualMode, !string.IsNullOrEmpty(actualToken));

        if (string.IsNullOrEmpty(actualMode) || string.IsNullOrEmpty(actualToken) || string.IsNullOrEmpty(actualChallenge))
        {
            _logger.LogWarning("Missing required webhook verification parameters");
            return BadRequest("Missing required parameters");
        }

        var isValid = await _whatsAppService.VerifyWebhookAsync(actualMode, actualToken, actualChallenge);

        if (isValid)
        {
            _logger.LogInformation("Webhook verification successful, returning challenge");
            return Ok(actualChallenge);
        }

        _logger.LogWarning("Webhook verification failed");
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
            _logger.LogInformation("========== WhatsApp Webhook POST Received ==========");
            _logger.LogInformation("Request ContentType: {ContentType}", Request.ContentType);
            _logger.LogInformation("Request Headers: {Headers}", string.Join(", ", Request.Headers.Keys));

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
            _logger.LogInformation("Parsing webhook payload...");
            var payload = await _whatsAppService.ParseWebhookAsync(Request.Body);

            if (payload == null || payload.Entry == null || payload.Entry.Count == 0)
            {
                _logger.LogWarning("Empty or invalid webhook payload");
                return Ok(new { status = "received", message = "Empty payload" }); // Return OK to acknowledge receipt
            }

            _logger.LogInformation("Webhook payload parsed successfully. Entries: {Count}", payload.Entry.Count);

            // Process each entry in the webhook
            var messagesProcessed = 0;
            var statusesProcessed = 0;

            foreach (var entry in payload.Entry)
            {
                if (entry.Changes == null) continue;

                foreach (var change in entry.Changes)
                {
                    if (change.Value == null) continue;

                    // Process incoming messages
                    if (change.Value.Messages != null)
                    {
                        _logger.LogInformation("Processing {Count} incoming messages", change.Value.Messages.Count);
                        await ProcessIncomingMessagesAsync(change.Value);
                        messagesProcessed += change.Value.Messages.Count;
                    }

                    // Process message status updates (delivered, read, failed, etc.)
                    if (change.Value.Statuses != null)
                    {
                        _logger.LogInformation("Processing {Count} status updates", change.Value.Statuses.Count);
                        await ProcessMessageStatusesAsync(change.Value);
                        statusesProcessed += change.Value.Statuses.Count;
                    }
                }
            }

            _logger.LogInformation("Webhook processing complete. Messages: {Messages}, Statuses: {Statuses}", 
                messagesProcessed, statusesProcessed);

            return Ok(new { 
                status = "received", 
                messagesProcessed, 
                statusesProcessed 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
            
            // Return 200 OK even on error to prevent Meta from retrying
            // Log the error but acknowledge receipt
            return Ok(new { 
                status = "error", 
                message = "Error processing webhook, but acknowledged",
                error = ex.Message 
            });
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

                _logger.LogInformation("Creating WhatsApp message record: From={From}, Type={Type}, Text={Text}",
                    phoneNumber, messageType, messageText.Length > 100 ? messageText.Substring(0, 100) + "..." : messageText);

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
                    "✅ SUCCESS: Saved message to database. MessageId={MessageId}, From={PhoneNumber}, ConversationId={ConversationId}, ContactId={ContactId}",
                    whatsAppMessage.Id, phoneNumber, conversation.Id, contact.Id);
                
                _logger.LogInformation(
                    "Message details - Type: {MessageType}, Direction: {Direction}, Status: {Status}, Text: {Content}",
                    messageType, whatsAppMessage.Direction, whatsAppMessage.Status, messageText);

                // Handle document/media uploads
                if (messageType == MessageType.Document || messageType == MessageType.Image)
                {
                    await HandleDocumentUploadAsync(phoneNumber, message, contact);
                }

                // Check if user exists and send personalized response
                await HandleUserRecognitionAndLoanFlowAsync(phoneNumber, contact, conversation, messageText);
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

    /// <summary>
    /// Handle user recognition and loan application flow initiation
    /// </summary>
    private async Task HandleUserRecognitionAndLoanFlowAsync(
        string phoneNumber, 
        WhatsAppContact contact, 
        WhatsAppConversation conversation,
        string messageText)
    {
        try
        {
            // ============================================================
            // DEVELOPMENT MODE: Send a simple "in development" message
            // Remove this block when ready to go live with full loan flows
            // ============================================================
            _logger.LogInformation("📱 Development mode: Sending auto-reply to {PhoneNumber}. Message received: \"{MessageText}\"", 
                phoneNumber, messageText);

            await _whatsAppService.SendMessageAsync(
                phoneNumber,
                "Hi, we are currently in development. 🚧\n\n" +
                "Thank you for your interest in Ho Hema Loans! We're working hard to bring you a seamless lending experience.\n\n" +
                "We'll notify you when our services are live. Stay tuned! 🙏"
            );

            _logger.LogInformation("✅ Development auto-reply sent to {PhoneNumber}", phoneNumber);
            return;
            // ============================================================
            // END DEVELOPMENT MODE
            // ============================================================
            
            _logger.LogInformation("Checking for registered user with phone number: {PhoneNumber}", phoneNumber);

            // Clean phone number for comparison
            var cleanPhoneNumber = phoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "");

            // Check if the phone number matches any registered user
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber != null && 
                    u.PhoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "") == cleanPhoneNumber);

            if (user == null)
            {
                _logger.LogInformation("No registered user found for phone number: {PhoneNumber}", phoneNumber);
                
                // Send welcome message for unregistered users
                await _whatsAppService.SendMessageAsync(
                    phoneNumber,
                    "Welcome to Ho Hema Loans! 👋\n\n" +
                    "To apply for a loan, please register on our website first: https://hohemaloans.com\n\n" +
                    "Or reply with REGISTER to start the registration process via WhatsApp."
                );
                return;
            }

            _logger.LogInformation("✅ Found registered user: {FirstName} {LastName} (ID: {UserId})", 
                user.FirstName, user.LastName, user.Id);

            // Get or create active WhatsApp session
            var session = await _context.WhatsAppSessions
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.SessionStatus == "Active");

            if (session == null)
            {
                session = new WhatsAppSession
                {
                    PhoneNumber = phoneNumber,
                    UserId = user.Id,
                    SessionStatus = "Active",
                    CreatedAt = DateTime.UtcNow
                };
                _context.WhatsAppSessions.Add(session);
                await _context.SaveChangesAsync();

                // Send personalized greeting on first message
                await _whatsAppService.SendMessageAsync(phoneNumber, $"Hi {user.FirstName}! 👋");

                // Check for existing loan applications
                await CheckExistingApplicationsAsync(phoneNumber, user.Id, session);
            }
            else
            {
                // Existing session - process the message through wizard
                session.LastUpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Process user response through the wizard
                await _wizardService.ProcessUserResponseAsync(phoneNumber, user.Id, messageText, session);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling user recognition and loan flow for phone number: {PhoneNumber}", phoneNumber);
        }
    }

    /// <summary>
    /// Check for existing loan applications and guide user
    /// </summary>
    private async Task CheckExistingApplicationsAsync(string phoneNumber, string userId, WhatsAppSession session)
    {
        try
        {
            // Check for active/pending loan applications
            var activeLoanApplication = await _context.LoanApplications
                .Where(la => la.UserId == userId && 
                    (la.Status == LoanStatus.Draft || 
                     la.Status == LoanStatus.Pending || 
                     la.Status == LoanStatus.UnderReview))
                .OrderByDescending(la => la.ApplicationDate)
                .FirstOrDefaultAsync();

            if (activeLoanApplication != null)
            {
                _logger.LogInformation("Found active loan application for user {UserId}: {ApplicationId} (Status: {Status})", 
                    userId, activeLoanApplication.Id, activeLoanApplication.Status);

                // Inform user about existing application
                var statusMessage = activeLoanApplication.Status switch
                {
                    LoanStatus.Draft => $"You have an incomplete loan application. Would you like to continue where you left off?\n\n" +
                                       $"Application Amount: R{activeLoanApplication.Amount:N2}\n" +
                                       $"Current Step: {activeLoanApplication.CurrentStep + 1}/7\n\n" +
                                       $"Reply YES to continue or NEW to start a fresh application.",
                    
                    LoanStatus.Pending => $"Your loan application of R{activeLoanApplication.Amount:N2} is currently pending review.\n\n" +
                                         $"Applied on: {activeLoanApplication.ApplicationDate:dd MMM yyyy}\n" +
                                         $"Status: Awaiting approval\n\n" +
                                         $"We'll notify you once it's reviewed. Usually takes 24-48 hours.",
                    
                    LoanStatus.UnderReview => $"Your loan application of R{activeLoanApplication.Amount:N2} is under review.\n\n" +
                                             $"Applied on: {activeLoanApplication.ApplicationDate:dd MMM yyyy}\n" +
                                             $"Status: Being reviewed by our team\n\n" +
                                             $"We'll contact you soon with an update!",
                    
                    _ => "You have an active loan application. Please check your account for details."
                };

                await _whatsAppService.SendMessageAsync(phoneNumber, statusMessage);

                // Link the session to the existing application
                if (activeLoanApplication.Status == LoanStatus.Draft)
                {
                    session.DraftApplicationId = activeLoanApplication.Id;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Linked session to existing draft application {ApplicationId}", activeLoanApplication.Id);
                }
            }
            else
            {
                _logger.LogInformation("No active loan application found for user {UserId}. Initiating new application flow.", userId);

                // No active application - offer to start a new one
                await _whatsAppService.SendMessageAsync(
                    phoneNumber,
                    "I see you don't have any active loan applications. Would you like to apply for a loan? 💰\n\n" +
                    "Reply YES to start your application via WhatsApp, or visit our website for more options.\n\n" +
                    "Ho Hema Loans - Quick, simple, and transparent! ✨"
                );

                _logger.LogInformation("Ready for loan application for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existing applications for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Handle document uploads (ID, payslips, bank statements, etc.)
    /// </summary>
    private async Task HandleDocumentUploadAsync(string phoneNumber, WebhookMessage message, WhatsAppContact contact)
    {
        try
        {
            string? mediaId = null;
            string? mimeType = null;
            string? caption = message.Image?.Caption ?? message.Document?.Caption;

            if (message.Type == "image" && message.Image != null)
            {
                mediaId = message.Image.Id;
                mimeType = message.Image.MimeType;
            }
            else if (message.Type == "document" && message.Document != null)
            {
                mediaId = message.Document.Id;
                mimeType = message.Document.MimeType;
            }

            if (string.IsNullOrEmpty(mediaId))
            {
                _logger.LogWarning("No media ID found in document message");
                return;
            }

            _logger.LogInformation("Processing document upload. MediaId: {MediaId}, Type: {MimeType}, Caption: {Caption}",
                mediaId, mimeType, caption);

            // Get the media URL
            var mediaUrl = await _whatsAppService.DownloadMediaAsync(mediaId);

            if (string.IsNullOrEmpty(mediaUrl))
            {
                await _whatsAppService.SendMessageAsync(
                    phoneNumber,
                    "❌ Sorry, there was an error processing your document. Please try again."
                );
                return;
            }

            _logger.LogInformation("Document received. MediaUrl: {MediaUrl}", mediaUrl);

            // In a production system, you would:
            // 1. Download the file from mediaUrl
            // 2. Upload to secure cloud storage (Azure Blob, AWS S3)
            // 3. Link to user's profile or loan application
            // 4. Run virus scan and validation
            // 5. Extract text if needed (OCR for ID documents)

            // For now, acknowledge receipt
            var documentType = DetermineDocumentType(caption, mimeType);
            
            await _whatsAppService.SendMessageAsync(
                phoneNumber,
                $"✅ Document received: {documentType}\n\n" +
                $"Your document has been uploaded successfully and will be reviewed by our team.\n\n" +
                $"Reference: {mediaId[..8]}..."
            );

            _logger.LogInformation("Document upload processed for contact {ContactId}. MediaId: {MediaId}, Type: {DocumentType}",
                contact.Id, mediaId, documentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling document upload for phone number: {PhoneNumber}", phoneNumber);
            
            await _whatsAppService.SendMessageAsync(
                phoneNumber,
                "❌ Sorry, there was an error processing your document. Please try again or contact support."
            );
        }
    }

    /// <summary>
    /// Determine document type from caption or mime type
    /// </summary>
    private static string DetermineDocumentType(string? caption, string? mimeType)
    {
        if (!string.IsNullOrEmpty(caption))
        {
            var lowerCaption = caption.ToLower();
            if (lowerCaption.Contains("id") || lowerCaption.Contains("identity"))
                return "ID Document";
            if (lowerCaption.Contains("payslip") || lowerCaption.Contains("salary"))
                return "Payslip";
            if (lowerCaption.Contains("bank") || lowerCaption.Contains("statement"))
                return "Bank Statement";
            if (lowerCaption.Contains("proof") && lowerCaption.Contains("address"))
                return "Proof of Address";
        }

        if (!string.IsNullOrEmpty(mimeType))
        {
            if (mimeType.Contains("image"))
                return "Image Document";
            if (mimeType.Contains("pdf"))
                return "PDF Document";
        }

        return "Supporting Document";
    }
}
