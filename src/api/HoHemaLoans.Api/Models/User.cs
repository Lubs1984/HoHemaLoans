using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HoHemaLoans.Api.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(13)]
    public string IdNumber { get; set; } = string.Empty;
    
    public DateTime DateOfBirth { get; set; }
    
    [StringLength(100)]
    public string Address { get; set; } = string.Empty;
    
    public decimal MonthlyIncome { get; set; }
    
    public bool IsVerified { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<LoanApplication> LoanApplications { get; set; } = new List<LoanApplication>();
    
    // WhatsApp related navigation properties
    public virtual WhatsAppContact? WhatsAppContact { get; set; }
    public virtual ICollection<WhatsAppMessage> HandledMessages { get; set; } = new List<WhatsAppMessage>();
}