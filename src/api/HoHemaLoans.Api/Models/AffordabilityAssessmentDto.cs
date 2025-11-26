namespace HoHemaLoans.Api.Models;

public class AffordabilityAssessmentDto
{
    public Guid Id { get; set; }
    public decimal GrossMonthlyIncome { get; set; }
    public decimal NetMonthlyIncome { get; set; }
    public decimal TotalMonthlyExpenses { get; set; }
    public decimal EssentialExpenses { get; set; }
    public decimal NonEssentialExpenses { get; set; }
    public decimal DebtToIncomeRatio { get; set; }
    public decimal AvailableFunds { get; set; }
    public decimal ExpenseToIncomeRatio { get; set; }
    public string AffordabilityStatus { get; set; } = string.Empty;
    public string AssessmentNotes { get; set; } = string.Empty;
    public decimal MaxRecommendedLoanAmount { get; set; }
    public DateTime AssessmentDate { get; set; }
    public DateTime ExpiryDate { get; set; }
}
