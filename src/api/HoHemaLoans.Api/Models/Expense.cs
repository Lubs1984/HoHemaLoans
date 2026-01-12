using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoHemaLoans.Api.Models;

public class Expense
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty; 
    // Housing, Utilities, Transport, Food, Debt, Communication, Insurance, Dependents, Medical, Personal, Other
    
    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlyAmount { get; set; }
    
    [StringLength(50)]
    public string? Frequency { get; set; } = "Monthly"; // Weekly, Bi-weekly, Monthly, Annual
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    public bool IsEssential { get; set; } = false; // Essential vs Non-essential per NCA guidelines
    
    public bool IsFixed { get; set; } = false; // Fixed vs Variable expenses
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
}
