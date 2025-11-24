using System.ComponentModel.DataAnnotations;

namespace HoHemaLoans.Api.Models;

public class WhatsAppContact
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DisplayName { get; set; }

    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<WhatsAppConversation> Conversations { get; set; } = new List<WhatsAppConversation>();
    public virtual ICollection<WhatsAppMessage> Messages { get; set; } = new List<WhatsAppMessage>();

    // Link to application user if they register
    public string? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }
}