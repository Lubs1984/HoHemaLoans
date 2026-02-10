using System.ComponentModel.DataAnnotations;

namespace HoHemaLoans.Api.Models;

public class BankTransaction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>Transaction date from bank statement</summary>
    public DateTime TransactionDate { get; set; }
    
    /// <summary>Amount (positive = credit/incoming, negative = debit/outgoing)</summary>
    public decimal Amount { get; set; }
    
    /// <summary>Running balance from bank statement</summary>
    public decimal? Balance { get; set; }
    
    /// <summary>Description from bank statement</summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>Bank reference number</summary>
    public string? Reference { get; set; }
    
    /// <summary>Credit or Debit</summary>
    public TransactionType Type { get; set; }
    
    /// <summary>Incoming = repayment received, Outgoing = loan disbursement</summary>
    public TransactionCategory Category { get; set; } = TransactionCategory.Unknown;
    
    /// <summary>Whether this transaction has been matched to a deduction or payout</summary>
    public MatchStatus MatchStatus { get; set; } = MatchStatus.Unmatched;
    
    /// <summary>Matched deduction schedule entry (for repayments)</summary>
    public Guid? MatchedDeductionId { get; set; }
    public virtual DeductionScheduleEntry? MatchedDeduction { get; set; }
    
    /// <summary>Matched loan application (for disbursements)</summary>
    public Guid? MatchedLoanId { get; set; }
    public virtual LoanApplication? MatchedLoan { get; set; }
    
    /// <summary>Who matched this (admin user ID)</summary>
    public string? MatchedByUserId { get; set; }
    
    /// <summary>When was it matched</summary>
    public DateTime? MatchedAt { get; set; }
    
    /// <summary>Import batch identifier</summary>
    public string? ImportBatchId { get; set; }
    
    /// <summary>Name of the uploaded file</summary>
    public string? SourceFileName { get; set; }
    
    /// <summary>FNB account number this came from</summary>
    public string? AccountNumber { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum TransactionType
{
    Credit,
    Debit
}

public enum TransactionCategory
{
    Unknown,
    LoanRepayment,
    LoanDisbursement,
    Fee,
    Interest,
    Reversal,
    Other
}

public enum MatchStatus
{
    Unmatched,
    AutoMatched,
    ManuallyMatched,
    Ignored
}
