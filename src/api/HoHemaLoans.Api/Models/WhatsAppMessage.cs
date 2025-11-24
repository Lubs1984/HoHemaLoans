using System.ComponentModel.DataAnnotations;

namespace HoHemaLoans.Api.Models;

public class WhatsAppMessage
{
    public int Id { get; set; }

    [Required]
    public int ConversationId { get; set; }
    public virtual WhatsAppConversation Conversation { get; set; } = null!;

    [Required]
    public int ContactId { get; set; }
    public virtual WhatsAppContact Contact { get; set; } = null!;

    // WhatsApp message ID for tracking
    [StringLength(100)]
    public string? WhatsAppMessageId { get; set; }

    [Required]
    public string MessageText { get; set; } = string.Empty;

    public MessageType Type { get; set; } = MessageType.Text;

    public MessageDirection Direction { get; set; } = MessageDirection.Inbound;

    public MessageStatus Status { get; set; } = MessageStatus.Received;

    // Media/attachment information
    [StringLength(500)]
    public string? MediaUrl { get; set; }

    [StringLength(100)]
    public string? MediaType { get; set; }

    [StringLength(200)]
    public string? MediaCaption { get; set; }

    // Message metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeliveredAt { get; set; }

    public DateTime? ReadAt { get; set; }

    // Error information for failed messages
    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    public int? RetryCount { get; set; } = 0;

    // Template message information
    [StringLength(100)]
    public string? TemplateName { get; set; }

    public string? TemplateParameters { get; set; } // JSON string

    // Agent who handled the message (if any)
    public string? HandledByUserId { get; set; }
    public virtual ApplicationUser? HandledByUser { get; set; }
}

public enum MessageType
{
    Text,
    Image,
    Document,
    Audio,
    Video,
    Location,
    Contact,
    Template,
    Interactive,
    Reaction,
    System
}

public enum MessageDirection
{
    Inbound,  // From customer to business
    Outbound  // From business to customer
}

public enum MessageStatus
{
    Received,
    Sent,
    Delivered,
    Read,
    Failed
}