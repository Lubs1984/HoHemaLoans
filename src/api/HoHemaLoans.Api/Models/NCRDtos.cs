namespace HoHemaLoans.Api.Models;

/// <summary>
/// DTO for pre-agreement statement request
/// </summary>
public class PreAgreementRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal LoanAmount { get; set; }
    public decimal InterestRate { get; set; }
    public int TermInMonths { get; set; }
    public decimal InitiationFee { get; set; }
    public decimal MonthlyServiceFee { get; set; }
    public decimal MonthlyInstallment { get; set; }
    public decimal TotalAmountPayable { get; set; }
}

/// <summary>
/// DTO for NCR compliance validation response
/// </summary>
public class NCRComplianceValidationResponse
{
    public Guid ApplicationId { get; set; }
    public bool IsCompliant { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime ValidationDate { get; set; }
}

/// <summary>
/// DTO for loan cancellation request
/// </summary>
public class CancelLoanRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// DTO for cooling-off status response
/// </summary>
public class CoolingOffStatusResponse
{
    public Guid ApplicationId { get; set; }
    public bool IsWithinCoolingOffPeriod { get; set; }
    public DateTime CheckedAt { get; set; }
}