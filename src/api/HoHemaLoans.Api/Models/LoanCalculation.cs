using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoHemaLoans.Api.Models;

/// <summary>
/// Detailed loan calculation with all financial components
/// </summary>
public class LoanCalculation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid LoanApplicationId { get; set; }
    
    [ForeignKey("LoanApplicationId")]
    public virtual LoanApplication? LoanApplication { get; set; }

    // Principal amount
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal LoanAmount { get; set; }

    // Interest rate (annual percentage)
    [Required]
    [Column(TypeName = "decimal(5,4)")]
    public decimal InterestRate { get; set; }

    // Term in months
    [Required]
    public int TermInMonths { get; set; }

    // Fees
    [Column(TypeName = "decimal(10,2)")]
    public decimal InitiationFee { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal MonthlyServiceFee { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal InsuranceFee { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal OtherFees { get; set; }

    // Calculated amounts
    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlyInstallment { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalInterest { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalFees { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmountPayable { get; set; }

    // NCR compliance calculations
    [Column(TypeName = "decimal(5,2)")]
    public decimal EffectiveInterestRate { get; set; } // APR including all costs

    [Column(TypeName = "decimal(5,2)")]
    public decimal DebtServiceRatio { get; set; } // Percentage of income

    [Column(TypeName = "decimal(18,2)")]
    public decimal DisposableIncomeRequired { get; set; }

    // Calculation metadata
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public string CalculatedBy { get; set; } = "System";
    public string? CalculationMethod { get; set; } = "Standard Amortization";
    public string? Notes { get; set; }

    // NCR compliance flags
    public bool IsNCRCompliant { get; set; }
    public string? ComplianceNotes { get; set; }
}

/// <summary>
/// Personal information for NCR compliance
/// </summary>
public class PersonalInformation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    [Required]
    [StringLength(13)]
    public string IdNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    [Required]
    public MaritalStatus MaritalStatus { get; set; }

    public int Dependents { get; set; }

    [StringLength(500)]
    public string HomeAddress { get; set; } = string.Empty;

    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(10)]
    public string PostalCode { get; set; } = string.Empty;

    [StringLength(50)]
    public string Province { get; set; } = string.Empty;

    [StringLength(15)]
    public string? HomePhone { get; set; }

    [StringLength(15)]
    public string? WorkPhone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Financial information for affordability assessment
/// </summary>
public class FinancialInformation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    // Employment details
    [Required]
    public EmploymentStatus EmploymentStatus { get; set; }

    [StringLength(200)]
    public string? EmployerName { get; set; }

    [StringLength(500)]
    public string? EmployerAddress { get; set; }

    [StringLength(100)]
    public string? JobTitle { get; set; }

    public DateTime? EmploymentStartDate { get; set; }

    // Income
    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlyIncome { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? OtherIncome { get; set; }

    [StringLength(200)]
    public string? OtherIncomeSource { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalMonthlyIncome { get; set; }

    // Expenses
    [Column(TypeName = "decimal(10,2)")]
    public decimal MonthlyExpenses { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? ExistingDebtPayments { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? RentMortgagePayment { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? UtilitiesPayment { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? FoodTransportExpenses { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? OtherExpenses { get; set; }

    // Bank details
    [StringLength(50)]
    public string? BankName { get; set; }

    [StringLength(20)]
    public string? AccountType { get; set; }

    [StringLength(50)]
    public string? AccountNumber { get; set; }

    [StringLength(10)]
    public string? BranchCode { get; set; }

    public bool HasCreditCheck { get; set; }
    public DateTime? LastCreditCheckDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// Enums for personal information
public enum MaritalStatus
{
    Single,
    Married,
    Divorced,
    Widowed,
    Other
}

public enum EmploymentStatus
{
    Employed,
    SelfEmployed,
    Unemployed,
    Retired,
    Student,
    Other
}