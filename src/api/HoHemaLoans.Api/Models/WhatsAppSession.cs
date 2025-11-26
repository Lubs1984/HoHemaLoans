using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoHemaLoans.Api.Models;

public class WhatsAppSession
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public Guid? DraftApplicationId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastUpdatedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    [StringLength(20)]
    public string SessionStatus { get; set; } = "Active";
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
    
    [ForeignKey("DraftApplicationId")]
    public virtual LoanApplication? DraftApplication { get; set; }
}
