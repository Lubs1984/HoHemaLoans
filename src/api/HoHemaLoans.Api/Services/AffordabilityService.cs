using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HoHemaLoans.Api.Services;

public interface IAffordabilityService
{
    Task<AffordabilityAssessment> CalculateAffordabilityAsync(string userId);
    Task<decimal> GetMaxRecommendedLoanAmountAsync(string userId);
}

public class AffordabilityService : IAffordabilityService
{
    private readonly ApplicationDbContext _context;

    public AffordabilityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AffordabilityAssessment> CalculateAffordabilityAsync(string userId)
    {
        // Get all incomes for the user
        var incomes = await _context.Incomes
            .Where(i => i.UserId == userId)
            .ToListAsync();

        // Get all expenses for the user
        var expenses = await _context.Expenses
            .Where(e => e.UserId == userId)
            .ToListAsync();

        // Calculate income totals
        var grossMonthlyIncome = incomes.Sum(i => ConvertToMonthly(i.MonthlyAmount, i.Frequency));
        
        // Calculate expense totals
        var totalMonthlyExpenses = expenses.Sum(e => ConvertToMonthly(e.MonthlyAmount, e.Frequency));
        var essentialExpenses = expenses
            .Where(e => e.IsEssential)
            .Sum(e => ConvertToMonthly(e.MonthlyAmount, e.Frequency));
        var nonEssentialExpenses = totalMonthlyExpenses - essentialExpenses;

        // Calculate net income
        var netMonthlyIncome = grossMonthlyIncome - totalMonthlyExpenses;

        // Calculate affordability ratios
        var debtToIncomeRatio = grossMonthlyIncome > 0 ? totalMonthlyExpenses / grossMonthlyIncome : 0;
        var expenseToIncomeRatio = grossMonthlyIncome > 0 ? totalMonthlyExpenses / grossMonthlyIncome : 0;
        var availableFunds = netMonthlyIncome;

        // Determine affordability status based on NCA guidelines
        var affordabilityStatus = DetermineAffordabilityStatus(debtToIncomeRatio, netMonthlyIncome, grossMonthlyIncome);
        
        // Calculate max recommended loan amount
        // Based on: Available funds can support monthly repayment within affordability limits
        var maxRecommendedLoanAmount = CalculateMaxLoanAmount(netMonthlyIncome, debtToIncomeRatio, grossMonthlyIncome);

        // Create assessment notes
        var assessmentNotes = GenerateAssessmentNotes(
            affordabilityStatus,
            debtToIncomeRatio,
            netMonthlyIncome,
            essentialExpenses,
            totalMonthlyExpenses,
            grossMonthlyIncome
        );

        // Check if assessment already exists
        var existingAssessment = await _context.AffordabilityAssessments
            .FirstOrDefaultAsync(a => a.UserId == userId);

        var assessment = existingAssessment ?? new AffordabilityAssessment { UserId = userId };

        assessment.GrossMonthlyIncome = grossMonthlyIncome;
        assessment.NetMonthlyIncome = netMonthlyIncome;
        assessment.TotalMonthlyExpenses = totalMonthlyExpenses;
        assessment.EssentialExpenses = essentialExpenses;
        assessment.NonEssentialExpenses = nonEssentialExpenses;
        assessment.DebtToIncomeRatio = debtToIncomeRatio;
        assessment.AvailableFunds = availableFunds;
        assessment.ExpenseToIncomeRatio = expenseToIncomeRatio;
        assessment.AffordabilityStatus = affordabilityStatus;
        assessment.AssessmentNotes = assessmentNotes;
        assessment.MaxRecommendedLoanAmount = maxRecommendedLoanAmount;
        assessment.AssessmentDate = DateTime.UtcNow;
        assessment.ExpiryDate = DateTime.UtcNow.AddDays(30); // Assessment valid for 30 days

        if (existingAssessment == null)
        {
            _context.AffordabilityAssessments.Add(assessment);
        }

        await _context.SaveChangesAsync();
        return assessment;
    }

    public async Task<decimal> GetMaxRecommendedLoanAmountAsync(string userId)
    {
        var assessment = await _context.AffordabilityAssessments
            .FirstOrDefaultAsync(a => a.UserId == userId);

        return assessment?.MaxRecommendedLoanAmount ?? 0;
    }

    /// <summary>
    /// Convert any frequency to monthly amount
    /// </summary>
    private decimal ConvertToMonthly(decimal amount, string? frequency)
    {
        return (frequency?.ToLower()) switch
        {
            "weekly" => amount * 4.33m, // Average weeks per month
            "bi-weekly" => amount * 2.17m,
            "annual" => amount / 12m,
            _ => amount // Monthly or default
        };
    }

    /// <summary>
    /// Determine affordability status per NCA guidelines
    /// </summary>
    private string DetermineAffordabilityStatus(decimal debtToIncomeRatio, decimal netIncome, decimal grossIncome)
    {
        // NCA Guideline: Debt to Income ratio should not exceed 35%
        const decimal ncrLimit = 0.35m;

        if (debtToIncomeRatio > ncrLimit)
        {
            return "NotAffordable"; // Over NCA limit
        }

        if (netIncome <= 0)
        {
            return "NotAffordable"; // No money left after expenses
        }

        if (netIncome < (grossIncome * 0.10m)) // Less than 10% available after expenses
        {
            return "LimitedAffordability";
        }

        return "Affordable";
    }

    /// <summary>
    /// Calculate maximum recommended loan amount
    /// Formula: Available monthly funds can support a loan repayment over the loan term
    /// Assuming 36-month term and prime rate lending
    /// </summary>
    private decimal CalculateMaxLoanAmount(decimal availableFunds, decimal debtToIncomeRatio, decimal grossIncome)
    {
        // Reserve at least 20% of available funds for unexpected expenses
        var availableForLoan = availableFunds * 0.80m;

        // Typical monthly repayment capacity
        var monthlyRepaymentCapacity = availableForLoan;

        // For a 36-month loan at approximately 11% annual interest (South African prime rate approximate)
        // Monthly repayment formula: P = [r(PV)] / [1 - (1 + r)^-n]
        // Where P = monthly payment, r = monthly rate, PV = principal, n = number of periods

        const decimal annualInterestRate = 0.11m; // 11% approximate
        const decimal monthlyInterestRate = annualInterestRate / 12m;
        const int loanTermMonths = 36;

        var factor = ((1 + monthlyInterestRate) * (decimal)Math.Pow((double)(1 + monthlyInterestRate), loanTermMonths - 1)) / 
                     (((decimal)Math.Pow((double)(1 + monthlyInterestRate), loanTermMonths)) - 1);
        var maxLoan = monthlyRepaymentCapacity / factor;
        return Math.Max(0, maxLoan);
    }

    /// <summary>
    /// Generate detailed assessment notes
    /// </summary>
    private string GenerateAssessmentNotes(
        string status,
        decimal debtToIncomeRatio,
        decimal netIncome,
        decimal essentialExpenses,
        decimal totalExpenses,
        decimal grossIncome)
    {
        var notes = status switch
        {
            "NotAffordable" when debtToIncomeRatio > 0.35m =>
                $"Your debt-to-income ratio of {(debtToIncomeRatio * 100):F1}% exceeds the NCR limit of 35%. " +
                "You may not be approved for additional loans until your debt levels decrease.",

            "NotAffordable" when netIncome <= 0 =>
                "Your monthly expenses exceed your income. Consider reducing non-essential expenses " +
                "before applying for a loan.",

            "LimitedAffordability" =>
                $"Your available funds after expenses are limited ({(netIncome):C}). " +
                "A small loan may be considered, but you should focus on reducing expenses first.",

            "Affordable" =>
                $"Your affordability profile is healthy. You have {(netIncome):C} available monthly after expenses. " +
                $"Your debt-to-income ratio is {(debtToIncomeRatio * 100):F1}%, well within NCR guidelines.",

            _ => "Affordability assessment complete."
        };

        // Add essential vs non-essential breakdown
        var essentialPercentage = totalExpenses > 0 ? (essentialExpenses / totalExpenses) * 100 : 0;
        notes += $"\n\nExpense Analysis: {essentialPercentage:F1}% of your expenses are essential (food, utilities, etc.)";

        return notes;
    }
}
