using System.ComponentModel.DataAnnotations;

namespace HoHemaLoans.Api.Models;

public class DeductionScheduleEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid LoanApplicationId { get; set; }
    public virtual LoanApplication? LoanApplication { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public virtual ApplicationUser? User { get; set; }
    
    /// <summary>Installment number (1, 2, 3...)</summary>
    public int InstallmentNumber { get; set; }
    
    /// <summary>Date the deduction is due</summary>
    public DateTime DueDate { get; set; }
    
    /// <summary>Principal portion of this installment</summary>
    public decimal PrincipalAmount { get; set; }
    
    /// <summary>Interest portion of this installment</summary>
    public decimal InterestAmount { get; set; }
    
    /// <summary>Admin fee portion (first installment only usually)</summary>
    public decimal AdminFeeAmount { get; set; }
    
    /// <summary>Total amount due this installment</summary>
    public decimal TotalAmount { get; set; }
    
    /// <summary>Status: Scheduled, Paid, Overdue, Failed, Reversed</summary>
    public DeductionStatus Status { get; set; } = DeductionStatus.Scheduled;
    
    /// <summary>Date when payment was actually received</summary>
    public DateTime? PaidDate { get; set; }
    
    /// <summary>Amount actually paid (may differ from TotalAmount)</summary>
    public decimal? PaidAmount { get; set; }
    
    /// <summary>Bank reference for the payment</summary>
    public string? PaymentReference { get; set; }
    
    /// <summary>Linked bank transaction if auto-matched</summary>
    public Guid? BankTransactionId { get; set; }
    public virtual BankTransaction? BankTransaction { get; set; }
    
    /// <summary>Notes/comments</summary>
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum DeductionStatus
{
    Scheduled,
    Paid,
    Overdue,
    Failed,
    Reversed,
    PartiallyPaid
}
