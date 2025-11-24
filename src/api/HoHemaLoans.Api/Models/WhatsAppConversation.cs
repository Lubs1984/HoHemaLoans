using System.ComponentModel.DataAnnotations;

namespace HoHemaLoans.Api.Models;

public class WhatsAppConversation
{
    public int Id { get; set; }

    [Required]
    public int ContactId { get; set; }
    public virtual WhatsAppContact Contact { get; set; } = null!;

    [StringLength(200)]
    public string? Subject { get; set; }

    public ConversationStatus Status { get; set; } = ConversationStatus.Open;

    public ConversationType Type { get; set; } = ConversationType.General;

    // Link to loan application if conversation is about a specific loan
    public Guid? LoanApplicationId { get; set; }
    public virtual LoanApplication? LoanApplication { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    // Navigation properties
    public virtual ICollection<WhatsAppMessage> Messages { get; set; } = new List<WhatsAppMessage>();
}

public enum ConversationStatus
{
    Open,
    InProgress,
    Resolved,
    Closed
}

public enum ConversationType
{
    General,
    LoanInquiry,
    LoanApplication,
    LoanSupport,
    PaymentReminder,
    Marketing
}