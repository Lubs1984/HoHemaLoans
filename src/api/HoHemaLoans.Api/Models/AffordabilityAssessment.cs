using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoHemaLoans.Api.Models;

public class AffordabilityAssessment
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    // Income Summary
    [Column(TypeName = "decimal(18,2)")]
    public decimal GrossMonthlyIncome { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal NetMonthlyIncome { get; set; }
    
    // Expense Summary
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalMonthlyExpenses { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal EssentialExpenses { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal NonEssentialExpenses { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal ExistingDebtPayments { get; set; } // Total monthly debt obligations (loans + debt expenses)
    
    // Affordability Metrics
    [Column(TypeName = "decimal(5,2)")]
    public decimal DebtToIncomeRatio { get; set; } // Debt / Gross Income - Should be < 0.35 per NCA
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal AvailableFunds { get; set; } // Net Income - Expenses
    
    [Column(TypeName = "decimal(5,4)")]
    public decimal ExpenseToIncomeRatio { get; set; } // Total Expenses / Gross Income
    
    // Assessment Result
    [StringLength(50)]
    public string AffordabilityStatus { get; set; } = string.Empty; // Affordable, LimitedAffordability, NotAffordable
    
    [StringLength(500)]
    public string AssessmentNotes { get; set; } = string.Empty;
    
    // Maximum Loan Amount Based on Affordability
    [Column(TypeName = "decimal(18,2)")]
    public decimal MaxRecommendedLoanAmount { get; set; }
    
    // Assessment Details
    [StringLength(50)]
    public string AssessmentMethod { get; set; } = "NCR_Compliant"; // Assessment methodology used
    
    public DateTime AssessmentDate { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiryDate { get; set; } // Assessment valid for 30 days
    
    // Navigation properties
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;
}
