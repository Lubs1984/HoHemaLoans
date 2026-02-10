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
    
    // Address fields (South African format)
    [StringLength(200)]
    public string StreetAddress { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string Suburb { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string City { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string Province { get; set; } = string.Empty; // Gauteng, Western Cape, etc.
    
    [StringLength(10)]
    public string PostalCode { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string Address { get; set; } = string.Empty; // Legacy field, keep for compatibility
    
    // Employment Information (NCR Required)
    [StringLength(200)]
    public string EmployerName { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string EmployeeNumber { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string PayrollReference { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string EmploymentType { get; set; } = string.Empty; // Permanent, Contract, Self-Employed, Unemployed
    
    public decimal MonthlyIncome { get; set; }
    
    // Banking Information (NCR Required for disbursement)
    [StringLength(100)]
    public string BankName { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string AccountType { get; set; } = string.Empty; // Cheque, Savings
    
    [StringLength(50)]
    public string AccountNumber { get; set; } = string.Empty;
    
    [StringLength(10)]
    public string BranchCode { get; set; } = string.Empty;
    
    // Next of Kin (NCR Recommended)
    [StringLength(100)]
    public string NextOfKinName { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string NextOfKinRelationship { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string NextOfKinPhone { get; set; } = string.Empty;
    
    public bool IsVerified { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Business / Employer relationship
    public Guid? BusinessId { get; set; }
    public virtual Business? Business { get; set; }

    // Navigation properties
    public virtual ICollection<LoanApplication> LoanApplications { get; set; } = new List<LoanApplication>();
    
    // Income and Expense related navigation properties
    public virtual ICollection<Income> Incomes { get; set; } = new List<Income>();
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public virtual ICollection<AffordabilityAssessment> AffordabilityAssessments { get; set; } = new List<AffordabilityAssessment>();
    
    // WhatsApp related navigation properties
    public virtual WhatsAppContact? WhatsAppContact { get; set; }
    public virtual ICollection<WhatsAppMessage> HandledMessages { get; set; } = new List<WhatsAppMessage>();
    public virtual ICollection<WhatsAppSession> WhatsAppSessions { get; set; } = new List<WhatsAppSession>();
}